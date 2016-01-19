using System;
#if !PORTABLE
using System.Runtime.Serialization;
#endif
using System.Xml;
using System.Xml.Schema;
using VDS.RDF;
using VDS.RDF.Storage.Virtualisation;
using VDS.RDF.Writing;
using VDS.RDF.Writing.Formatting;

namespace BrightstarDB.Query
{
    internal class BrightstarVirtualNode : 
        IVirtualNode<ulong, int>, 
        IComparable<BrightstarVirtualNode>, 
        IEquatable<BrightstarVirtualNode>,
        IUriNode,
        ILiteralNode
    {
        private readonly int _graphId;
        private Uri _graphUri;
        private readonly ulong _nodeId;
        private INode _value;
        private readonly IVirtualRdfProvider<ulong, int> _provider; 

        public BrightstarVirtualNode(ulong nodeId, int graphId, IVirtualRdfProvider<ulong, int> provider)
        {
            _graphId = graphId;
            _nodeId = nodeId;
            _provider = provider;
        }

        public BrightstarVirtualNode(ulong nodeId, IVirtualRdfProvider<ulong, int> provider, INode value)
        {
            _nodeId = nodeId;
            _provider = provider;
            _value = value;
        }

        protected void MaterialiseValue()
        {
            if (_value == null)
            {
                _value = _provider.GetValue(null, _nodeId);
                OnMaterialise();
            }
        }

        protected virtual void OnMaterialise()
        {

        }

        #region IVirtualNode<TNodeId, TGraphId> members
        public ulong VirtualId {get { return _nodeId; }}

        public ulong VirtualID
        {
            get { return _nodeId; }
        }

        public IVirtualRdfProvider<ulong, int> Provider
        {
            get { return _provider; }
        } 

        public bool IsMaterialised {get { return _value != null; }}

        public INode MaterialisedValue
        {
            get
            {
                if (_value == null) MaterialiseValue();
                return _value;
            }
        }

        public int CompareVirtualId(ulong otherId)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IComparable Implementation

        public int CompareTo(IVirtualNode<ulong, int> other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other == null) return 1;
            bool areEqual;
            if (TryVirtualEquality(other, out areEqual) && areEqual)
            {
                return 0;
            }
            return this.CompareTo((INode) other);
        }

        /// <summary>
        /// Compares this Node to another Virtual Node
        /// </summary>
        /// <param name="other">Other Virtual Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform comparison.
        /// </remarks>
        public int CompareTo(BrightstarVirtualNode other)
        {
            return this.CompareTo((IVirtualNode<ulong, int>)other);
        }

        /// <summary>
        /// Compares this Node to another Node
        /// </summary>
        /// <param name="other">Other Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform comparison.
        /// </remarks>
        public int CompareTo(INode other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (other == null) return 1;
            bool areEqual;
            if (this.TryVirtualEquality(other, out areEqual) && areEqual) return 0;

            MaterialiseValue();
            switch (this._value.NodeType)
            {
                case NodeType.Blank:
                    if (other.NodeType == NodeType.Variable)
                    {
                        //Blank Nodes are greater than variables
                        return 1;
                    }
                    else if (other.NodeType == NodeType.Blank)
                    {
                        //Compare Blank Node appropriately
                        return ComparisonHelper.CompareBlankNodes((IBlankNode)this, (IBlankNode)other);
                    }
                    else
                    {
                        //Blank Nodes are less than everything else
                        return -1;
                    }

                case NodeType.GraphLiteral:
                    if (other.NodeType == NodeType.GraphLiteral)
                    {
                        //Compare Graph Literals appropriately
                        return ComparisonHelper.CompareGraphLiterals((IGraphLiteralNode)this, (IGraphLiteralNode)other);
                    }
                    else
                    {
                        //Graph Literals are greater than everything else
                        return 1;
                    }

                case NodeType.Literal:
                    if (other.NodeType == NodeType.GraphLiteral)
                    {
                        //Literals are less than Graph Literals
                        return -1;
                    }
                    else if (other.NodeType == NodeType.Literal)
                    {
                        //Compare Literals appropriately
                        return ComparisonHelper.CompareLiterals((ILiteralNode)this, (ILiteralNode)other);
                    }
                    else
                    {
                        //Literals are greater than anything else (i.e. Blanks, Variables and URIs)
                        return 1;
                    }

                case NodeType.Uri:
                    if (other.NodeType == NodeType.GraphLiteral || other.NodeType == NodeType.Literal)
                    {
                        //URIs are less than Literals and Graph Literals
                        return -1;
                    }
                    else if (other.NodeType == NodeType.Uri)
                    {
                        //Compare URIs appropriately
                        return ComparisonHelper.CompareUris((IUriNode)this, (IUriNode)other);
                    }
                    else
                    {
                        //URIs are greater than anything else (i.e. Blanks and Variables)
                        return 1;
                    }

                case NodeType.Variable:
                    if (other.NodeType == NodeType.Variable)
                    {
                        //Compare Variables accordingly
                        return ComparisonHelper.CompareVariables((IVariableNode)this, (IVariableNode)other);
                    }
                    else
                    {
                        //Variables are less than anything else
                        return -1;
                    }

                default:
                    //Things are always greater than unknown node types
                    return 1;
            }
        }

