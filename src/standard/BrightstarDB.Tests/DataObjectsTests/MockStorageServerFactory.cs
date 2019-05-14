using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF;
using VDS.RDF.Configuration;
using VDS.RDF.Storage;
using VDS.RDF.Storage.Management;
using VDS.RDF.Storage.Management.Provisioning;

namespace BrightstarDB.Tests.DataObjectsTests
{
    public class MockStorageServerFactory : IObjectFactory 
    {
        public bool TryLoadObject(IGraph g, INode objNode, Type targetType, out object obj)
        {
            if (targetType == typeof (MockStorageServer))
            {
                var server = new MockStorageServer();
                var storeIdNode = g.CreateUriNode("http://www.dotnetrdf.org/configuration#storeId");
                foreach (var t in g.GetTriplesWithSubjectPredicate(objNode, storeIdNode))
                {
                    var lit = t.Object as ILiteralNode;
                    if (lit != null)
                    {
                        server.AddStore(lit.Value);
                    }
                }
                obj = server;
                return true;
            }
            obj = null;
            return false;
        }

        public bool CanLoadObject(Type t)
        {
            return (t == typeof (MockStorageServer));
        }
    }

    public class MockStorageServer : IStorageServer
    {
        private readonly Dictionary<string, IStorageProvider> _stores = new Dictionary<string, IStorageProvider>();
        private readonly IStoreTemplate _defaultStoreTemplate = new StoreTemplate("default");

        public void AddStore(string store)
        {
            _stores.Add(store, new MockStorageProvider());
        }

        public void Dispose()
        {
            
        }

        public IEnumerable<string> ListStores()
        {
            return _stores.Keys;
        }

        public IStoreTemplate GetDefaultTemplate(string id)
        {
            return _defaultStoreTemplate;
        }

        public IEnumerable<IStoreTemplate> GetAvailableTemplates(string id)
        {
            throw new NotImplementedException();
        }

        public bool CreateStore(IStoreTemplate template)
        {
            if (_stores.ContainsKey(template.ID)) return false;
            _stores.Add(template.ID, new MockStorageProvider());
            return true;
        }

        public void DeleteStore(string storeID)
        {
            if (_stores.ContainsKey(storeID)) _stores.Remove(storeID);

        }

        public IStorageProvider GetStore(string storeID)
        {
            if (_stores.ContainsKey(storeID)) return _stores[storeID];
            throw new Exception("No such store");
        }

        public IOBehaviour IOBehaviour { get; private set; }
    }

    public class MockStorageProvider : IStorageProvider
    {
        public bool IsReady { get; private set; }
        public bool IsReadOnly { get; private set; }
        public IOBehaviour IOBehaviour { get; private set; }
        public bool UpdateSupported { get; private set; }
        public bool DeleteSupported { get; private set; }
        public bool ListGraphsSupported { get; private set; }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void LoadGraph(IGraph g, Uri graphUri)
        {
            throw new NotImplementedException();
        }

        public void LoadGraph(IGraph g, string graphUri)
        {
            throw new NotImplementedException();
        }

        public void LoadGraph(IRdfHandler handler, Uri graphUri)
        {
            throw new NotImplementedException();
        }

        public void LoadGraph(IRdfHandler handler, string graphUri)
        {
            throw new NotImplementedException();
        }

        public void SaveGraph(IGraph g)
        {
            throw new NotImplementedException();
        }

        public void UpdateGraph(Uri graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals)
        {
            throw new NotImplementedException();
        }

        public void UpdateGraph(string graphUri, IEnumerable<Triple> additions, IEnumerable<Triple> removals)
        {
            throw new NotImplementedException();
        }

        public void DeleteGraph(Uri graphUri)
        {
            throw new NotImplementedException();
        }

        public void DeleteGraph(string graphUri)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Uri> ListGraphs()
        {
            throw new NotImplementedException();
        }

        public IStorageServer ParentServer { get; private set; }
    }
}
