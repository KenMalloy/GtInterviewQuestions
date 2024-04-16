using System.Data;

namespace GtInterviewQ2.Utils
{
    public static class DataRowExtension
    {
        public static T? TryGetValue<T>(this DataRow row, string columnName) where T : struct, IConvertible
        {
            if (row == null || !row.Table.Columns.Contains(columnName))
                return default(T?);
            object? convertedObj = default;
            try
            {
                var rawValue = row[columnName];
                if (rawValue == DBNull.Value) return default(T?);

                convertedObj = Convert.ChangeType(rawValue, typeof(T));
            }
            catch (Exception)
            {
                //log
            }
            return (T?)convertedObj;
        }
    }
}
