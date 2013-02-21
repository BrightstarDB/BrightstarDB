using System;
using System.Collections.Generic;
using System.Data;

namespace BrightstarDB.SdShare
{
    public class DbDataRow : DataRowAdaptor
    {
        readonly DataRow _dr;

        public DbDataRow(DataRow row)
        {
            _dr = row;
        }

        public override IEnumerable<string> ColumnNames
        {
            get
            {
                foreach (DataColumn col in _dr.Table.Columns)
                {
                    yield return col.ColumnName.ToLower();
                }
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