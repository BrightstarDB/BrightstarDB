using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#endif

namespace BrightstarDB.Storage.BTreeStore
{
    internal class PersistentBTree<T> : IPersistable where T : class, IStorable
    {
        public int KeyCount;

        private ulong _rootNodeId;

        private ulong _objectId;

        public int KeyMedian
        {
            get { return KeyCount / 2; }
        }

        public int KeysMinimum
        {
            get { return KeyCount / 2; }
        }

        /// <summary>
        /// The nodes modified on a write operation
        /// </summary>
        // private List<Node> _commitList =  new List<Node>();

        public ulong ObjectId
        {
            get { return _objectId; }
            set { _objectId = value; }
        }

        public bool ScheduledForCommit
        {
            get;
            set;
        }

        protected Store _store;

        /// <summary>
        /// The store to which this btree belongs
        /// </summary>
        public IStore Store
        {
            get { return _store; }
            set { _store = value as Store; }
        }


        public PersistentBTree(ulong objectId, int keyCount, Store store)
        {
            Store = store;
            KeyCount = keyCount;
            _rootNodeId = StoreConstants.NullUlong;
            _objectId = objectId;
        }

        public int Save(BinaryWriter dataStream, ulong offset = 0ul)
        {
            int count = SerializationUtils.WriteVarint(dataStream, _objectId);
            count += SerializationUtils.WriteVarint(dataStream, _rootNodeId);
            count += SerializationUtils.WriteVarint(dataStream, (ulong)KeyCount);
            return count;
        }

        public void Read(BinaryReader dataStream)
        {
            _objectId = SerializationUtils.ReadVarint(dataStream);
            _rootNodeId = SerializationUtils.ReadVarint(dataStream);
            KeyCount = (int)SerializationUtils.ReadVarint(dataStream);
        }

        public Node<T> LoadNode(ulong objectId)
        {
            if (objectId == StoreConstants.NullUlong) return null;
            return _store.LoadObject<Node<T>>(objectId);
        }        

        /// <summary>
        /// Default constuctor to support serialisation
        /// </summary>
        public PersistentBTree()
        {
        }

        private void AddToCommitList(IPersistable node)
        {
            _store.AddToCommitList(node);
        }        

        public Node<T> MakeNewNode(ulong parent = StoreConstants.NullUlong)
        {
            var nodeId = _store.GetNextObjectId();
            var newNode = new Node<T>(nodeId, parent, KeyCount);

            _store.AddToCommitList(newNode);
            
            return newNode;
        }

        public Node<T> Root
        {            
            get
            {
                if (_rootNodeId == 0) return null;
                return LoadNode(_rootNodeId);
            }
            set { 
                _rootNodeId = value.NodeId;
                _store.AddToCommitList(this);
            }
        }

        public Node<T> Lookup(ulong key)
        {
            return Lookup(new Entry<T>(key));
        }

        public Node<T> Lookup(Entry<T> key)
        {
            if (Root == null)
            {
                return null;
            }
            return FindKeyNode(Root, key);
        }

        public T FindValue(ulong key)
        {
            Entry<T> entry;
            Node<T> node;
            LookupEntry(key, out entry, out node);
            if (entry == null) return null;
            return entry.Value;
        }

        public bool LookupEntry(ulong key, out Entry<T> entry, out Node<T> node)
        {
            if (Root == null)
            {
                entry = null;
                node = null;
                return false;
            }
            return FindKeyEntry(Root, new Entry<T>(key), out entry, out node);
        }
        
        public IEnumerable<Entry<T>> InOrderTraversal()
        {
            return TraverseNode(Root);
        }

        private IEnumerable<Entry<T>> TraverseNode(Node<T> node)
        {
            if (node == null) yield break;
            
            if (node.ChildNodes.Count == 0)
            {
                foreach (var k in node.Keys) yield return k;
            }
            else
            {
                for (int i = 0; i < node.Keys.Count; i++ )
                {
                    foreach (var k in TraverseNode(LoadNode(node.ChildNodes[i]))) yield return k;
                    yield return node.Keys[i];
                }
                foreach (var k in TraverseNode(LoadNode(node.ChildNodes[node.Keys.Count]))) yield return k;
                /*
                    foreach (var n in node.ChildNodes)
                    {
                        foreach (var k in TraverseNode(LoadNode(n))) yield return k;
                    }                   
                 */
            }
        }

        public static IEnumerable<Entry<T>> InOrderTraversal(PersistentBTree<T> tree)
        {
            return TraverseNode(tree, tree.Root);
        }

