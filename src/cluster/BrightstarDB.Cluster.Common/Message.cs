using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace BrightstarDB.Cluster.Common
{
    public class Message
    {
        public string Header { get; private set; }
        public string Args { get; private set; }
        private MemoryStream _contentStream;

        public Message(string header, string args, string textContent)
        {
            Header = header;
            Args = args;
            _contentStream = new MemoryStream();
            using(var writer = GetContentWriter())
            {
                writer.Write(textContent);
                writer.Flush();
            }
        }

        public Message(string header, string args, byte[] buff, int offset, int count)
        {
            Header = header;
            Args = args;
            _contentStream = new MemoryStream();
            _contentStream.Write(buff, offset, count);
        }

        public Message(string header, string args)
        {
            Header = header;
            Args = args;
        }

        public Message(string header, string[] args) :this(header, String.Join(" ", args)){}
        public Message(string header, string[] args, byte[] buff, int offset, int count):
            this(header, String.Join(" ", args), buff, offset, count){}

        public TextWriter GetContentWriter()
        {
            if (_contentStream == null)
            {
                _contentStream = new MemoryStream();
            }
            return new StreamWriter(_contentStream, Encoding.UTF8) {NewLine = "\n"};
        }

        public TextReader GetContentReader()
        {
            if (_contentStream == null)
            {
                return TextReader.Null;
            }
            _contentStream.Seek(0, SeekOrigin.Begin);
            return new StreamReader(_contentStream, Encoding.UTF8);
        }

        public Stream GetContentStream()
        {
            return _contentStream ?? (_contentStream = new MemoryStream());
        }

        public void WriteTo(Stream outputStream)
        {
            if (String.IsNullOrEmpty(Header))
            {
                throw new FormatException("Header must not be null or an empty string");
            }
            if (Args == null)
            {
                Args = String.Empty;
            }
            if (Args.Contains('\n'))
            {
                throw new FormatException("Args string must not contain a newline character");
            }
            string headerAndArgs = Header + "\n" + Args + '\n';
            byte[] headerAndArgsBuff = Encoding.UTF8.GetBytes(headerAndArgs);
            int totalLength = headerAndArgsBuff.Length;
            byte[] contentArray = null;
            if (_contentStream != null)
            {
                contentArray = _contentStream.ToArray();
                totalLength += contentArray.Length;
            }
            outputStream.Write(BitConverter.GetBytes(totalLength), 0, 4);
            outputStream.Write(headerAndArgsBuff, 0, headerAndArgsBuff.Length);
            if (contentArray != null)
            {
                outputStream.Write(contentArray, 0, contentArray.Length);
            }

        }

        public static Message Read(Stream inputStream)
        {
            try
            {
                var lenBuff = new byte[4];
                inputStream.Read(lenBuff, 0, 4);
                int len = BitConverter.ToInt32(lenBuff, 0);
                var contentBuff = new byte[len];
                var bytesRead = ReadBytes(inputStream, contentBuff, 0, len, 10);
                //var bytesRead = inputStream.Read(contentBuff, 0, len);
                if (bytesRead < len)
                {
                    throw new FormatException("Truncated message received");
                }


                var headerNewline = IndexOf((byte) '\n', contentBuff, 0);
                var header = Encoding.UTF8.GetString(contentBuff, 0, headerNewline);
                var argsNewline = IndexOf((byte) '\n', contentBuff, headerNewline + 1);
                var args = Encoding.UTF8.GetString(contentBuff, headerNewline + 1, argsNewline - headerNewline - 1);

                if (len > argsNewline)
                {
                    return new Message(header, args, contentBuff, argsNewline + 1, len - argsNewline - 1);
                }
                return new Message(header, args);
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading message", ex);
            }
        }

        private static int ReadBytes(Stream inputStream, byte[] buff, int start, int count, int maxRetries, int retry = 0, int totalBytesRead = 0)
        {
            if (retry == maxRetries) return totalBytesRead;
            var bytesRead = inputStream.Read(buff, start, count);
            totalBytesRead += bytesRead;
            if (bytesRead < count)
            {
                Thread.Sleep(5*retry);
                ReadBytes(inputStream, buff, start+bytesRead, count-bytesRead, retry+1);
            }
            return totalBytesRead;
        }

        public override string ToString()
        {
            throw new Exception("ATtempt to stringify a Message");
        }
        private static int IndexOf(byte ch, byte[] buff, int start)
        {
            for(int i = start; i<buff.Length;i++)
            {
                if (buff[i] == ch) return i;
            }
            return -1;
        }

        public static Message ACK = new Message("ACK", String.Empty);
        public static Message NAK = new Message("NAK", String.Empty);

    }


}