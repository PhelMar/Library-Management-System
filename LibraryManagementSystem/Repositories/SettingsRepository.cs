using LibrarySystem.Core.Database;
using LibrarySystem.Core.Security;
using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace LibrarySystem.Repositories
{
    public class SettingsRepository
    {
        private MySqlConnection GetConnection()
        {
            var conn = DatabaseConnection.GetConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();
            return conn;
        }

        // ── School Year ───────────────────────────────────
        public DataTable GetSchoolYears()
        {
            return GetAll("SELECT id, year_label, is_active FROM school_year ORDER BY year_label DESC");
        }

        public void AddSchoolYear(string yearLabel)
        {
            Execute("INSERT INTO school_year (year_label) VALUES (@p1)",
                new[] { ("@p1", (object)yearLabel) });
        }

        public void UpdateSchoolYear(int id, string yearLabel)
        {
            Execute("UPDATE school_year SET year_label = @p1 WHERE id = @p2",
                new[] { ("@p1", (object)yearLabel), ("@p2", (object)id) });
        }

        public void DeleteSchoolYear(int id)
        {
            Execute("DELETE FROM school_year WHERE id = @p1",
                new[] { ("@p1", (object)id) });
        }

        public void SetActiveSchoolYear(int id)
        {
            using (var conn = GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    using (var cmd = new MySqlCommand(
                        "UPDATE school_year SET is_active = 0", conn, tran))
                        cmd.ExecuteNonQuery();

                    using (var cmd = new MySqlCommand(
                        "UPDATE school_year SET is_active = 1 WHERE id = @id", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    tran.Commit();
                }
                catch { tran.Rollback(); throw; }
            }
        }

        // ── Semester ──────────────────────────────────────
        public DataTable GetSemesters()
        {
            return GetAll("SELECT id, semester_name, is_active FROM semester ORDER BY id");
        }

        public void AddSemester(string name)
        {
            Execute("INSERT INTO semester (semester_name) VALUES (@p1)",
                new[] { ("@p1", (object)name) });
        }

        public void UpdateSemester(int id, string name)
        {
            Execute("UPDATE semester SET semester_name = @p1 WHERE id = @p2",
                new[] { ("@p1", (object)name), ("@p2", (object)id) });
        }

        public void DeleteSemester(int id)
        {
            Execute("DELETE FROM semester WHERE id = @p1",
                new[] { ("@p1", (object)id) });
        }

        public void SetActiveSemester(int id)
        {
            using (var conn = GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    using (var cmd = new MySqlCommand(
                        "UPDATE semester SET is_active = 0", conn, tran))
                        cmd.ExecuteNonQuery();

                    using (var cmd = new MySqlCommand(
                        "UPDATE semester SET is_active = 1 WHERE id = @id", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@id", id);
                        cmd.ExecuteNonQuery();
                    }

                    tran.Commit();
                }
                catch { tran.Rollback(); throw; }
            }
        }

        // ── Category ──────────────────────────────────────
        public DataTable GetCategories()
        {
            return GetAll("SELECT id, category_name FROM category ORDER BY category_name");
        }

        public void AddCategory(string name)
        {
            Execute("INSERT INTO category (category_name) VALUES (@p1)",
                new[] { ("@p1", (object)name) });
        }

        public void UpdateCategory(int id, string name)
        {
            Execute("UPDATE category SET category_name = @p1 WHERE id = @p2",
                new[] { ("@p1", (object)name), ("@p2", (object)id) });
        }

        public void DeleteCategory(int id)
        {
            Execute("DELETE FROM category WHERE id = @p1",
                new[] { ("@p1", (object)id) });
        }

        // ── Course ────────────────────────────────────────
        public DataTable GetCourses()
        {
            return GetAll("SELECT id, course_code, course_name FROM course ORDER BY course_code");
        }

        public void AddCourse(string code, string name)
        {
            Execute("INSERT INTO course (course_code, course_name) VALUES (@p1, @p2)",
                new[] { ("@p1", (object)code), ("@p2", (object)name) });
        }

        public void UpdateCourse(int id, string code, string name)
        {
            Execute("UPDATE course SET course_code = @p1, course_name = @p2 WHERE id = @p3",
                new[] { ("@p1", (object)code), ("@p2", (object)name), ("@p3", (object)id) });
        }

        public void DeleteCourse(int id)
        {
            Execute("DELETE FROM course WHERE id = @p1",
                new[] { ("@p1", (object)id) });
        }

        // ── Year Level ────────────────────────────────────
        public DataTable GetYearLevels()
        {
            return GetAll("SELECT id, level_name FROM year_level ORDER BY id");
        }

        public void AddYearLevel(string name)
        {
            Execute("INSERT INTO year_level (level_name) VALUES (@p1)",
                new[] { ("@p1", (object)name) });
        }

        public void UpdateYearLevel(int id, string name)
        {
            Execute("UPDATE year_level SET level_name = @p1 WHERE id = @p2",
                new[] { ("@p1", (object)name), ("@p2", (object)id) });
        }

        public void DeleteYearLevel(int id)
        {
            Execute("DELETE FROM year_level WHERE id = @p1",
                new[] { ("@p1", (object)id) });
        }

        // ── Librarian + User ──────────────────────────────
        public DataTable GetLibrarians()
        {
            return GetAll(@"
                SELECT
                    l.id, l.full_name, l.shift,
                    l.contact_no, l.email,
                    u.username, u.role,
                    u.id AS user_id
                FROM librarian l
                LEFT JOIN user u ON u.librarian_id = l.id
                ORDER BY l.full_name");
        }

        public void AddLibrarianAndUser(string fullName, string shift, string contactNo,
            string email, string username, string password, string role)
        {
            using (var conn = GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    int librarianId;

                    using (var cmd = new MySqlCommand(@"
                        INSERT INTO librarian (full_name, shift, contact_no, email)
                        VALUES (@name, @shift, @contact, @email)", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@name", fullName);
                        cmd.Parameters.AddWithValue("@shift", shift);
                        cmd.Parameters.AddWithValue("@contact", contactNo);
                        cmd.Parameters.AddWithValue("@email", (object)email ?? DBNull.Value);
                        cmd.ExecuteNonQuery();
                        librarianId = Convert.ToInt32(cmd.LastInsertedId);
                    }

                    using (var cmd = new MySqlCommand(@"
                        INSERT INTO user (librarian_id, username, password, role)
                        VALUES (@libId, @username, @password, @role)", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@libId", librarianId);
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", PasswordHelper.HashPassword(password));
                        cmd.Parameters.AddWithValue("@role", role);
                        cmd.ExecuteNonQuery();
                    }

                    tran.Commit();
                }
                catch { tran.Rollback(); throw; }
            }
        }

        public void UpdateLibrarianAndUser(int librarianId, int userId, string fullName,
            string shift, string contactNo, string email,
            string username, string newPassword, string role)
        {
            using (var conn = GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    using (var cmd = new MySqlCommand(@"
                        UPDATE librarian
                        SET full_name  = @name,
                            shift      = @shift,
                            contact_no = @contact,
                            email      = @email
                        WHERE id = @id", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@name", fullName);
                        cmd.Parameters.AddWithValue("@shift", shift);
                        cmd.Parameters.AddWithValue("@contact", contactNo);
                        cmd.Parameters.AddWithValue("@email", (object)email ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@id", librarianId);
                        cmd.ExecuteNonQuery();
                    }

                    if (!string.IsNullOrWhiteSpace(newPassword))
                    {
                        using (var cmd = new MySqlCommand(@"
                            UPDATE user
                            SET username = @username,
                                password = @password,
                                role     = @role
                            WHERE id = @id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@username", username);
                            cmd.Parameters.AddWithValue("@password", PasswordHelper.HashPassword(newPassword));
                            cmd.Parameters.AddWithValue("@role", role);
                            cmd.Parameters.AddWithValue("@id", userId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cmd = new MySqlCommand(@"
                            UPDATE user
                            SET username = @username,
                                role     = @role
                            WHERE id = @id", conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@username", username);
                            cmd.Parameters.AddWithValue("@role", role);
                            cmd.Parameters.AddWithValue("@id", userId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                }
                catch { tran.Rollback(); throw; }
            }
        }

        public void DeleteLibrarian(int librarianId)
        {
            using (var conn = GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    using (var cmd = new MySqlCommand(
                        "DELETE FROM user WHERE librarian_id = @id", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@id", librarianId);
                        cmd.ExecuteNonQuery();
                    }

                    using (var cmd = new MySqlCommand(
                        "DELETE FROM librarian WHERE id = @id", conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@id", librarianId);
                        cmd.ExecuteNonQuery();
                    }

                    tran.Commit();
                }
                catch { tran.Rollback(); throw; }
            }
        }

        // ── Private helpers ───────────────────────────────
        private DataTable GetAll(string query)
        {
            var dt = new DataTable();
            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
                adapter.Fill(dt);
            return dt;
        }

        private void Execute(string query, (string name, object value)[] parameters)
        {
            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            {
                foreach (var p in parameters)
                    cmd.Parameters.AddWithValue(p.name, p.value);
                cmd.ExecuteNonQuery();
            }
        }
    }
}