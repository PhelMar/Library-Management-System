using LibrarySystem.Core.Database;
using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace LibrarySystem.Repositories
{
    public class ReportRepository
    {
        private MySqlConnection GetConnection()
        {
            var conn = DatabaseConnection.GetConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();
            return conn;
        }

        public DataTable GetBooksReport()
        {
            var dt = new DataTable();
            string query = @"
                SELECT
                    c.category_name                         AS Category,
                    COUNT(b.id)                             AS Total_Books,
                    COALESCE(SUM(
                        CASE
                            WHEN bi.action IN ('add','returned','correction') THEN  bi.qty
                            WHEN bi.action IN ('lost','damaged')              THEN -bi.qty
                            ELSE 0
                        END
                    ), 0)                                   AS Available_Stock,
                    COUNT(CASE WHEN b.is_archived = 1 THEN 1 END) AS Archived
                FROM books b
                JOIN category c ON c.id = b.category_id
                LEFT JOIN book_inventory bi ON bi.book_id = b.id
                GROUP BY c.id, c.category_name
                ORDER BY c.category_name";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
                adapter.Fill(dt);

            return dt;
        }

        public DataTable GetMostBorrowedBooks()
        {
            var dt = new DataTable();
            string query = @"
                SELECT
                    b.book_title    AS Book_Title,
                    b.author        AS Author,
                    c.category_name AS Category,
                    COUNT(bt.id)    AS Times_Borrowed
                FROM book_transactions bt
                JOIN books b    ON b.id = bt.book_id
                JOIN category c ON c.id = b.category_id
                GROUP BY b.id, b.book_title, b.author, c.category_name
                ORDER BY Times_Borrowed DESC
                LIMIT 10";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
                adapter.Fill(dt);

            return dt;
        }

        public DataTable GetBorrowReturnReport(int schoolYearId, int semesterId, int? month)
        {
            var dt = new DataTable();

            string monthFilter = month.HasValue
                ? "AND MONTH(bt.borrow_date) = @month"
                : "";

            string query = $@"
                SELECT
                    s.student_id        AS Student_No,
                    s.student_name      AS Student_Name,
                    b.book_title        AS Book_Title,
                    lib.full_name       AS Librarian,
                    DATE(bt.borrow_date)AS Borrow_Date,
                    bt.due_date         AS Due_Date,
                    COALESCE(DATE(bt.returned_date), 'Not Returned') AS Returned_Date,
                    bt.status           AS Status
                FROM book_transactions bt
                JOIN enrollment e   ON e.id   = bt.enrollment_id
                JOIN student s      ON s.id   = e.student_id
                JOIN books b        ON b.id   = bt.book_id
                JOIN librarian lib  ON lib.id = bt.librarian_id
                WHERE e.school_year_id = @schoolYearId
                AND   e.semester_id    = @semesterId
                {monthFilter}
                ORDER BY bt.borrow_date DESC";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@schoolYearId", schoolYearId);
                cmd.Parameters.AddWithValue("@semesterId", semesterId);
                if (month.HasValue)
                    cmd.Parameters.AddWithValue("@month", month.Value);
                adapter.Fill(dt);
            }

            return dt;
        }

        public DataTable GetFinesReport(int schoolYearId, int semesterId, int? month)
        {
            var dt = new DataTable();

            string monthFilter = month.HasValue
                ? "AND MONTH(f.recorded_at) = @month"
                : "";

            string query = $@"
                SELECT
                    s.student_id        AS Student_No,
                    s.student_name      AS Student_Name,
                    b.book_title        AS Book_Title,
                    bt.due_date         AS Due_Date,
                    f.days_overdue      AS Days_Overdue,
                    f.amount            AS Fine_Amount,
                    f.status            AS Payment_Status,
                    COALESCE(DATE(f.paid_at), '--') AS Paid_Date
                FROM fines f
                JOIN book_transactions bt ON bt.id  = f.transaction_id
                JOIN enrollment e         ON e.id   = bt.enrollment_id
                JOIN student s            ON s.id   = f.student_id
                JOIN books b              ON b.id   = bt.book_id
                WHERE e.school_year_id = @schoolYearId
                AND   e.semester_id    = @semesterId
                {monthFilter}
                ORDER BY f.recorded_at DESC";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@schoolYearId", schoolYearId);
                cmd.Parameters.AddWithValue("@semesterId", semesterId);
                if (month.HasValue)
                    cmd.Parameters.AddWithValue("@month", month.Value);
                adapter.Fill(dt);
            }

            return dt;
        }

        public DataTable GetOverdueReport(int schoolYearId, int semesterId, int? month)
        {
            var dt = new DataTable();

            string monthFilter = month.HasValue
                ? "AND MONTH(bt.due_date) = @month"
                : "";

            string query = $@"
                SELECT
                    s.student_id        AS Student_No,
                    s.student_name      AS Student_Name,
                    b.book_title        AS Book_Title,
                    DATE(bt.borrow_date)AS Borrow_Date,
                    bt.due_date         AS Due_Date,
                    DATEDIFF(CURDATE(), bt.due_date) AS Days_Overdue,
                    COALESCE(f.amount, 0) AS Fine_Amount,
                    COALESCE(f.status, 'no fine') AS Fine_Status
                FROM book_transactions bt
                JOIN enrollment e   ON e.id   = bt.enrollment_id
                JOIN student s      ON s.id   = e.student_id
                JOIN books b        ON b.id   = bt.book_id
                LEFT JOIN fines f   ON f.transaction_id = bt.id
                WHERE bt.status IN ('overdue','borrowed')
                AND   bt.due_date < CURDATE()
                AND   e.school_year_id = @schoolYearId
                AND   e.semester_id    = @semesterId
                {monthFilter}
                ORDER BY Days_Overdue DESC";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@schoolYearId", schoolYearId);
                cmd.Parameters.AddWithValue("@semesterId", semesterId);
                if (month.HasValue)
                    cmd.Parameters.AddWithValue("@month", month.Value);
                adapter.Fill(dt);
            }

            return dt;
        }

        public DataTable GetSchoolYears()
        {
            var dt = new DataTable();
            string query = "SELECT id, year_label AS display_name FROM school_year ORDER BY year_label DESC";
            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
                adapter.Fill(dt);
            return dt;
        }

        public DataTable GetSemesters()
        {
            var dt = new DataTable();
            string query = "SELECT id, semester_name AS display_name FROM semester ORDER BY id";
            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
                adapter.Fill(dt);
            return dt;
        }
    }
}