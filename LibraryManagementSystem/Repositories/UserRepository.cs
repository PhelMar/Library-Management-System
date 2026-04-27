using LibrarySystem.Core.Database;
using LibrarySystem.Models;
using MySql.Data.MySqlClient;
using System;
using System.Data.SqlClient;

namespace LibrarySystem.Repositories
{
    public class UserRepository
    {
        public User GetByUsername(string username)
        {
            User user = null;

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    // Always use parameterized queries — never string concat
                    string query = "SELECT id, librarian_id, username, password, role " +
                                   "FROM user WHERE username = @username LIMIT 1";

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                user = new User
                                {
                                    Id = reader.GetInt32("id"),
                                    LibrarianId = reader.GetInt32("librarian_id"),
                                    Username = reader.GetString("username"),
                                    Password = reader.GetString("password"),
                                    Role = reader.GetString("role")
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or rethrow — let the form handle the message
                throw new Exception("Database error during login: " + ex.Message);
            }

            return user;
        }
    }
}