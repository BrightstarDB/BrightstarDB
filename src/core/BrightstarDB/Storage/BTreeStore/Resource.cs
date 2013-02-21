using System.IO;
using System.Linq;
using System.Text;

namespace BrightstarDB.Storage.BTreeStore
{
    internal class Resource : IStorable   
    {
        public ulong Rid;

        public ulong DataTypeResourceId;

        public bool IsLiteral;

        public string LexicalValue;

        public string LanguageCode;

        public int Save(BinaryWriter dataStream, ulong offset = 0ul)
        {
            var count = SerializationUtils.WriteVarint(dataStream, Rid);
            count += SerializationUtils.WriteVarint(dataStream, DataTypeResourceId);

            dataStream.Write(IsLiteral);
            dataStream.Write(LanguageCode == null);

            var langCodeByteCount = 0;
            if (LanguageCode != null)
            {
                var langCodeBytes = Encoding.UTF8.GetBytes(LanguageCode);
                langCodeByteCount = langCodeBytes.Count();
                count += SerializationUtils.WriteVarint(dataStream, (ulong) langCodeByteCount);
                dataStream.Write(langCodeBytes);
            }

            var lexValueBytes = Encoding.UTF8.GetBytes(LexicalValue);
            count += SerializationUtils.WriteVarint(dataStream, (ulong)lexValueBytes.Count());
            dataStream.Write(lexValueBytes);

            return 2 + count + lexValueBytes.Count() + langCodeByteCount;
        }

        public void Read(BinaryReader dataStream)
        {
            Rid = SerializationUtils.ReadVarint(dataStream);

            DataTypeResourceId = SerializationUtils.ReadVarint(dataStream);

            IsLiteral = dataStream.ReadBoolean();
            var isLangCodeNull = dataStream.ReadBoolean();

            if (!isLangCodeNull)
            {
                var langCodeValueByteCount = (int)SerializationUtils.ReadVarint(dataStream);
                LanguageCode = Encoding.UTF8.GetString(dataStream.ReadBytes(langCodeValueByteCount), 0, langCodeValueByteCount);
            }

            var lexValueByteCount = (int)SerializationUtils.ReadVarint(dataStream);
            LexicalValue = Encoding.UTF8.GetString(dataStream.ReadBytes(lexValueByteCount), 0, lexValueByteCount);                
        }

    }
}
