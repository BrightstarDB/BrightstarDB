using System;
using System.Collections.Generic;

namespace BrightstarDB.ClusterNode
{
    internal class StoreTransactionInfo
    {
        private readonly HashSet<Guid> _committed;
        private readonly HashSet<Guid> _queued;
        private Guid _last;
        public StoreTransactionInfo(HashSet<Guid> committedTransactions, Guid lastTxn )
        {
            _committed = committedTransactions;
            _last = lastTxn;
            _queued = new HashSet<Guid>();
        }

        public bool HaveTransaction(Guid txnId)
        {
            return _last.Equals(txnId) || _queued.Contains(txnId) || _committed.Contains(txnId);
        }

        public void Commit(Guid txnId)
        {
            _committed.Add(txnId);
            _queued.Remove(txnId);
        }

        public Guid Queue(Guid txnId)
        {
            lock (this)
            {
                Guid ret = _last;
                _queued.Add(txnId);
                _last = txnId;
                return ret;
            }
        }

        public Guid Last { get { return _last; } }
    }
}