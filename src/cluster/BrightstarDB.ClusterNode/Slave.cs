using System;
using System.Net;
using System.Net.Sockets;

namespace BrightstarDB.ClusterNode
{
    internal class Slave : IComparable
    {
        public IPAddress Address { get; private set; }
        public int Port { get; private set; }
        public TcpClient TcpClient { get; private set; }

        public Slave(IPAddress slaveAddress, int slavePort, TcpClient slaveClient)
        {
            Address = slaveAddress;
            Port = slavePort;
            TcpClient = slaveClient;
        }

        #region Implementation of IComparable

        /// <summary>
        /// Compares the current instance with another object of the same type and returns an integer that indicates whether the current instance precedes, follows, or occurs in the same position in the sort order as the other object.
        /// </summary>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has these meanings: Value Meaning Less than zero This instance precedes <paramref name="obj"/> in the sort order. Zero This instance occurs in the same position in the sort order as <paramref name="obj"/>. Greater than zero This instance follows <paramref name="obj"/> in the sort order. 
        /// </returns>
        /// <param name="obj">An object to compare with this instance. </param><exception cref="T:System.ArgumentException"><paramref name="obj"/> is not the same type as this instance. </exception><filterpriority>2</filterpriority>
        public int CompareTo(object obj)
        {
            if (!(obj is Slave))
            {
                throw new ArgumentException("Object to compare must be a BrightstarDB.ClusterNode.Slave instance");
            }
            var other = obj as Slave;
            var comp = String.CompareOrdinal(Address.ToString(), other.Address.ToString());
            if (comp == 0)
            {
                return Port.CompareTo(other.Port);
            }
            return comp;
        }

        #endregion
    }
}