        /// <summary>
        /// Compares this Node to another Blank Node
        /// </summary>
        /// <param name="other">Other Blank Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform comparison.
        /// </remarks>
        public virtual int CompareTo(IBlankNode other)
        {
            return this.CompareTo((INode)other);
        }

        /// <summary>
        /// Compares this Node to another Graph LiteralNode
        /// </summary>
        /// <param name="other">Other Graph Literal Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform comparison.
        /// </remarks>
        public virtual int CompareTo(IGraphLiteralNode other)
        {
            return this.CompareTo((INode)other);
        }

        /// <summary>
        /// Compares this Node to another Literal Node
        /// </summary>
        /// <param name="other">Other Literal Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform comparison.
        /// </remarks>
        public virtual int CompareTo(ILiteralNode other)
        {
            return this.CompareTo((INode)other);
        }

        /// <summary>
        /// Compares this Node to another URI Node
        /// </summary>
        /// <param name="other">Other URI Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform comparison.
        /// </remarks>
        public virtual int CompareTo(IUriNode other)
        {
            return this.CompareTo((INode)other);
        }

        /// <summary>
        /// Compares this Node to another Variable Node
        /// </summary>
        /// <param name="other">Other Variable Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform comparison.
        /// </remarks>
        public virtual int CompareTo(IVariableNode other)
        {
            return this.CompareTo((INode)other);
        }

        #endregion

        #region IEquatable Implementations

        /// <summary>
        /// Checks this Node for equality against another Object
        /// </summary>
        /// <param name="obj">Other Object</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform the equality check.
        /// </remarks>
        public sealed override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj == null) return false;

            if (obj is INode)
            {
                return this.Equals((INode)obj);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks this Node for equality against another Virtual Node
        /// </summary>
        /// <param name="other">Other Virtual Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform the equality check.
        /// </remarks>
        public bool Equals(IVirtualNode<ulong, int> other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null) return false;

            return ReferenceEquals(this._provider, other.Provider) && this._nodeId.Equals(other.VirtualID);
        }

        /// <summary>
        /// Checks this Node for equality against another Virtual Node
        /// </summary>
        /// <param name="other">Other Virtual Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform the equality check.
        /// </remarks>
        public bool Equals(BrightstarVirtualNode other)
        {
            return this.Equals((IVirtualNode<ulong, int>)other);
        }

        /// <summary>
        /// Checks this Node for equality against another Node
        /// </summary>
        /// <param name="other">Other Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform the equality check.
        /// </remarks>
        public bool Equals(INode other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null) return false;

            bool areEqual;
            if (this.TryVirtualEquality(other, out areEqual))
            {
                //If Virtual Nodes originate from same virtual RDF provider can compare based on their virtual Node IDs
                return areEqual;
            }
            else
            {
                //If not both virtual and are of the same type the only way to determine equality is to
                //materialise the value of this node and then check that against the other node
                if (this._value == null) this.MaterialiseValue();
                return this._value.Equals(other);
            }
        }

        /// <summary>
        /// Checks the Node Types and if they are equal invokes the INode based comparison
        /// </summary>
        /// <param name="other">Node to compare with for equality</param>
        /// <returns></returns>
        private bool TypedEquality(INode other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other == null) return false;

            MaterialiseValue();
            return _value.NodeType == other.NodeType && Equals(other);
        }


        /// <summary>
        /// Checks this Node for equality against another Blank Node
        /// </summary>
        /// <param name="other">Other Blank Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform the equality check.
        /// </remarks>
        public virtual bool Equals(IBlankNode other)
        {
            return this.TypedEquality(other);
        }

        /// <summary>
        /// Checks this Node for equality against another Graph Literal Node
        /// </summary>
        /// <param name="other">Other Graph Literal Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform the equality check.
        /// </remarks>
        public virtual bool Equals(IGraphLiteralNode other)
        {
            return this.TypedEquality(other);
        }

        /// <summary>
        /// Checks this Node for equality against another Literal Node
        /// </summary>
        /// <param name="other">Other Literal Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform the equality check.
        /// </remarks>
        public virtual bool Equals(ILiteralNode other)
        {
            return this.TypedEquality(other);
        }

        /// <summary>
        /// Checks this Node for equality against another URI Node
        /// </summary>
        /// <param name="other">Other URI Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform the equality check.
        /// </remarks>
        public virtual bool Equals(IUriNode other)
        {
            return this.TypedEquality(other);
        }

        /// <summary>
        /// Checks this Node for equality against another Variable Node
        /// </summary>
        /// <param name="other">Other Variable Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Unless Virtual Equality (equality based on the Virtual RDF Provider and Virtual ID) can be determined or the Nodes are of different types then the Nodes value will have to be materialised in order to perform the equality check.
        /// </remarks>
        public virtual bool Equals(IVariableNode other)
        {
            return this.TypedEquality(other);
        }

        #endregion

        
        /// <summary>
        /// Tries to check for equality using virtual node IDs
        /// </summary>
        /// <param name="other">Node to test against</param>
        /// <param name="areEqual">Whether the virtual nodes are equal</param>
        /// <returns>
        /// Whether the virtual equality test was valid, if false then other means must be used to determine equality
        /// </returns>
        protected bool TryVirtualEquality(INode other, out bool areEqual)
        {
            areEqual = false;
            if (!(other is IVirtualNode<ulong, int>))
            {
                return false;
            }
            var virt = (IVirtualNode<ulong, int>) other;
            if (!ReferenceEquals(_provider, virt.Provider))
            {
                return false;
            }
            areEqual = _nodeId.Equals(virt.VirtualID);
            return true;
        }

        /// <summary>
        /// Gets the Hash Code of the Virtual Node
        /// </summary>
        /// <returns></returns>
        public sealed override int GetHashCode()
        {
            return this._nodeId.GetHashCode();
        }

        /// <summary>
        /// Attempt to compare this object with another INode instance
        /// </summary>
        /// <param name="other">The other INode instance to compare with</param>
        /// <param name="compareResult">Receives the result of the comparison if it could be performed</param>
        /// <returns>True if a virutal ID comparison could be performed, false otherwise.</returns>
        public bool TryCompareVirtualId(INode other, out int compareResult)
        {
            if (other is BrightstarVirtualNode)
            {
                var virt = other as BrightstarVirtualNode;
                if (ReferenceEquals(_provider, virt.Provider))
                {
                    compareResult = _nodeId.CompareTo(virt.VirtualID);
                    return true;
                }
            }
            compareResult = 0;
            return false;
        }

