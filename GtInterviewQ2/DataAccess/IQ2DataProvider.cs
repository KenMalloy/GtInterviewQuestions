using GtInterviewQ2.Models;

namespace GtInterviewQ2.DataAccess
{
    internal interface IQ2DataProvider
    {
        List<GroupableLevelValue> GetAllGroupValues();
        //IEnumerable<LevelMetadata> GetColumnNames();
        List<GroupableLevelValue> GetGroupValues(IEnumerable<int> enumerable, params string[] strings);
    }
}