        private static IEnumerable<Entry<T>> TraverseNode(PersistentBTree<T> tree, Node<T> node)
        {
            if (node.ChildNodes.Count == 0)
            {
                foreach(var e in node.Keys) yield return e;
                yield break;
            }

            for (int i = 0; i < node.Keys.Count; i++)
            {
                foreach(var e in TraverseNode(tree, tree.LoadNode(node.ChildNodes[i])))
                {
                    yield return e;
                }
                yield return node.Keys[i];
            }

            foreach(var e in TraverseNode(tree, tree.LoadNode(node.ChildNodes.Last())))
            {
                yield return e;
            }
        }

        //public void Update(Node<T> node)
        //{
        //    AddToCommitList(node);
        //}

        public void Delete(ulong key)
        {
            Delete(new Entry<T>(key));
        }

        public void Delete(Entry<T> key)
        {
            // get node
            var node = FindKeyNode(Root, key);

            if (node == null)
            {
                throw new BrightstarInternalException("Key does not exist");
            }

            if (node.ChildNodes.Count == 0)
            {
                // delete from leaf
                node.Keys.Remove(key);
                AddToCommitList(node);
                AssertAfterDeleteInvariant(node);
            } else
            {
                // delete from internal node
                // find the pos get the first key in the node to the right as the seperator
                var keyPos = node.Keys.IndexOf(key);
                var successorNode = GetSuccessorLeftTraversal(LoadNode(node.ChildNodes[keyPos + 1]));

                // replace the deleted key in this node with the leftmost value in successor node
                node.Keys[keyPos] = successorNode.Keys[0];
                AddToCommitList(node);

                // call delete on the successor 
                successorNode.Keys.RemoveAt(0);
                AddToCommitList(successorNode);

                // assert invariants
                AssertAfterDeleteInvariant(successorNode);
            }
        }

        private void AssertAfterDeleteInvariant(Node<T> node)
        {
            if (node.IsRoot)
            {
                return;
            }

            if (node.Keys.Count < KeysMinimum)
            {
                var leftSibling = GetLeftSibling(node);
                var rightSibling = GetRightSibling(node);
                var nodeIndexInParentChildNodes = LoadNode(node.ParentNodeId).ChildNodes.IndexOf(node.NodeId);

                if (rightSibling != null && rightSibling.Keys.Count > KeysMinimum)
                {
                    BorrowFromRightSibling(node, nodeIndexInParentChildNodes, rightSibling);
                    return;
                }

                if (leftSibling != null && leftSibling.Keys.Count > KeysMinimum)
                {
                    BorrowFromLeftSibling(node, nodeIndexInParentChildNodes, leftSibling);
                    return;
                }

                if (leftSibling != null)
                {
                    // do left merge
                    PerformLeftMerge(node, nodeIndexInParentChildNodes, leftSibling);
                    
                    // unless we have become the new root check the parent is ok
                    if (node.NodeId != Root.NodeId)
                    {
                        AssertAfterDeleteInvariant(LoadNode(node.ParentNodeId));
                    }
                    return;
                }

                if (rightSibling != null)
                {
                    PerformRightMerge(node, nodeIndexInParentChildNodes, rightSibling);

                    // check the parent is ok                    
                    if (node.NodeId != Root.NodeId)
                    {
                        AssertAfterDeleteInvariant(LoadNode(node.ParentNodeId));
                    }
                    return;
                }
            }
            
        }

        private void PerformRightMerge(Node<T> node, int nodeIndexInParentChildNodes, Node<T> rightSibling)
        {
            // get the parent key to include in merged child
            var parentNode = LoadNode(node.ParentNodeId);
            var parentKey = parentNode.Keys[nodeIndexInParentChildNodes];

            // remove parent key and right sibling pointer
            parentNode.Keys.RemoveAt(nodeIndexInParentChildNodes);
            parentNode.ChildNodes.RemoveAt(nodeIndexInParentChildNodes + 1);
            AddToCommitList(parentNode);

            // add the parent key and then all the keys in the left sibling
            node.Keys.Add(parentKey);
            node.Keys.AddRange(rightSibling.Keys);
            AddToCommitList(node);

            // add the child nodes from the left sibling
            if (node.ChildNodes.Count > 0)
            {
                node.ChildNodes.AddRange(rightSibling.ChildNodes);

                // update parent pointers
                foreach (var childNodeId in rightSibling.ChildNodes)
                {
                    var childNode = LoadNode(childNodeId); 
                    childNode.ParentNodeId = node.ParentNodeId;
                    AddToCommitList(childNode);
                }
            }

            // check for root collapsed.
            if (parentNode.IsRoot && parentNode.Keys.Count == 0)
            {
                // set tree root to be this node
                Root = node;
                node.ParentNodeId = StoreConstants.NullUlong;
            }
        }

