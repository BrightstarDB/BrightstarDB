using System;
using System.IO;
#if PORTABLE
using BrightstarDB.Portable.Compatibility;
#endif

namespace BrightstarDB.Storage.Persistence
{
    /// <summary>
    /// A stream abstraction on top of a block provider
    /// </summary>
    public class BlockProviderStream : Stream
    {
        private byte[] _block;
        private long _blockOffset;
        private int _blockLength;
        private readonly IBlockProvider _blockProvider;
        private readonly string _path;
        private long _pos;
        private bool _flushRequired = false;

        /// <summary>
        /// Creates a new stream on top of a block provider
        /// </summary>
        /// <param name="provider">The block provider</param>
        /// <param name="path">The path to the block store</param>
        /// <param name="fileMode">The file mode that the stream should simulate. Currently only FileMode.Open and FileMode.Append are supported</param>
        public BlockProviderStream(IBlockProvider provider, string path, FileMode fileMode = FileMode.Open)
        {
            _blockProvider = provider;
            _path = path;
            _block = new byte[provider.BlockSize];
            _blockOffset = -1;
            switch(fileMode)
            {
                case FileMode.Append:
                    _Seek(0, SeekOrigin.End);
                    break;
                case FileMode.Open:
                    break;
                default:
                    throw new NotSupportedException("BlockProviderStream does not support file mode: " + fileMode);
            }
        }

        #region Overrides of Stream

        /// <summary>
        /// When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        /// </summary>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>2</filterpriority>
        public override void Flush()
        {
            if (_blockOffset == _blockProvider.GetActiveBlockOffset(_path))
            {
                _blockProvider.WriteBlock(_path, _blockOffset, _block, _blockLength);
            }
            _flushRequired = false;
        }

#if PORTABLE
        /// <summary>
        /// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
        /// </summary>
        public void Close()
        {
            if (_flushRequired)
            {
                Flush();
            }
        }
#else
        /// <summary>
        /// Closes the current stream and releases any resources (such as sockets and file handles) associated with the current stream.
        /// </summary>
        /// <filterpriority>1</filterpriority>
        public override void Close()
        {
            if (_flushRequired)
            {
                Flush();
            }
            base.Close();
        }
#endif

        /// <summary>
        /// When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <returns>
        /// The new position within the current stream.
        /// </returns>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter. </param><param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"/> indicating the reference point used to obtain the new position. </param><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return _Seek(offset, origin);
        }

        private long _Seek(long offset, SeekOrigin origin)
        {
            long toPos = 0;
            var length = _blockProvider.GetLength(_path);
            if (origin == SeekOrigin.Begin)
            {
                toPos = offset;
            }
            else if (origin == SeekOrigin.Current)
            {
                toPos = _pos + offset;
            }
            else if (origin == SeekOrigin.End)
            {
                toPos = length + offset;
            }
            /*
            if (toPos < 0 || toPos > length)
            {
                throw new IOException("Invalid file position");
            }
             */
            if (toPos < 0)
            {
                throw new IOException("Invalid file position");
            }
            _pos = toPos;
            return _pos;
        }

