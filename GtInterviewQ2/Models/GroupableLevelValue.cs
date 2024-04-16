namespace GtInterviewQ2.Models
{
    public class GroupableLevelValue
    {
        public int GroupableLevelId { get; set; }
        public int? GroupableValueId { get; set; }
        public int? GroupableLevelParentId { get; set; }
        public string GroupLabel { get; set; }
        public string ValueLabel { get; set; }
        public decimal? DecimalValue { get; set; }
        public int HierarchyLevel { get; set; }
    }
}
