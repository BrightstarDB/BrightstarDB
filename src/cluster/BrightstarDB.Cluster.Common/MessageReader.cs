using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace BrightstarDB.Cluster.Common
{
    public class MessageReader
    {
        private readonly TcpClient _socket;
        private readonly NetworkStream _socketStream;
        private readonly Func<Message, Response> _messageHandler;
        private Thread _readerThread;

        public MessageReader(TcpClient socket, Func<Message, Response> messageHandler)
        {
            _socket = socket;
            _socketStream = _socket.GetStream();
            _messageHandler = messageHandler;
        }

        public void Start()
        {
            _readerThread = new Thread(ReadMessages);
            _readerThread.Start();
        }

        private void ReadMessages()
        {
            bool keepGoing = _socket.Connected;
            while (keepGoing)
            {
                if (_socketStream.DataAvailable)
                {
                    var msg = Message.Read(_socketStream);
                    var response = _messageHandler(msg);
                    response.ResponseMessage.WriteTo(_socketStream);
                    _socketStream.Flush();
                    keepGoing = _socket.Connected && !response.CloseConnection;
                }
                else
                {
                    Thread.Sleep(10); // Wait a bit for new data
                }
            }
            // Cleanup resources
            _socketStream.Close();
            _socketStream.Dispose();
            _socket.Close();
        }

        
    }
}