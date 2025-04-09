using DBChatPro.Models;
using Microsoft.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace DBChatPro
{
    public class DatabaseManagerService(SqlServerDatabaseService msSqlDb) : IDatabaseService
    {
        public async Task<List<List<string>>> GetDataTable(AIConnection conn, string sqlQuery)
        {
            return await msSqlDb.GetDataTable(conn, sqlQuery);
        }

        public async Task<DatabaseSchema> GenerateSchema(AIConnection conn)
        {
            return await msSqlDb.GenerateSchema(conn);
        }
    }
}
