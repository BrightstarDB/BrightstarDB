#if NETSTANDARD16
using System.IO;
using System.Xml;

namespace BrightstarDB.Utils
{
    public static class WriterExtensions
    {
        public static void Close(this Stream stream)
        {
            stream.Dispose();
        }

        public static void Close(this BinaryWriter writer)
        {
            writer.BaseStream.Close();
            writer.Dispose();
        }

        public static void Close(this FileStream stream)
        {
            stream.Dispose();
        }

        public static void Close(this StreamWriter writer)
        {
            writer.BaseStream.Close();
            writer.Dispose();
        }

        public static void Close(this StreamReader reader)
        {
            reader.Dispose();
        }

        public static void Close(this XmlWriter writer)
        {
            writer.Dispose();
        }
    }
}
#endif