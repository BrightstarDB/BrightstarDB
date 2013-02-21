using System.Collections.Generic;

namespace BrightstarDB.SdShare
{
    public abstract class DataRowAdaptor
    {
        public abstract IEnumerable<string> ColumnNames { get; }
        public abstract string GetValue(string columnName);
        public abstract string GetXmlSchemaDataType(string columnName);
    }
}