        private void PerformLeftMerge(Node<T> node, int nodeIndexInParentChildNodes, Node<T> leftSibling)
        {
            // get the parent key to include in merged child
            var parentNode = LoadNode(node.ParentNodeId);
            var parentKey = parentNode.Keys[nodeIndexInParentChildNodes - 1];

            // remove parent key and left sibling pointer
            parentNode.Keys.RemoveAt(nodeIndexInParentChildNodes - 1);
            parentNode.ChildNodes.RemoveAt(nodeIndexInParentChildNodes - 1);
            AddToCommitList(parentNode);

            // pre-pend the parent key and then all the keys in the left sibling
            node.Keys.Insert(0, parentKey);
            node.Keys.InsertRange(0, leftSibling.Keys);
            AddToCommitList(node);

            // pre-pend the child nodes from the left sibling
            if (node.ChildNodes.Count > 0)
            {
                node.ChildNodes.InsertRange(0, leftSibling.ChildNodes);

                // change parent pointers
                foreach (var childNodeId in leftSibling.ChildNodes)
                {
                    var childNode = LoadNode(childNodeId);
                    childNode.ParentNodeId = node.ParentNodeId;
                    AddToCommitList(childNode);
                }
            }

            // check for root collapsed.
            if (parentNode.IsRoot && parentNode.Keys.Count == 0)
            {
                // set tree root to be this node
                Root = node;
                node.ParentNodeId = StoreConstants.NullUlong;
            }
        }

        private void BorrowFromRightSibling(Node<T> node, int nodeIndexInParentChildNodes, Node<T> rightSibling)
        {
            // get the successor key in parent
            var parentNode = LoadNode(node.ParentNodeId);
            var successorKey = parentNode.Keys[nodeIndexInParentChildNodes];

            // move that down and add it to the end
            node.Keys.Add(successorKey);
            AddToCommitList(node);

            // move the left most key in right sibling to position in parent
            var leftMostKey = rightSibling.Keys.ElementAt(0);
            rightSibling.Keys.RemoveAt(0);
            AddToCommitList(rightSibling);

            // if this has children make it the right most pointer
            if (rightSibling.ChildNodes.Count > 0)
            {
                var rightSiblingFirstChild = LoadNode(rightSibling.ChildNodes.First());
                node.ChildNodes.Add(rightSiblingFirstChild.NodeId);
                rightSiblingFirstChild.ParentNodeId = node.NodeId;
                rightSibling.ChildNodes.RemoveAt(0);
                AddToCommitList(rightSiblingFirstChild);
            }

            parentNode.Keys[nodeIndexInParentChildNodes] = leftMostKey;            
            AddToCommitList(parentNode);
        }

        private void BorrowFromLeftSibling(Node<T> node, int nodeIndexInParentChildNodes, Node<T> leftSibling)
        {
            // get the successor key in parent
            var parentNode = LoadNode(node.ParentNodeId);
            var successorKey = parentNode.Keys[nodeIndexInParentChildNodes-1];

            // move that down and add it to the beginning
            node.Keys.Insert(0, successorKey);
            AddToCommitList(node);

            // move the rigth most key in left sibling to position in parent
            var rightMostKey = leftSibling.Keys.Last();
            leftSibling.Keys.RemoveAt(leftSibling.Keys.Count-1);
            AddToCommitList(leftSibling);

            // if this sibling has children make it the left most pointer
            if (leftSibling.ChildNodes.Count > 0)
            {
                var leftSiblingLastChild = LoadNode(leftSibling.ChildNodes.Last());
                node.ChildNodes.Insert(0, leftSiblingLastChild.NodeId);
                leftSiblingLastChild.ParentNodeId = node.NodeId;
                leftSibling.ChildNodes.RemoveAt(leftSibling.ChildNodes.Count - 1);
                AddToCommitList(leftSiblingLastChild);
            }

            parentNode.Keys[nodeIndexInParentChildNodes-1] = rightMostKey;
            AddToCommitList(parentNode);
        }

