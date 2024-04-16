using GtInterviewQ2.DataAccess;
using GtInterviewQ2.Utils;
using System.Data;
using System.Diagnostics;

namespace UnitTests
{
    [TestClass]
    public class TestHarness
    {
        [TestMethod]
        public void TestGetAllGroupValues()
        {
            var sqlHelper = new SqlServerHelper();
            var results = sqlHelper.ExecuteStoredProcedure("q2.GetAllGroupValues");
            Debug.Assert(results != null && results.Rows.Count > 1);
        }

        [TestMethod]
        public void TestGetGroupValues()
        {
            GtInterviewDataProvider provider = new GtInterviewDataProvider();
            var idsToRequest = new int[100];
            idsToRequest.GenerateSpan(0, 100, 0);
            var filteredValues = provider.GetGroupValues(idsToRequest, "NAV");
        }

        #region test sql
        /*
         *
        drop PROCEDURE if exists dbo.EchoInt;
        go
        drop PROCEDURE if exists dbo.EchoInts;
        go
        drop type if exists dbo.IntIdTableType
        go
        CREATE TYPE dbo.IntIdTableType AS TABLE
        (
        IntId INT NOT NULL
        );
        go
        CREATE or alter PROCEDURE dbo.EchoInts
        (
        @Input dbo.IntIdTableType READONLY
        )
        AS
        BEGIN
        SELECT * FROM @Input;
        END
        go
        CREATE or alter PROCEDURE dbo.EchoInt
        (
        @Input int 
        )
        AS
        BEGIN
        SELECT [Id] = @Input;
        END
        go
        *
        */

        #endregion test sql

        [TestMethod]
        public void TestSimpleParametersParsing()
        {
            var input = 57;
            var sqlHelper = new SqlServerHelper();
            var results = sqlHelper.ExecuteStoredProcedure("dbo.EchoInt",
                SqlDataRecordFactory.ToSqlParameter("@Input", input));
            Debug.Assert(results != null && (int)results.Rows[0][0] == input);
        }

        [TestMethod]
        public void TestTableParametersParsing()
        {
            var random = new Random();
            var sqlHelper = new SqlServerHelper();
            var count = random.Next(0, 100);
            var list = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(random.Next());
            }
            DataTable results = sqlHelper.ExecuteStoredProcedure("dbo.EchoInts",
                SqlDataRecordFactory.ToSqlParameter("Input", list));

            Debug.Assert(results != null && results.Rows.Count == count);
            for (int i = 0; i < results.Rows.Count; i++)
            {
                DataRow row = results.Rows[i];
                Debug.Assert((int)row[0] == list[i]);
            }
        }
    }
}