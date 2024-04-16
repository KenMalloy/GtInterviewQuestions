using GtInterviewQ2.Models;
using GtInterviewQ2.Utils;
using System.Data;

namespace GtInterviewQ2.DataAccess
{
    public class GtInterviewDataProvider : IQ2DataProvider
    {
        public List<GroupableLevelValue> GetAllGroupValues()
        {
            DataTable rawData = new SqlServerHelper().ExecuteStoredProcedure("q2.GetAllGroupValues");
            return ToGroupableLevelValue(rawData);
        }

        public List<GroupableLevelValue> GetGroupValues(IEnumerable<int> groupableLevelIds, params string[] requestedValueLabel)
        {
            DataTable rawData = new SqlServerHelper().ExecuteStoredProcedure("q2.GetGroupValues",
                SqlDataRecordFactory.ToSqlParameter("GroupableLevelId", groupableLevelIds),
                SqlDataRecordFactory.ToSqlParameter("RequestedValues", requestedValueLabel));
            return ToGroupableLevelValue(rawData);
        }

        private static List<GroupableLevelValue> ToGroupableLevelValue(DataTable rawData)
        {
            var result = new List<GroupableLevelValue>();
            foreach (DataRow row in rawData.Rows)
            {
                result.Add(new GroupableLevelValue
                {
                    GroupableLevelId = (int)row["GroupableLevelId"],
                    GroupableValueId = row.TryGetValue<int>("GroupableValueId"),
                    GroupableLevelParentId = row.TryGetValue<int>("GroupableLevelParentId"),
                    GroupLabel = row["GroupLabel"]?.ToString(),
                    ValueLabel = row["ValueLabel"]?.ToString(),
                    DecimalValue = row.TryGetValue<decimal>("DecimalValue"),
                    HierarchyLevel = Convert.ToInt32(row["HeirarchyLevel"])
                });
            }
            return result;
        }
    }
}
