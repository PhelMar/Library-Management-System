using LibrarySystem.Core.Database;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows.Forms;

namespace LibrarySystem.Repositories
{
    public class DashboardStats
    {
        public int TotalStudents { get; set; }
        public int TotalBooks { get; set; }
        public int AvailableBooks { get; set; }
        public int StudentsDue { get; set; }
        public int BooksLost { get; set; }
        public int BooksDamaged { get; set; }
    }

    public class DashboardRepository
    {
        private MySqlConnection GetConnection()
        {
            var conn = DatabaseConnection.GetConnection();

            if (conn.State != ConnectionState.Open)
                conn.Open();

            return conn;
        }

        public int GetTotalStudents()
        {
            string query = @"
                SELECT COUNT(DISTINCT e.student_id)
                FROM enrollment e
                INNER JOIN school_year sy ON sy.id = e.school_year_id
                INNER JOIN semester s ON s.id = e.semester_id
                WHERE sy.year_label = (
                    SELECT year_label 
                    FROM school_year 
                    WHERE is_active = 1 
                    ORDER BY id DESC 
                    LIMIT 1
                )
                AND s.semester_name = (
                    SELECT semester_name 
                    FROM semester 
                    WHERE is_active = 1 
                    AND semester_name LIKE '%Semester%'
                    ORDER BY 
                        CASE 
                            WHEN semester_name = '2nd Semester' THEN 2
                            WHEN semester_name = '1st Semester' THEN 1
                            ELSE 3
                        END
                    LIMIT 1
                )";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            {
                var result = cmd.ExecuteScalar();
                return result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        public int GetTotalBooks()
        {
            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM books", conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int GetAvailableBooks()
        {
            string query = @"
                SELECT COUNT(*) 
                FROM books b
                WHERE b.id NOT IN (
                    SELECT DISTINCT book_id 
                    FROM book_transactions 
                    WHERE status IN ('borrowed', 'overdue')
                    AND returned_date IS NULL
                )";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int GetStudentsDue()
        {
            string query = @"
                SELECT COUNT(DISTINCT bt.enrollment_id)
                FROM book_transactions bt
                WHERE bt.status IN ('borrowed', 'overdue')
                AND bt.due_date < CURDATE()
                AND bt.returned_date IS NULL";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int GetBooksLost()
        {
            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(
                "SELECT COALESCE(SUM(qty),0) FROM book_inventory WHERE action = 'lost'", conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public int GetBooksDamaged()
        {
            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(
                "SELECT COALESCE(SUM(qty),0) FROM book_inventory WHERE action = 'damaged'", conn))
            {
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public DashboardStats GetDashboardStats()
        {
            var stats = new DashboardStats();

            using (var conn = GetConnection())
            {
                stats.TotalStudents = GetTotalStudents();
                stats.TotalBooks = GetTotalBooks();
                stats.AvailableBooks = GetAvailableBooks();
                stats.StudentsDue = GetStudentsDue();
                stats.BooksLost = GetBooksLost();
                stats.BooksDamaged = GetBooksDamaged();
            }

            return stats;
        }
    }
}