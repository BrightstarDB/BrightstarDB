using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;
using BrightstarDB.Cluster.Common;

namespace BrightstarDB.ClusterNode
{
    internal class NodeComms
    {
        private Thread _listenThread;
        private bool _keepRunning;
        private TcpListener _tcpListener;
        private readonly INodeCoreRequestHandler _requestHandler;
        private readonly List<Slave> _slaves;
        private MessageReader _transactionMessageReader;

        public delegate void SlaveListEvent(object sender, SlaveListEventArgs e);

        public event SlaveListEvent SlaveAdded;
        public event SlaveListEvent SlaveStatusChanged;
        public event SlaveListEvent SlaveRemoved;

        public NodeComms(INodeCoreRequestHandler requestHandler)
        {
            _requestHandler = requestHandler;
            _slaves = new List<Slave>();
        }

        public void Start(int port)
        {
            _keepRunning = true;
            _tcpListener = new TcpListener(IPAddress.Any, port);
            _listenThread = new Thread(ListenForConnections);
            _listenThread.Start();
        }

        public void Stop()
        {
            _keepRunning = false;
            if (_tcpListener != null)
            {
                _tcpListener.Stop();
            }
            if (_listenThread != null)
            {
                _listenThread.Abort();
            }
        }


        private void ListenForConnections()
        {
            _tcpListener.Start();
            while (_keepRunning)
            {
                var client = _tcpListener.AcceptTcpClient();
                var clientThread = new Thread(HandleClientRequest);
                clientThread.Start(client);
            }
        }

        private void HandleClientRequest(object client)
        {
            var tcpClient = (TcpClient)client;
            var clientStream = tcpClient.GetStream();

            // all message have a single line header and then data.
            var msg = Message.Read(clientStream);
            switch (msg.Header.ToLowerInvariant())
            {
                case "ping":
                    SendMessage(clientStream, new Message("pong", String.Empty));
                    break;
                case "master":
                    {
                        var configuration = MasterConfiguration.FromMessage(msg);
                        bool handledOk = _requestHandler.SetMaster(configuration);
                        SendMessage(clientStream, handledOk ? Message.ACK : Message.NAK);
                        break;
                    }
                case "slaveof":
                    {
                        var args = msg.Args.Split(' ');
                        bool handledOk = _requestHandler.SlaveOf(args[0], Int32.Parse(args[1]));
                        SendMessage(clientStream, handledOk ? Message.ACK : Message.NAK);
                        break;
                    }
                case "sync":
                    {
                        var dict = new Dictionary<string, string>();
                        using (var reader = msg.GetContentReader())
                        {
                            string line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                var args = line.Split('|');
                                var storeName = args[0];
                                var txnId = args[1];
                                dict.Add(storeName, txnId);
                            }
                        }

                        var syncOk = _requestHandler.SyncSlave(
                            dict,
                            new SyncContext {Connection = tcpClient},
                            (context, message) => SendMessageAndWaitForAck(context.Connection, message));

                        // Send endsync message
                        var endsync = new Message("endsync", new[] { syncOk ? "OK" : "FAIL" }); // TODO: Body could provide some check - e.g. # messages sent in sync ?
                        SendMessage(clientStream, endsync);
                        break;
                    }
                case "listen":
                    {
                        AddSlave(tcpClient);
                        break;
                    }
            }
        }

        private void SendMessage(Stream networkStream, Message messageToSend)
        {
            messageToSend.WriteTo(networkStream);
            networkStream.Flush();
        }

        private bool SendMessageAndWaitForAck(TcpClient socket, Message messageToSend)
        {
            var stream = socket.GetStream();
            messageToSend.WriteTo(stream);
            stream.Flush();

            var message = Message.Read(stream);
            return message.Header.Equals("ACK", StringComparison.OrdinalIgnoreCase);
        }

        private void AddSlave(TcpClient slave)
        {
            var slaveAddress = ((IPEndPoint) slave.Client.RemoteEndPoint).Address;
            var slavePort = ((IPEndPoint) slave.Client.RemoteEndPoint).Port;
            var slaveRecord = new Slave(slaveAddress, slavePort, slave);
            _slaves.Add(slaveRecord);
            if (SlaveAdded != null) SlaveAdded(this, new SlaveListEventArgs(slaveRecord));
        }

