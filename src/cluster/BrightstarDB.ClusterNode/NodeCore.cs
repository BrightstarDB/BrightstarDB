using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.ServiceModel;
using BrightstarDB.Cluster.Common;
using BrightstarDB.Server;
using BrightstarDB.Storage;
using BrightstarDB.Storage.TransactionLog;

namespace BrightstarDB.ClusterNode
{
    internal class NodeCore : INodeCoreRequestHandler
    {
        private readonly ServerCore _serverCore;
        private readonly NodeComms _nodeComms;
        private List<string> _stores;
        private EndpointAddress _masterAddress;
        private CoreState _state;
        private MasterConfiguration _masterConfiguration;
        private Dictionary<string, StoreTransactionInfo> _storeInfo;

        /// <summary>
        /// Create a new node core
        /// </summary>
        /// <param name="baseLocation"> </param>
        public NodeCore(string baseLocation)
        {
            _serverCore = ServerCoreManager.GetServerCore(baseLocation);
            _serverCore.JobCompleted += OnJobCompleted;
            _nodeComms = new NodeComms(this);
            _nodeComms.SlaveAdded += OnSlaveAdded;
            _storeInfo = new Dictionary<string, StoreTransactionInfo>();
        }

        private void OnSlaveAdded(object sender, SlaveListEventArgs e)
        {
            if (_state == CoreState.WaitingForSlaves)
            {
                if (_nodeComms.GetActiveSlaveCount()>= _masterConfiguration.WriteQuorum)
                {
                    _state = CoreState.RunningMaster;
                }
            }
        }

        /// <summary>
        /// Start up the node process
        /// </summary>
        public void Start(int port)
        {
            AssertStoresLocation();
            InitializeStores();
            
            _nodeComms.Start(port);
            _state = CoreState.WaitingForMaster;
        }

        private void AssertStoresLocation()
        {
            if (Configuration.StoreLocation == null) return;
            var storeLocation = Configuration.StoreLocation;
            if (!Directory.Exists(storeLocation))
            {
                Directory.CreateDirectory(storeLocation);
            }
        }

        private void InitializeStores()
        {
            _stores = new List<string>(_serverCore.ListStores());
            foreach(var s in _stores)
            {
                var transactions = new HashSet<Guid>();
                Guid last = Guid.Empty;
                foreach(var txn in _serverCore.GetRecentTransactions(s, 100, TimeSpan.FromDays(7.0)))
                {
                    if (txn.TransactionStatus == TransactionStatus.CompletedOk)
                    {
                        transactions.Add(txn.JobId);
                        last = txn.JobId;
                    }

                }
                _storeInfo.Add(s, new StoreTransactionInfo(transactions, last));
            }
        }

        /// <summary>
        /// Stop processing and drop out of cluster
        /// </summary>
        public void Stop()
        {
            try
            {
                _serverCore.Shutdown(true);
            }
            catch (Exception)
            {
            }
            try
            {
                _nodeComms.Stop();
            }
            catch (Exception)
            {
            }
        }

        public Guid ProcessTransaction(ClusterUpdateTransaction txn)
        {
            if (_state != CoreState.RunningMaster)
            {
                throw new NotMasterException();
            }

            // Do some validation
            // Check store exists
            txn.JobId = Guid.NewGuid();
            txn.PrevTxnId = _storeInfo[txn.StoreId].Queue(txn.JobId);
            _serverCore.QueueTransaction(txn.JobId, txn.StoreId, txn.Preconditions, txn.Deletes, txn.Inserts);
            _nodeComms.SendTransactionToSlaves(txn);
            return txn.JobId;
        }

        public Guid ProcessUpdate(ClusterSparqlTransaction txn)
        {
            if (_state != CoreState.RunningMaster)
            {
                throw new NotMasterException();
            }

            // Do some validation
            // Check store exists
            txn.JobId = Guid.NewGuid();
            txn.PrevTxnId = _storeInfo[txn.StoreId].Queue(txn.JobId);
            _serverCore.QueueUpdate(txn.JobId, txn.StoreId, txn.Expression);
            _nodeComms.SendTransactionToSlaves(txn);
            return txn.JobId;            
        }

        public bool ProcessSyncTransaction(ClusterTransaction txn)
        {
            if(_state == CoreState.SyncToMaster)
            {
                if (HaveTransaction(txn))
                {
                    // Already have a job with this GUID
                    return true;
                }
                if (HavePrecedingTransaction(txn))
                {
                    QueueTransaction(txn);
                    return true;
                }
            }
            return false;
        }

