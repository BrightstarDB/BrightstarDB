using System;
using BrightstarDB.Storage;
using BrightstarDB.Storage.BPlusTreeStore.ResourceIndex;
using VDS.RDF;
using VDS.RDF.Storage.Virtualisation;

namespace BrightstarDB.Query
{
    internal class BrightstarRdfProvider : IVirtualRdfProvider<ulong, int>
    {
        private IStore _store;

        public BrightstarRdfProvider(IStore store)
        {
            _store = store;
        }

        /// <summary>
        /// Given a Node ID returns the materialised value in the given Graph
        /// </summary>
        /// <param name="g">Graph to create the Node in</param>
        /// <param name="id">Node ID</param>
        /// <returns></returns>
        public INode GetValue(IGraph g, ulong id)
        {
            IResource resource = _store.Resolve(id);
            return MakeVdsNode(resource);
        }

        /// <summary>
        /// Given a Graph ID returns the value of the Graph URI
        /// </summary>
        /// <param name="id">Graph ID</param>
        /// <returns>The URI of the graph or NULL if no graph with the specified internal ID was found</returns>
        public Uri GetGraphUri(int id)
        {
            string graphUri =  _store.ResolveGraphUri(id);
            return graphUri == null ? null : new Uri(graphUri);
        }

        /// <summary>
        /// Given a non-blank Node returns the Node ID
        /// </summary>
        /// <param name="value">Node</param>
        /// <remarks>
        /// Should function as equivalent to the two argument version with the <strong>createIfNotExists</strong> parameter set to false
        /// </remarks>
        public ulong GetID(INode value)
        {
            return GetID(value, false);
        }

        /// <summary>
        /// Gets the Graph ID for a Graph
        /// </summary>
        /// <param name="g">Graph</param>
        /// <returns></returns>
        /// <remarks>
        /// Should function as equivalent to the two argument version with the <strong>createIfNotExists</strong> parameter set to false
        /// </remarks>
        public int GetGraphID(IGraph g)
        {
            return GetGraphID(g.BaseUri, false);
        }

        /// <summary>
        /// Gets the Graph ID for a Graph creating it if necessary
        /// </summary>
        /// <param name="g">Graph</param>
        /// <param name="createIfNotExists">Determines whether to create a new Graph ID if there is not already one for the given Graph</param>
        /// <returns></returns>
        public int GetGraphID(IGraph g, bool createIfNotExists)
        {
            return GetGraphID(g.BaseUri, createIfNotExists);
        }

        /// <summary>
        /// Gets the Graph ID for a Graph URI
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        /// <returns></returns>
        /// <remarks>
        /// Should function as equivalent to the two argument version with the <strong>createIfNotExists</strong> parameter set to false
        /// </remarks>
        public int GetGraphID(Uri graphUri)
        {
            return GetGraphID(graphUri, false);
        }

        /// <summary>
        /// Gets the Graph ID for a Graph URI
        /// </summary>
        /// <param name="graphUri">Graph URI</param>
        /// <param name="createIfNotExists">Determines whether to create a new Graph ID if there is not already one for the given Graph URI</param>
        /// <returns></returns>
        public int GetGraphID(Uri graphUri, bool createIfNotExists)
        {
            int ret = _store.LookupGraph(graphUri.ToString());
            if (ret < 0 && createIfNotExists)
            {
                throw new NotSupportedException("The BrightstarRdfProvider does not support creating new graph identifiers.");
            }
            return ret;
        }

        /// <summary>
        /// Given a non-blank Node returns the Node ID
        /// </summary>
        /// <param name="value">Node</param>
        /// <param name="createIfNotExists">Determines whether to create a new Node ID if there is not already one for the given value</param>
        /// <returns></returns>
        public ulong GetID(INode value, bool createIfNotExists)
        {
            ulong ret;
            if (value is ILiteralNode)
            {
                var lit = value as ILiteralNode;
                ret = _store.LookupResource(lit.Value, lit.DataType.ToString(), lit.Language);
            }
            else if (value is IUriNode)
            {
                var u = value as IUriNode;
                ret = _store.LookupResource(u.Uri.ToString());
            }
            else
            {
                throw new NotImplementedException("The BrightsatrRdfProvider cannot resolve the ID for nodes of type " + value.GetType().FullName);
            }
            if (ret == StoreConstants.NullUlong && createIfNotExists)
            {
                throw new NotSupportedException("The BrightstarRdfProvider does not support creating new nodes.");
            }
            return ret;
            
        }

        /// <summary>
        /// Given a Blank Node returns a Graph scoped Node ID
        /// </summary>
        /// <param name="value">Blank Node</param>
        /// <param name="createIfNotExists">Determines whether to create a new Node ID if there is not already one for the given value</param>
        /// <returns></returns>
        public ulong GetBlankNodeID(IBlankNode value, bool createIfNotExists)
        {
            var blankUri = Constants.GeneratedUriPrefix + value.InternalID;
            ulong ret= _store.LookupResource(blankUri);
            if (ret == StoreConstants.NullUlong && createIfNotExists)
            {
                throw new NotSupportedException("The BrightstarRdfProvider does not support creating new nodes.");
            }
            return ret;
        }

        /// <summary>
        /// Given a Blank Node returns a Graph scoped Node ID
        /// </summary>
        /// <param name="value">Blank Node</param>
        /// <returns></returns>
        /// <remarks>
        /// Should function as equivalent to the two argument version with the <strong>createIfNotExists</strong> parameter set to false
        /// </remarks>
        public ulong GetBlankNodeID(IBlankNode value)
        {
            return GetBlankNodeID(value, false);
        }

        /// <summary>
        /// Gets the Node ID that is used to indicate that a Node does not exist in the underlying storage
        /// </summary>
        public ulong NullID
        {
            get { return StoreConstants.NullUlong; }
        }

        /// <summary>
        /// Loads a Graph creating all the Triples with virtual node values
        /// </summary>
        /// <param name="g">Graph to load into</param>
        /// <param name="graphUri">URI of the Graph to load</param>
        public void LoadGraphVirtual(IGraph g, Uri graphUri)
        {
            throw new NotImplementedException();
        }

        private INode MakeVdsNode(IResource resource)
        {
            if (resource.IsLiteral)
            {
                var dataTypeResource = resource.DataTypeId == StoreConstants.NullUlong
                                           ? null
                                           : _store.Resolve(resource.DataTypeId);
                var langResource = resource.LanguageCodeId == StoreConstants.NullUlong
                                       ? null
                                       : _store.Resolve(resource.LanguageCodeId);
                return BrightstarLiteralNode.Create(resource.Value,
                                                    dataTypeResource == null ? null : _store.ResolvePrefixedUri(dataTypeResource.Value),
                                                    langResource == null ? null : langResource.Value);
            }
            var uri = _store.ResolvePrefixedUri(resource.Value);
            return new BrightstarUriNode(new Uri(uri));
        }
    }
}