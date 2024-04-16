using System.Data;
using System.Data.SqlClient;

namespace GtInterviewQ2.Utils
{
    public class SqlServerHelper
    {
        private const string MyConnectionString = "Data Source=KenDevBox\\SQLEXPRESS;Initial Catalog=GtInterview;Integrated Security=True";
        private readonly string _connectionString;

        public SqlServerHelper(string connectionString = MyConnectionString)
        {
            _connectionString = connectionString;
        }

        public DataTable ExecuteStoredProcedure(string storedProcedureName, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand(storedProcedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.GetName(), param.Value);
                    }

                    var adapter = new SqlDataAdapter(command);
                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);

                    return dataTable;
                }
            }
        }
    }
}