        private Node<T> GetRightSibling(Node<T> node)
        {
            var parentNode = LoadNode(node.ParentNodeId);
            var nodeIndexInParentChildNodes = parentNode.ChildNodes.IndexOf(node.NodeId);
            if (nodeIndexInParentChildNodes < (parentNode.ChildNodes.Count - 1))
            {
                return LoadNode(parentNode.ChildNodes[nodeIndexInParentChildNodes + 1]);
            }
            return null;
        }

        private Node<T> GetLeftSibling(Node<T> node)
        {
            var parentNode = LoadNode(node.ParentNodeId);
            var nodeIndexInParentChildNodes = parentNode.ChildNodes.IndexOf(node.NodeId);
            if (nodeIndexInParentChildNodes > 0)
            {
                return LoadNode(parentNode.ChildNodes[nodeIndexInParentChildNodes - 1]);
            }
            return null;            
        }

        private Node<T> GetSuccessorLeftTraversal(Node<T> node)
        {
            if (node.ChildNodes.Count == 0)
            {
                return node;
            }
            return GetSuccessorLeftTraversal(LoadNode(node.ChildNodes.First()));
        }

#if DEBUG && !SILVERLIGHT
        private int insertCount = 0;
        private long insertTimeAggregator = 0;
#endif

        public void Insert(Entry<T> key)
        {
#if DEBUG && !SILVERLIGHT
            insertCount++;
            var timer = new Stopwatch();
            timer.Start();
#endif
            if (Root == null)
            {
                // create a new node and insert the element.
                Root = MakeNewNode();
                Root.Keys.Add(key);                
                return;
            }

            var insertNode = FindNodeForKeyInsert(Root, key);
            Insert(insertNode, key);

#if DEBUG && !SILVERLIGHT
            timer.Stop();
            insertTimeAggregator += timer.ElapsedMilliseconds;

            if (timer.ElapsedMilliseconds > 30)
            {
                Logging.LogWarning(BrightstarEventId.StorePerformanceWarning, "Long insert. Insert count={0}. Insert time={1}ms", insertCount,
                                   timer.ElapsedMilliseconds);
            }

            //if (insertCount == 10000)
            //{
            //    Console.WriteLine("insert last 1000 inserts in btree took: " + (insertTimeAggregator / 10000));
            //    insertCount = 0;
            //    insertTimeAggregator = 0;
            //} 
#endif
        }

        protected void Insert(Node<T> insertNode, Entry<T> key)
        {
            var insertPos = insertNode.Keys.BinarySearch(key, key);
            if (insertPos < 0)
            {
                insertNode.Keys.Insert(~insertPos, key);
            }

            // add this node to the commit list 
            AddToCommitList(insertNode);

            // check for need to split
            AssertInvariant(insertNode);
        }

        public void Insert(ulong key, T value)
        {
            Insert(new Entry<T>(key, value));
        }

