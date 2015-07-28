using System;
using System.IO;
using Windows.Storage;

namespace BrightstarDB.Storage
{
    /// <summary>
    /// Provides a wrapper around the readonly streams provided by WinRT which do not
    /// play nicely with a concurrent read operation. This wrapper catches the oplock
    /// handle closed errors and reopens the read stream. It is not suitable for the 
    /// truly concurrent read/write scenario that the background page writer involves
    /// as WinRT won't let a read stream be reopened while a write stream is open, 
    /// but it does at least allow for interleaved use of streams in a way that is
    /// relatively transparent to the stream user.
    /// </summary>
    internal sealed class PoliteReaderStream : Stream
    {
        private Stream _stream;
        private readonly StorageFile _file;

        public PoliteReaderStream(StorageFile file)
        {
            if (file == null) throw new ArgumentNullException("file");
            _file = file;
        }

        public override void Flush()
        {
            if (_stream != null)
            {
                WrapOplockViolations(() => _stream.Flush());
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return WrapOplockViolations(()=> _stream.Read(buffer, offset, count));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return WrapOplockViolations(() => _stream.Seek(offset, origin));
        }

        public override void SetLength(long value)
        {
            WrapOplockViolations(() => _stream.SetLength(value));
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            WrapOplockViolations(() => _stream.Write(buffer, offset, count));
        }

        public override bool CanRead
        {
            get { return WrapOplockViolations(() => _stream.CanRead); }
        }

        public override bool CanSeek
        {
            get { return WrapOplockViolations(() => _stream.CanSeek); }
        }

        public override bool CanWrite
        {
            get { return WrapOplockViolations(() => _stream.CanWrite); }
        }

        public override long Length
        {
            get { return WrapOplockViolations(() => _stream.Length); }
        }

        public override long Position
        {
            get { return WrapOplockViolations(() => _stream.Position); }
            set { WrapOplockViolations(() => _stream.Position = value); }
        }


        private const Int32 ErrorOplockHandleClosed = unchecked ((Int32) 0x80070323);

        private void WrapOplockViolations(Action action)
        {
            var currentPosition = 0L;
            while (true)
            {
                if (_stream == null)
                {
                    _stream = _file.OpenStreamForReadAsync().Result;
                    _stream.Seek(currentPosition, SeekOrigin.Begin);
                }
                try
                {
                    currentPosition = _stream.Position;
                    action();
                }
                catch (Exception ex)
                {
                    if (ex.HResult != ErrorOplockHandleClosed) throw;
                    _stream = null;
                }
            }
        }

        private T WrapOplockViolations<T>(Func<T> function)
        {
            var currentPosition = 0L;
            while (true)
            {
                if (_stream == null)
                {
                    _stream = _file.OpenStreamForReadAsync().Result;
                    _stream.Seek(currentPosition, SeekOrigin.Begin);
                }
                try
                {
                    // Record the current stream position in case we need to reopen the stream
                    currentPosition = _stream.Position;
                    return function();
                }
                catch (Exception ex)
                {
                    if (ex.HResult != ErrorOplockHandleClosed)
                    {
                        if (ex.InnerException == null || ex.InnerException.HResult != ErrorOplockHandleClosed)
                        {
                            throw;
                        }
                    }
                    _stream = null;
                }
            }
        }


    }
}