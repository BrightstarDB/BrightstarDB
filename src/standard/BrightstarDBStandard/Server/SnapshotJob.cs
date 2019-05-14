using System;
using BrightstarDB.Storage;

namespace BrightstarDB.Server
{
    internal class SnapshotJob : Job
    {
        private readonly string _destinationStoreName;
        private readonly ulong _sourceCommitPointId;
        private readonly PersistenceType _persistenceType;

        public SnapshotJob(Guid jobId, string label, StoreWorker storeWorker, string destinationStoreName, PersistenceType persistenceType, ulong sourceCommitPointId) : base(jobId, label, storeWorker)
        {
            _destinationStoreName = destinationStoreName;
            _sourceCommitPointId = sourceCommitPointId;
            _persistenceType = persistenceType;
        }

        public override void Run()
        {
            StoreWorker.CreateSnapshot(_destinationStoreName, _persistenceType, _sourceCommitPointId);
        }
    }
}
