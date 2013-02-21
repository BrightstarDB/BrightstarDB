namespace BrightstarDB.ClusterNode
{
    public enum CoreState
    {
        WaitingForMaster,
        WaitingForSlaves,
        RunningMaster,
        SyncToMaster,
        FullSyncToMaster,
        RunningSlave,
        Broken
    }
}
