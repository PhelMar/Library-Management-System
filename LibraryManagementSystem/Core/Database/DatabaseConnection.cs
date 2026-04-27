using MySql.Data.MySqlClient;
using System.Configuration;
using System.Data.SqlClient;

namespace LibrarySystem.Core.Database
{
    public static class DatabaseConnection
    {
        private static readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["LibraryDB"].ConnectionString;

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}