        private void AssertInvariant(Node<T> insertNode)
        {
            if (insertNode.Keys.Count == KeyCount)
            {
                if (insertNode.IsRoot)
                {
                    // insert node will be modified
                    AddToCommitList(insertNode);

                    // new root
                    var newRoot = MakeNewNode();

                    // create one new node
                    var newNode = MakeNewNode(newRoot.NodeId);

                    // add the right most elements to it
                    newNode.Keys.AddRange(insertNode.Keys.GetRange(KeyMedian + 1, insertNode.Keys.Count - (KeyMedian + 1)));
                    insertNode.Keys.RemoveRange(KeyMedian + 1, insertNode.Keys.Count - (KeyMedian + 1));
                    /*
                    foreach (var i in insertNode.Keys.GetRange(KeyMedian + 1, insertNode.Keys.Count - (KeyMedian + 1)))
                    {
                        insertNode.Keys.Remove(i);
                        newNode.Keys.Add(i);
                    }
                    */
                    if (insertNode.ChildNodes.Count > 0)
                    {
                        // move child nodes over as well
                        foreach (var i in insertNode.ChildNodes.GetRange(KeyMedian + 1, insertNode.ChildNodes.Count - (KeyMedian + 1)))
                        {
                            insertNode.ChildNodes.Remove(i);
                            newNode.ChildNodes.Add(i);
                            var childNode = LoadNode(i);
                            childNode.ParentNodeId = newNode.NodeId;
                            AddToCommitList(childNode);
                        }
                    }

                    // add median key to the parent
                    var medianKey = insertNode.Keys.Last();
                    insertNode.Keys.RemoveAt(insertNode.Keys.Count - 1);

                    // the insert pos of the median key is to the right of the node pointer we followed from the parent                
                    newRoot.ChildNodes.Add(insertNode.NodeId);
                    newRoot.ChildNodes.Add(newNode.NodeId);
                    newRoot.Keys.Add(medianKey);

                    insertNode.ParentNodeId = newRoot.NodeId;
                    Root = newRoot;
                }
                else
                {
                    AddToCommitList(insertNode);

                    // create one new node                   
                    var newNode = MakeNewNode(insertNode.ParentNodeId);

                    // add the right most elements to it
                    newNode.Keys.AddRange(insertNode.Keys.GetRange(KeyMedian + 1, insertNode.Keys.Count - (KeyMedian + 1)));
                    insertNode.Keys.RemoveRange(KeyMedian + 1, insertNode.Keys.Count - (KeyMedian + 1));
                    /*
                    foreach (var i in insertNode.Keys.GetRange(KeyMedian + 1, insertNode.Keys.Count - (KeyMedian + 1)))
                    {
                        insertNode.Keys.Remove(i);
                        newNode.Keys.Add(i);
                    }
                    */
                    // move child nodes over as well
                    if (insertNode.ChildNodes.Count > 0)
                    {
                        var transferredNodes = insertNode.ChildNodes.GetRange(KeyMedian + 1,
                                                                              insertNode.ChildNodes.Count -
                                                                              (KeyMedian + 1));
                        
                        newNode.ChildNodes.AddRange(transferredNodes);
                        insertNode.ChildNodes.RemoveRange(KeyMedian + 1, insertNode.ChildNodes.Count - (KeyMedian + 1));
                        foreach(var i in transferredNodes)
                        {
                            var childNode = LoadNode(i);
                            childNode.ParentNodeId = newNode.NodeId;
                            AddToCommitList(childNode);
                        }
                        /*
                        foreach (var i in insertNode.ChildNodes.GetRange(KeyMedian + 1, insertNode.ChildNodes.Count - (KeyMedian + 1)))
                        {
                            insertNode.ChildNodes.Remove(i);
                            newNode.ChildNodes.Add(i);
                            var childNode = LoadNode(i);
                            childNode.ParentNodeId = newNode.NodeId;
                            AddToCommitList(childNode);
                        }
                         */
                    }

                    // add median key to the parent
                    var medianKey = insertNode.Keys.Last();
                    insertNode.Keys.RemoveAt(insertNode.Keys.Count - 1);

                    // get the index of the insertnode in parent
                    var parentNode = LoadNode(insertNode.ParentNodeId);
                    int indexInParent = parentNode.ChildNodes.FindIndex(n => n == insertNode.NodeId);

                    parentNode.ChildNodes.Insert(indexInParent + 1, newNode.NodeId);
                    parentNode.Keys.Insert(indexInParent, medianKey);
                    AddToCommitList(parentNode);

                    AssertInvariant(parentNode);
                }
            }
        }

        private Node<T> FindKeyNode(Node<T> node, Entry<T> key)
        {
            var index = node.Keys.BinarySearch(key, key);
            if (index >= 0)
            {
                return node;
            } else
            {
                if (node.ChildNodes.Count == 0)
                {
                    return null;
                } else
                {
                    return FindKeyNode(LoadNode(node.ChildNodes[~index]), key);    
                }
            }
        }

        private bool FindKeyEntry(Node<T> node, Entry<T> key, out Entry<T> match, out Node<T> matchNode)
        {
            var index = node.Keys.BinarySearch(key, key);
            if (index >= 0)
            {
                match = node.Keys[index];
                match.Store = Store;
                matchNode = node;
                return true;
            }
            if (node.ChildNodes.Count == 0)
            {
                match = null;
                matchNode = node;
                return false;
            }
            return FindKeyEntry(LoadNode(node.ChildNodes[~index]), key, out match, out matchNode);
        }

        private Node<T> FindNodeForKeyInsert(Node<T> node, Entry<T> key)
        {
            if (node.ChildNodes.Count == 0)
            {
                if (node.Keys.BinarySearch(key, key) >= 0) throw new BrightstarInternalException("Key already exists");
                return node;
            }
            var index = node.Keys.BinarySearch(key,key);
            if (index >= 0)
            {
                throw new BrightstarInternalException("Key already exists");
            }
            var insertPos = ~index;
            return FindNodeForKeyInsert(LoadNode(node.ChildNodes[insertPos]), key);
        }

    }
}
