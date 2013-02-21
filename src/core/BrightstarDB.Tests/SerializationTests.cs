using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BrightstarDB.Tests
{
    [TestClass]
    public class SerializationTests
    {
        [TestMethod]
        public void TestWriteVarint()
        {
            var rdn = new Random();

            TestReadWrite(1);
            TestReadWrite(0);
            TestReadWrite(ulong.MaxValue);

            for (int i = 0; i < 100000000; i++)
            {
                TestReadWrite((ulong)rdn.Next());
            }
        }

        private void TestReadWrite(ulong val)
        {
            ulong val2;
            var buf = WriteVarint(val);
            ReadVarint(buf, out val2);
            Assert.AreEqual(val, val2);            
        }

        private byte[] WriteVarint(ulong value)
        {
            byte[] buffer = new byte[10]; 
            int count = 0;
            int index = 0;
            do
            {
                buffer[index++] = (byte)((value & 0x7F) | 0x80);
                count++;
            } while ((value >>= 7) != 0);
            return buffer;
        }

        private void ReadVarint(byte[] ioBuffer, out ulong value)
        {
            int readPos = 0;
            value = ioBuffer[readPos++];
            if ((value & 0x80) == 0) return;
            value &= 0x7F;

            ulong chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 7;
            if ((chunk & 0x80) == 0) return;

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 14;
            if ((chunk & 0x80) == 0) return;

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 21;
            if ((chunk & 0x80) == 0) return;

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 28;
            if ((chunk & 0x80) == 0) return;

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 35;
            if ((chunk & 0x80) == 0) return;

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 42;
            if ((chunk & 0x80) == 0) return;

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 49;
            if ((chunk & 0x80) == 0) return;

            chunk = ioBuffer[readPos++];
            value |= (chunk & 0x7F) << 56;
            if ((chunk & 0x80) == 0) return;

            chunk = ioBuffer[readPos];
            value |= chunk << 63; // can only use 1 bit from this chunk

            // if ((chunk & ~(ulong)0x01) != 0) throw AddErrorData(new OverflowException(), this);
            return;
        }

    }
}
