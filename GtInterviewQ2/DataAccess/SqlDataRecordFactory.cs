using GtInterviewQ2.Utils;
using Microsoft.SqlServer.Server;
using System.Data;

namespace GtInterviewQ2.DataAccess
{
    public static class SqlDataRecordFactory
    {
        public static SqlDataRecord ToSqlDataRecord(object value)
        {
            switch (value)
            {
                case int:
                    var intRecord = new SqlDataRecord(new SqlMetaData("IntId", SqlDbType.Int));
                    intRecord.SetInt32(0, (int)value);
                    return intRecord;
            }
            throw new NotImplementedException();
        }

        public static SqlParameter ToSqlParameter(string name, object value)
        {
            SqlDbType sqlType = SqlDbType.Structured;
            DataTable newValue;
            string typeName = null;
            switch (value)
            {
                case int:
                    sqlType = SqlDbType.Int;
                    break;
                case IEnumerable<int>:
                    typeName = "dbo.IntIdTableType";
                    var intValues = value as IEnumerable<int>;
                    newValue = new DataTable(typeName);
                    newValue.Columns.Add("IntId", typeof(int));
                    foreach (int id in intValues)
                    {
                        newValue.Rows.Add(id);
                    }
                    value = newValue;
                    break;
                case string:
                    sqlType = SqlDbType.NVarChar;
                    break;
                case IEnumerable<string>:
                    typeName = "dbo.StringTableType";
                    var stringValue = value as IEnumerable<string>;
                    newValue = new DataTable(typeName);
                    newValue.Columns.Add("String", typeof(string));
                    foreach (string id in stringValue)
                    {
                        newValue.Rows.Add(id);
                    }
                    value = newValue;
                    break;
            }
            var sqlParam = new SqlParameter(name, value, sqlType, typeName);

            return sqlParam;
        }
    }
}
