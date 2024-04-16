using System.Data;

namespace GtInterviewQ2.Utils
{
    public struct SqlParameter(string name, object value, SqlDbType dbType, string typeName = null)
    {
        private readonly string name = name;

        public readonly string GetName()
        {
            return name[0] == '@' ? name : $"@{name}";
        }

        public object Value { get; } = value;
        public SqlDbType DbType { get; } = dbType;
        public string TypeName { get; } = typeName;
    }
}