        public IEnumerable<string> ListStores()
        {
            // todo: should we only do this if we are master?
            return _serverCore.ListStores();
        } 

        private bool HaveTransaction(ClusterTransaction txn)
        {
            // TODO: If not in cached transaction list, check back through txn log
            return _storeInfo[txn.StoreId].HaveTransaction(txn.JobId);
        }

        public bool CreateStore(string storeName)
        {
            try
            {
                _serverCore.CreateStore(storeName, PersistenceType.AppendOnly);
                _stores.Add(storeName);
                _storeInfo.Add(storeName, new StoreTransactionInfo(new HashSet<Guid> {Guid.Empty}, Guid.Empty));
                return true;
            }
            catch (Exception ex)
            {
                // TODO: Log failure
                return false;
            }
        }

        public bool DeleteStore(string args)
        {
            try
            {
                _serverCore.DeleteStore(args, false);
                return true;
            }
            catch (Exception ex)
            {
                // TODO: Log failure
                return false;
            }
        }

        public bool DoesStoreExist(string storeName)
        {
            return _serverCore.DoesStoreExist(storeName);
        }

        public bool ProcessSlaveTransaction(ClusterTransaction txn)
        {
            // check we have store info
            if (!_storeInfo.ContainsKey(txn.StoreId))
            {
                // we need to create the store
                CreateStore(txn.StoreId);
            }

            if (!HavePrecedingTransaction(txn))
            {
                if (_state == CoreState.SyncToMaster)
                {
                    // TODO: buffer this transaction locally to replay when sync is finished
                    return true;
                }
                // We must have missed at least one message. Resync to master
                StartSyncToMaster();
                return false;
            } 
            QueueTransaction(txn);
            return true;
        }

        public string ProcessQuery(string storeId, string query)
        {
            return _serverCore.Query(storeId, query, SparqlResultsFormat.Xml);
        }

        public void ProcessQuery(string storeId, string query, DateTime? ifNotModifiedSince, SparqlResultsFormat format, Stream resultStream)
        {
            _serverCore.Query(storeId, query, ifNotModifiedSince, format, resultStream);
        }

        #region Implementation of INodeCoreRequestHandler

        /// <summary>
        /// A request to accept the proposed endpoint at the master node.
        /// </summary>
        /// <param name="host">The proposed master host name</param>
        /// <param name="port">The proposed master port number</param>
        /// <returns>True is this is an acceptable master</returns>
        public bool SlaveOf(string host, int port)
        {
            _masterAddress = new EndpointAddress("tcp://" + host + ":" + port);
            _nodeComms.OpenTransactionChannel(_masterAddress);
            StartSyncToMaster();
            return true;
        }

        public bool SetMaster(MasterConfiguration masterConfiguration)
        {
            _masterConfiguration = masterConfiguration;
            if (_masterConfiguration.WriteQuorum == 0)
            {
                // Don't need any slaves to enter running state
                _state = CoreState.RunningMaster;
            }
            else
            {
                // Need 1 or more slaves to connect before we enter running state
                _state = CoreState.WaitingForSlaves;
            }
            return true;
        }

        public CoreState GetStatus()
        {
            return _state;
        }

        public bool SyncSlave(Dictionary<string, string> lastTxn, SyncContext context, Func<SyncContext, Message, bool> messageSink )
        {
            bool syncOk = true;
            // Push existing transactions down the pipe
            foreach(var storeId in _stores)
            {
                IEnumerable<ITransactionInfo> transactionsToSend;
                if (lastTxn.ContainsKey(storeId))
                {
                    Guid lastJobId = Guid.Parse(lastTxn[storeId]);
                    transactionsToSend = _serverCore.GetTransactions(storeId).SkipWhile(t => !t.JobId.Equals(lastJobId)).Skip(1);
                    syncOk &= SendTransactions(context, messageSink, transactionsToSend, storeId, lastJobId);
                }
                else
                {
                    // For all stores that aren't listed in the lastTxn dictionary
                    // Send a create store message followed byt all transactions
                    var createMessage = new Message("+store", storeId);
                    if (messageSink(context, createMessage))
                    {
                        syncOk &= SendTransactions(context, messageSink, _serverCore.GetTransactions(storeId), storeId,
                                                   Guid.Empty);
                    }
                    else
                    {
                        syncOk = false;
                    }
                }

                
            }
            
            // For all stores that are listed in lastTxn dictionary and not in our store list
            // send a delete store message
            foreach (var storeId in lastTxn.Keys.Where(s=>!_stores.Contains(s)))
            {
                var deleteMessage = new Message("-store", storeId);
                messageSink(context, deleteMessage);
            }

            return syncOk;
        }

