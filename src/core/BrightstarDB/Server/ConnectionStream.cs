using System;
using System.IO;
using System.Threading;

namespace BrightstarDB.Server
{
    
    internal class ConnectionStream : Stream
    {
        /// <summary>
        /// Buffer. This is resized if there are more bytes to write than available space.
        /// </summary>
        private byte[] _buffer;

        /// <summary>
        /// The current read position
        /// </summary>
        private int _readPos;

        /// <summary>
        /// The current write position
        /// </summary>
        private int _writePos;
        
        /// <summary>
        /// The number of bytes currently available. When bytes are read this number is decreased, when written
        /// the number is incremented.
        /// </summary>
        private int _available;

        /// <summary>
        /// Semaphore used to signal whenever the producer puts more data into the buffer.
        /// </summary>
        private readonly AutoResetEvent _dataAvailable;


        private bool _producerClosed;

        /// <summary>
        /// Flag that indicates when the producer has finished writing
        /// </summary>
        public bool IsProducerClosed
        {
            get { return _producerClosed; }
            set
            {
                _producerClosed = value; if (_producerClosed)
                {
                    _dataAvailable.Set();
                }
            }
        }

        /// <summary>
        /// Flag that indicates when the consumer has finished reading
        /// </summary>
        public bool IsConsumerClosed { get; set; }

        /// <summary>
        /// Initializes a new connection stream with a default initial buffer size of 1024 bytes
        /// </summary>
        public ConnectionStream()
        {
            _buffer = new byte[1024];
            _dataAvailable = new AutoResetEvent(false);
        }

        public override void Flush()
        {            
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        private int ReadFromBuffer(byte[] outputBuffer, int offset, int bytesToRead)
        {
            if (_available == 0)
            {
                _dataAvailable.WaitOne();
                if (IsProducerClosed && _available == 0) return 0;
            }
            int bytesToProvide;
            lock (this)
            {
                bytesToProvide = Math.Min(bytesToRead, _available);
                if (_readPos + bytesToProvide <= _buffer.Length)
                {
                    // A single copy can pull all the butes
                    Array.ConstrainedCopy(_buffer, _readPos, outputBuffer, offset, bytesToProvide);
                }
                else
                {
                    int bytesToEnd = _buffer.Length - _readPos;
                    Array.ConstrainedCopy(_buffer, _readPos, outputBuffer, offset, bytesToEnd);
                    Array.ConstrainedCopy(_buffer, 0, outputBuffer, offset + bytesToEnd, bytesToProvide - bytesToEnd);
                }
                _available -= bytesToProvide;
                _readPos = (_readPos + bytesToProvide)%_buffer.Length;
            }
            return bytesToProvide;
        }

        private void WriteToBuffer(byte[] dataBuffer, int offset, int bytesToWrite)
        {
            lock (this)
            {
                if (_available + bytesToWrite > _buffer.Length)
                {
                    // Writing would cause an overflow.
                    ExpandBuffer((_available + bytesToWrite));
                }
                if (_writePos + bytesToWrite <= _buffer.Length)
                {
                    Array.ConstrainedCopy(dataBuffer, offset, _buffer, _writePos, bytesToWrite);
                }
                else
                {
                    int bytesToEnd = _buffer.Length - _writePos;
                    Array.ConstrainedCopy(dataBuffer, offset, _buffer, _writePos, bytesToEnd);
                    Array.ConstrainedCopy(dataBuffer, offset + bytesToEnd, _buffer, 0, bytesToWrite - bytesToEnd);
                }
                _available += bytesToWrite;
                _writePos = (_writePos + bytesToWrite)%_buffer.Length;
            }
            _dataAvailable.Set();
        }

        /// <summary>
        /// Lock the buffer, create a new buffer and copy the available bytes into it and replace the old buffer with the new one
        /// </summary>
        /// <param name="newSize">The minimum size to increase the buffer to</param>
        private void ExpandBuffer(int newSize)
        {
            int newBuffSize = Math.Max(_buffer.Length*4, newSize);
            var newBuffer = new byte[newBuffSize];
            if (_readPos + _available <= _buffer.Length)
            {
                Array.ConstrainedCopy(_buffer, _readPos, newBuffer, 0, _available);
            }
            else
            {
                int bytesToEnd = _buffer.Length - _readPos;
                Array.ConstrainedCopy(_buffer, _readPos, newBuffer, 0, bytesToEnd);
                Array.ConstrainedCopy(_buffer, 0, newBuffer, bytesToEnd, _available - bytesToEnd);
            }
            _buffer = newBuffer;
            _readPos = 0;
            _writePos = _available;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (IsProducerClosed && _available == 0)
            {
                // End of stream
                return 0;
            }
            return ReadFromBuffer(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (IsConsumerClosed)
            {
                // no point writing any more if its closed
                return;
            }

            WriteToBuffer(buffer, offset, count);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return true; }
        }

        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }
    }
}