        /// <summary>
        /// When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes. </param><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>2</filterpriority>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <returns>
        /// The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
        /// </returns>
        /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between <paramref name="offset"/> and (<paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes read from the current source. </param><param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the data read from the current stream. </param><param name="count">The maximum number of bytes to be read from the current stream. </param><exception cref="T:System.ArgumentException">The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the buffer length. </exception><exception cref="T:System.ArgumentNullException"><paramref name="buffer"/> is null. </exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.NotSupportedException">The stream does not support reading. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        public override int Read(byte[] buffer, int offset, int count)
        {
            try
            {
                LoadBlock(false);
                if (count == 0) return 0;
                int startOffset = (int)(_pos - _blockOffset);
                if (startOffset + count < _blockLength)
                {
                    if (count == 1)
                    {
                        buffer[offset] = _block[startOffset];
                        _pos++;
                    }
                    else
                    {
                        Buffer.BlockCopy(_block, startOffset, buffer, offset, count);
                        _pos += count;
                    }
                    return count;
                }
                if (_blockProvider.IsActive(_path, _blockOffset))
                {
                    var availableByteCount = _blockLength - startOffset;
                    if (availableByteCount < 0)
                    {
                        Logging.LogError(BrightstarEventId.BlockProviderError, 
                            "Read(buff, {0}, {1}) about to fail. Block provider reports block with offset {2} as active block and internal length is {3}. AvailableByteCount={4}",
                            offset, count, _blockOffset, _blockLength, availableByteCount);
                    }
                    Buffer.BlockCopy(_block, startOffset, buffer, offset, availableByteCount);
                    _pos += availableByteCount;
                    return availableByteCount;
                }
                var bytesInThisBlock = _blockLength - startOffset;
                Buffer.BlockCopy(_block, startOffset, buffer, offset, bytesInThisBlock);
                _pos += bytesInThisBlock;
                return bytesInThisBlock + Read(buffer, offset + bytesInThisBlock, count - bytesInThisBlock);
            }
            catch (Exception ex)
            {
                Logging.LogError(BrightstarEventId.BlockProviderError,
                                 "Error in BlockProviderStream.Read. Path is {0}, _pos={1}, _blockOffset={2}, _blockLength={3}. Read called with count={4}. Exception: {5}",
                                 _path, _pos, _blockOffset, _blockLength, count, ex);
                throw;
            }
        }

        private void LoadBlock(bool createIfNotExists)
        {
            long blockOffset = _pos - (_pos%_blockProvider.BlockSize);
            if (_blockOffset != blockOffset)
            {
                _blockOffset = blockOffset;
                var block = _blockProvider.ReadBlock(_path, blockOffset, createIfNotExists);
                _blockLength = block.Length;
                _block = block.Data;
            }
        }

        /// <summary>
        /// When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream. </param><param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream. </param><param name="count">The number of bytes to be written to the current stream. </param><exception cref="T:System.ArgumentException">The sum of <paramref name="offset"/> and <paramref name="count"/> is greater than the buffer length. </exception><exception cref="T:System.ArgumentNullException"><paramref name="buffer"/> is null. </exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="offset"/> or <paramref name="count"/> is negative. </exception><exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.NotSupportedException">The stream does not support writing. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        public override void Write(byte[] buffer, int offset, int count)
        {
            LoadBlock(true);
            var activeOffset = _blockProvider.GetActiveBlockOffset(_path);
            if (_blockOffset < activeOffset)
            {
                throw new IOException(
                    String.Format("Block files are append only. Asked to write to block with offset {0} but active block offset is {1}", _blockOffset, activeOffset));
            }
            var startOffset = (int)(_pos - _blockOffset);
            if (startOffset + count < _blockProvider.BlockSize)
            {
                Buffer.BlockCopy(buffer, offset, _block, startOffset, count);
                _pos = _blockOffset + startOffset + count;
                _blockLength = startOffset + count;
            }
            else
            {
                var bytesToThisBlock = _blockProvider.BlockSize - startOffset;
                Buffer.BlockCopy(buffer, offset, _block, startOffset, bytesToThisBlock);
                _blockProvider.WriteBlock(_path, _blockOffset, _block, _blockProvider.BlockSize);
                _pos += bytesToThisBlock;
                Write(buffer, offset + bytesToThisBlock, count - bytesToThisBlock);
            }
            _flushRequired = true;
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        /// </summary>
        /// <returns>
        /// true if the stream supports reading; otherwise, false.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public override bool CanRead
        {
            get { return true; }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        /// </summary>
        /// <returns>
        /// true if the stream supports seeking; otherwise, false.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public override bool CanSeek
        {
            get { return true; }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        /// </summary>
        /// <returns>
        /// true if the stream supports writing; otherwise, false.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <summary>
        /// When overridden in a derived class, gets the length in bytes of the stream.
        /// </summary>
        /// <returns>
        /// A long value representing the length of the stream in bytes.
        /// </returns>
        /// <exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        public override long Length
        {
            get { return _blockProvider.GetLength(_path); }
        }

        /// <summary>
        /// When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        /// <returns>
        /// The current position within the stream.
        /// </returns>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs. </exception><exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception><exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        public override long Position
        {
            get { return _pos; }
            set { Seek(value, SeekOrigin.Begin); }
        }

        #endregion
    }
}
