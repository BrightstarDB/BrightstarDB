using System;
using System.Collections.Generic;
using System.Data.Odbc;

namespace BrightstarDB.SdShare
{
    public class DbReaderDataRow : DataRowAdaptor
    {
        private readonly OdbcDataReader _dr;
        private readonly IEnumerable<string> _columnNames;

        public DbReaderDataRow(OdbcDataReader reader, List<string> columnNames)
        {
            _dr = reader;
            _columnNames = columnNames;
        }

        public override IEnumerable<string> ColumnNames
        {
            get
            {
                return _columnNames;
            }
        }

        public override string GetValue(string columnName)
        {
            return _dr[columnName].ToString();
        }

        public override string GetXmlSchemaDataType(string columnName)
        {
            throw new NotImplementedException();
        }
    }
}