        public void SendTransactionToSlaves(ClusterTransaction txn)
        {
            var msg = txn.AsMessage();
            foreach(var slave in _slaves)
            {
                msg.WriteTo(slave.TcpClient.GetStream());
            }
        }

        /// <summary>
        /// Construct a sync message that lists all the stores we have and the last commited transaction id for each one
        /// then start a reader to process the updates the master will send to us
        /// </summary>
        /// <param name="masterAddress"></param>
        /// <param name="lastTxn"></param>
        public void SendSyncToMaster(EndpointAddress masterAddress, Dictionary<string, string> lastTxn)
        {
            var msg = new Message("sync", String.Empty);
            using(var writer = msg.GetContentWriter())
            {
                foreach (var entry in lastTxn)
                {
                    writer.WriteLine("{0}|{1}", entry.Key, entry.Value);
                }
            }

            var socket = new TcpClient(masterAddress.Uri.Host, masterAddress.Uri.Port);
            var socketStream = socket.GetStream();

            msg.WriteTo(socketStream);
            socketStream.Flush();

            var messageReader = new MessageReader(socket, HandleSyncMessage);
            messageReader.Start();
        }

        /// <summary>
        /// Handler function for messages that can be received by the slave 
        /// through the initial sync connection with the master
        /// </summary>
        /// <param name="msg">The message received</param>
        /// <returns>The response to send to the message</returns>
        private Response HandleSyncMessage(Message msg)
        {
            bool handledOk = false;
            switch (msg.Header.ToLowerInvariant())
            {
                case "txn":
                    var txn = ClusterUpdateTransaction.FromMessage(msg);
                    handledOk = _requestHandler.ProcessSyncTransaction(txn);
                    break;
                case "+store":
                    handledOk = _requestHandler.CreateStore(msg.Args);
                    break;
                case "-store":
                    handledOk = _requestHandler.DeleteStore(msg.Args);
                    break;
                case "endsync":
                    var syncStatus = msg.Args.Split(' ')[0];
                    _requestHandler.SlaveSyncCompleted(syncStatus);
                    handledOk = true;
                    break;

            }
            if (handledOk)
            {
                return new Response(Message.ACK);
            }
            return new Response(Message.NAK, true);
        }

        public int GetActiveSlaveCount()
        {
            // TODO: Should check table of slave status to only return ones that we haven't detected connection issues for
            return _slaves.Count;
        }

        /// <summary>
        /// Create a channel to the master for receiving new transactions
        /// </summary>
        /// <param name="masterAddress"></param>
        public void OpenTransactionChannel(EndpointAddress masterAddress)
        {
            var tcpClient = new TcpClient(masterAddress.Uri.Host, masterAddress.Uri.Port);
            var msg = new Message("listen", String.Empty);
            var stream = tcpClient.GetStream();
            msg.WriteTo(stream);
            stream.Flush();

            _transactionMessageReader = new MessageReader(tcpClient, HandleTransactionMessage);
            _transactionMessageReader.Start();
        }

        private Response HandleTransactionMessage(Message txnMessage)
        {
            bool handledOk = false;
            switch (txnMessage.Header.ToLowerInvariant())
            {
                case "txn":
                    var updateTransaction = ClusterUpdateTransaction.FromMessage(txnMessage);
                    handledOk = _requestHandler.ProcessSlaveTransaction(updateTransaction);
                    break;
                case "spu":
                    var sparqlTransaction = ClusterSparqlTransaction.FromMessage(txnMessage);
                    handledOk = _requestHandler.ProcessSlaveTransaction(sparqlTransaction);
                    break;
                case "+store":
                    handledOk = _requestHandler.CreateStore(txnMessage.Args);
                    break;
                case "-store":
                    handledOk = _requestHandler.DeleteStore(txnMessage.Args);
                    break;
            }
            return new Response(handledOk ? Message.ACK : Message.NAK);
        }
    }
}