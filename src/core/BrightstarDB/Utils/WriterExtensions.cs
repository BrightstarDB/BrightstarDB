#if NETSTANDARD16
using System.IO;
using System.Xml;

namespace BrightstarDB.Utils
{
    public static class WriterExtensions
    {
        public static void Close(this Stream stream)
        {
        }

        public static void Close(this BinaryWriter writer) { }

        public static void Close(this FileStream stream)
        {
        }

        public static void Close(this XmlWriter writer)
        {
        }
    }
}
#endif