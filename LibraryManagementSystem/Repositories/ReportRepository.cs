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
            c.category_name                                             AS Category,
            COUNT(DISTINCT b.id)                                        AS Total_Books,
            COALESCE((
                SELECT SUM(
                    CASE
                        WHEN bi2.action IN ('add', 'returned') THEN  bi2.qty
                        WHEN bi2.action IN ('lost', 'damaged') THEN -bi2.qty
                        WHEN bi2.action = 'correction'         THEN  bi2.qty
                        ELSE 0
                    END)
                FROM book_inventory bi2
                WHERE bi2.book_id IN (
                    SELECT id FROM books WHERE category_id = c.id AND is_archived = 0)
            ), 0)                                                       AS Available_Stock,
            COALESCE((
                SELECT SUM(bi2.qty)
                FROM book_inventory bi2
                WHERE bi2.action = 'damaged'
                AND bi2.book_id IN (
                    SELECT id FROM books WHERE category_id = c.id AND is_archived = 0)
            ), 0)                                                       AS Damaged,
            COALESCE((
                SELECT SUM(bi2.qty)
                FROM book_inventory bi2
                WHERE bi2.action = 'lost'
                AND bi2.book_id IN (
                    SELECT id FROM books WHERE category_id = c.id AND is_archived = 0)
            ), 0)                                                       AS Lost,
            COALESCE((
                SELECT COUNT(*)
                FROM book_transactions bt2
                JOIN enrollment e ON e.id = bt2.enrollment_id
                JOIN books b2 ON b2.id = bt2.book_id
                WHERE b2.category_id = c.id
                AND b2.is_archived = 0
            ), 0)                                                       AS Total_Borrowed,
            COALESCE((
                SELECT COUNT(*)
                FROM book_transactions bt2
                JOIN enrollment e ON e.id = bt2.enrollment_id
                JOIN books b2 ON b2.id = bt2.book_id
                WHERE b2.category_id = c.id
                AND bt2.status = 'returned'
                AND b2.is_archived = 0
            ), 0)                                                       AS Total_Returned,
            COUNT(DISTINCT CASE WHEN b.is_archived = 1 THEN b.id END)  AS Archived
        FROM category c
        LEFT JOIN books b ON b.category_id = c.id
        GROUP BY c.id, c.category_name
        ORDER BY c.category_name";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
                adapter.Fill(dt);

            return dt;
        }


        public DataTable GetArchivedBooksReport()
        {
            var dt = new DataTable();
            string query = @"
        SELECT
            b.book_title                    AS Book_Title,
            b.author                        AS Author,
            c.category_name                 AS Category,
            b.isbn                          AS ISBN,
            COALESCE(SUM(
                CASE
                    WHEN bi.action IN ('add','returned','correction') THEN  bi.qty
                    WHEN bi.action IN ('lost','damaged')              THEN -bi.qty
                    ELSE 0
                END
            ), 0)                           AS Last_Stock,
            COUNT(bt.id)                    AS Times_Borrowed,
            DATE(b.created_at)              AS Date_Added
        FROM books b
        JOIN category c             ON c.id      = b.category_id
        LEFT JOIN book_inventory bi ON bi.book_id = b.id
        LEFT JOIN book_transactions bt ON bt.book_id = b.id
        WHERE b.is_archived = 1
        GROUP BY b.id, b.book_title, b.author, c.category_name, b.isbn, b.created_at
        ORDER BY c.category_name, b.book_title";

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

        public DataTable GetAttendanceReport(int schoolYearId, int semesterId, int? month)
        {
            var dt = new DataTable();

            string monthFilter = month.HasValue
                ? "AND MONTH(la.time_in) = @month"
                : "";

            string query = $@"
        SELECT
            s.student_id                                        AS Student_No,
            s.student_name                                      AS Student_Name,
            c.course_name                                       AS Course,
            yl.level_name                                       AS Year_Level,
            DATE(la.time_in)                                    AS Date,
            TIME_FORMAT(la.time_in, '%h:%i:%s %p')             AS Time_In,
            CASE
                WHEN la.time_out IS NULL THEN 'Active'
                ELSE TIME_FORMAT(la.time_out, '%h:%i:%s %p')
            END                                                 AS Time_Out,
            COALESCE(
                TIME_FORMAT(TIMEDIFF(la.time_out, la.time_in), '%H:%i:%s'),
                '--:--:--'
            )                                                   AS Duration,
            CASE
                WHEN la.time_out IS NULL THEN 'In Library'
                ELSE 'Checked Out'
            END                                                 AS Status
        FROM library_attendance la
        INNER JOIN enrollment e   ON la.enrollment_id = e.id
        INNER JOIN student s      ON la.student_id    = s.id
        INNER JOIN course c       ON e.course_id      = c.id
        INNER JOIN year_level yl  ON e.year_level_id  = yl.id
        INNER JOIN semester sem   ON e.semester_id    = sem.id
        INNER JOIN school_year sy ON e.school_year_id = sy.id
        WHERE e.school_year_id = @schoolYearId
        AND   e.semester_id    = @semesterId
        {monthFilter}
        ORDER BY la.time_in DESC";

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

        public DataTable GetAttendanceSummaryReport(int schoolYearId, int semesterId, int? month)
        {
            var dt = new DataTable();

            string monthFilter = month.HasValue
                ? "AND MONTH(la.time_in) = @month"
                : "";

            string query = $@"
        SELECT
            s.student_id                            AS Student_No,
            s.student_name                          AS Student_Name,
            c.course_name                           AS Course,
            yl.level_name                           AS Year_Level,
            COUNT(la.id)                            AS Total_Visits,
            SUM(CASE WHEN la.time_out IS NOT NULL
                THEN TIME_TO_SEC(TIMEDIFF(la.time_out, la.time_in))
                ELSE 0 END) DIV 3600                AS Total_Hours
        FROM library_attendance la
        INNER JOIN enrollment e   ON la.enrollment_id = e.id
        INNER JOIN student s      ON la.student_id    = s.id
        INNER JOIN course c       ON e.course_id      = c.id
        INNER JOIN year_level yl  ON e.year_level_id  = yl.id
        INNER JOIN semester sem   ON e.semester_id    = sem.id
        INNER JOIN school_year sy ON e.school_year_id = sy.id
        WHERE e.school_year_id = @schoolYearId
        AND   e.semester_id    = @semesterId
        {monthFilter}
        GROUP BY s.id, s.student_id, s.student_name, c.course_name, yl.level_name
        ORDER BY Total_Visits DESC";

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