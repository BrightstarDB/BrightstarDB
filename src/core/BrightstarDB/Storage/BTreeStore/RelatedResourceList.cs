using System;
using System.Collections.Generic;
using System.Linq;

namespace BrightstarDB.Storage.BTreeStore
{

    internal class RelatedResourceList : PersistentBTree<RelatedResource>
    {
        [Obsolete("This method provided for serialization")]
        public RelatedResourceList() {}

        public RelatedResourceList(ulong objectId, int keyCount, Store store):base(objectId,keyCount,store){}

        public void AddRelatedResource(ulong rid, ulong graphId)
        {
            Entry<RelatedResource> existingEntry;
            Node<RelatedResource> existingNode;
            if (LookupEntry(rid, out existingEntry, out existingNode))
            {
                if (!existingEntry.Value.Graph.Contains(graphId))
                {
                    existingEntry.Value.Graph.Add(graphId);
                }
                _store.AddToCommitList(existingNode);
            }
            else if (existingNode != null)
            {
                var relatedResource = new RelatedResource {Rid = rid, Graph = new List<ulong> {graphId}};
                Insert(existingNode, new Entry<RelatedResource>(rid, relatedResource));
            }
            else
            {
                var relatedResource = new RelatedResource {Rid = rid, Graph = new List<ulong> {graphId}};
                Insert(rid, relatedResource);
            }
        }

        public IEnumerable<RelatedResource> Members
        {
            get { return InOrderTraversal(this).Select(x => x.Value); }
        }

        public void RemoveRelatedResource(ulong rid, ulong graphId)
        {
            Entry<RelatedResource> existingEntry;
            Node<RelatedResource> existingNode;
            if (LookupEntry(rid, out existingEntry, out existingNode))
            {
                if (existingEntry.Value.Graph.Contains(graphId))
                {
                    existingEntry.Value.Graph.Remove(graphId);
                    if (existingEntry.Value.Graph.Count == 0)
                    {
                        Delete(existingEntry);
                    }
                    else
                    {
                        _store.AddToCommitList(existingNode);
                    }
                }
            }
        }
    }

    
}