        private bool SendTransactions(SyncContext context, Func<SyncContext, Message, bool> messageSink, IEnumerable<ITransactionInfo> transactionsToSend, string storeId,
                                      Guid prevId)
        {
            foreach (var txn in transactionsToSend.Where(t => t.TransactionStatus == TransactionStatus.CompletedOk))
            {
                if (txn.TransactionType == TransactionType.UpdateTransaction)
                {
                    var job = _serverCore.LoadTransaction(storeId, txn);
                    if (job is UpdateTransaction)
                    {
                        var updateJob = job as UpdateTransaction;
                        var syncTransaction = new ClusterUpdateTransaction
                                                  {
                                                      StoreId = storeId,
                                                      PrevTxnId = prevId,
                                                      JobId = txn.JobId,
                                                      Preconditions = updateJob.Preconditions,
                                                      Deletes = updateJob.DeletePatterns,
                                                      Inserts = updateJob.InsertData
                                                  };
                        if (!messageSink(context, syncTransaction.AsMessage()))
                        {
                            // If message sink stops accepting messages, fail the entire sync process immediately
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else if (txn.TransactionType == TransactionType.SparqlUpdateTransaction)
                {
                    var job = _serverCore.LoadTransaction(storeId, txn);
                    if (job is SparqlUpdateJob)
                    {
                        var sparqlJob = job as SparqlUpdateJob;
                        var syncTransaction = new ClusterSparqlTransaction
                                                  {
                                                      StoreId = storeId,
                                                      PrevTxnId = prevId,
                                                      JobId = txn.JobId,
                                                      Expression = sparqlJob.Expression
                                                  };
                        if (!messageSink(context, syncTransaction.AsMessage()))
                        {
                            // If message sink stops accepting messages, fail the entire sync process immediately
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Handle a endsync message from a master to a slave
        /// </summary>
        /// <param name="syncStatus">Indicates the status of the sync. Should be either OK or FAIL.</param>
        public void SlaveSyncCompleted(string syncStatus)
        {
            if (syncStatus.Equals("OK", StringComparison.OrdinalIgnoreCase))
            {
                _state = CoreState.RunningSlave;
            } else
            {
                // Partial sync failed
                // TODO: start full sync
            }
        }

        #endregion

        private void QueueTransaction(ClusterTransaction txn)
        {
            if (txn is ClusterUpdateTransaction)
            {
                var cut = txn as ClusterUpdateTransaction;
                _storeInfo[txn.StoreId].Queue(txn.JobId);
                _serverCore.QueueTransaction(txn.JobId, txn.StoreId,
                                             cut.Preconditions, cut.Deletes,
                                             cut.Inserts);                
            } else if (txn is ClusterSparqlTransaction)
            {
                var sut = txn as ClusterSparqlTransaction;
                _storeInfo[txn.StoreId].Queue(txn.JobId);
                _serverCore.QueueUpdate(txn.JobId, txn.StoreId, sut.Expression);
            }
        }

        private void StartSyncToMaster()
        {
            _state = CoreState.SyncToMaster;
            var lastTxn = new Dictionary<string, string>();
            foreach (var store in _stores)
            {
                lastTxn.Add(store, GetLastTransactionId(store).ToString());
            }
            _nodeComms.SendSyncToMaster(_masterAddress, lastTxn);
        }

        /// <summary>
        /// Grovel through transaction log for store, get latest txn id
        /// </summary>
        /// <param name="storeId"></param>
        /// <returns></returns>
        private Guid GetLastTransactionId(string storeId)
        {
            return _serverCore.GetTransactions(storeId).First().JobId;
        }

        private bool HavePrecedingTransaction(ClusterTransaction txn)
        {
            return _storeInfo[txn.StoreId].Last.Equals(txn.PrevTxnId);
        }

        private void OnJobCompleted(object sender, JobCompletedEventArgs e)
        {
            var jobStatus = _serverCore.GetJobStatus(e.StoreId, e.CompletedJob.JobId.ToString());
            if (jobStatus != null && jobStatus.JobStatus == JobStatus.TransactionError)
            {
                // Because all transactions should be processable, this indicates a hardware problem or persistent software problem
                _serverCore.Shutdown(true);
                _state = CoreState.Broken;
            }
            _storeInfo[e.StoreId].Commit(e.CompletedJob.JobId);
        }

        public JobExecutionStatus GetJobStatus(string storeName, string jobId)
        {
            return _serverCore.GetJobStatus(storeName, jobId);
        }
    }
}