#if !PORTABLE
        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data. </param><param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization. </param><exception cref="T:System.Security.SecurityException">The caller does not have the required permission. </exception>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
#endif

        public NodeType NodeType
        {
            get
            {
                MaterialiseValue();
                return _value.NodeType;
            }
        }

        public IGraph Graph
        {
            // TODO: Should this return some sort of wrapper around GraphUri ?
            get { return null; }
        }

        public Uri GraphUri
        {
            get
            {
                MaterialiseGraph();
                return _graphUri;
            }
            set { throw new NotImplementedException(); }
        }

        private void MaterialiseGraph()
        {
            if (_graphUri == null)
            {
                _graphUri = _provider.GetGraphUri(_graphId);
            }
        }

        /// <summary>
        /// Gets the String representation of the Node
        /// </summary>
        /// <returns></returns>
        public sealed override string ToString()
        {
            if (this._value == null) this.MaterialiseValue();
            return this._value.ToString();
        }

        public string ToString(INodeFormatter formatter)
        {
            return formatter.Format(this);
        }

        public string ToString(INodeFormatter formatter, TripleSegment segment)
        {
            return formatter.Format(this, segment);
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new NotImplementedException("This INode implementation does not support XML Serialization");
        }

        public void WriteXml(XmlWriter writer)
        {
            throw new NotImplementedException("This INode implementation does not support XML Serialization");
        }

        # region IUriNode implentation
        Uri IUriNode.Uri
        {
            get
            {
                var uriNode = MaterialisedValue as IUriNode;
                if (uriNode == null) throw new InvalidCastException();
                return uriNode.Uri;
            }
        }
        #endregion

        string ILiteralNode.Value
        {
            get
            {
                var litNode = MaterialisedValue as ILiteralNode;
                if (litNode == null) throw new InvalidCastException();
                return litNode.Value;
            }
        }

        string ILiteralNode.Language
        {
            get {
                var litNode = MaterialisedValue as ILiteralNode;
                if (litNode == null) throw new InvalidCastException();
                return litNode.Language;
            }
        }

        Uri ILiteralNode.DataType
        {
            get
            {
                var litNode = MaterialisedValue as ILiteralNode;
                if (litNode == null) throw new InvalidCastException();
                return litNode.DataType;
            }
        }
    }
}