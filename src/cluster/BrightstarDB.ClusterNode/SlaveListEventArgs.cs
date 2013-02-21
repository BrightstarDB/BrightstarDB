using System;

namespace BrightstarDB.ClusterNode
{
    internal class SlaveListEventArgs : EventArgs
    {
        public Slave Slave { get; private set; }
        public SlaveListEventArgs(Slave slave) : base()
        {
            Slave = slave;
        }
    }
}