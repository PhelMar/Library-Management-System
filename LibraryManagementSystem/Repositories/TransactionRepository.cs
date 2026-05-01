using LibrarySystem.Core.Database;
using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace LibrarySystem.Repositories
{
    public class TransactionRepository
    {
        private const decimal FINE_RATE_PER_DAY = 10.00m;

        private MySqlConnection GetConnection()
        {
            var conn = DatabaseConnection.GetConnection();
            if (conn.State != ConnectionState.Open)
                conn.Open();
            return conn;
        }

        public DataTable SearchStudents(string keyword)
        {
            var dt = new DataTable();

            // Step 1 - get latest school year id
            int latestSchoolYearId = 0;
            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(
                "SELECT id FROM school_year ORDER BY year_label DESC LIMIT 1", conn))
            {
                var result = cmd.ExecuteScalar();
                if (result == null || result == DBNull.Value) return dt;
                latestSchoolYearId = Convert.ToInt32(result);
            }

            // Step 2 - check if 2nd semester exists for that school year
            int resolvedSemesterId = 0;
            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(@"
        SELECT DISTINCT e.semester_id
        FROM enrollment e
        JOIN semester sem ON sem.id = e.semester_id
        WHERE e.school_year_id = @syId
        AND sem.semester_name LIKE '%2nd%'
        LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@syId", latestSchoolYearId);
                var result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    resolvedSemesterId = Convert.ToInt32(result);
            }

            // Step 3 - fallback to 1st semester if 2nd not found
            if (resolvedSemesterId == 0)
            {
                using (var conn = GetConnection())
                using (var cmd = new MySqlCommand(@"
            SELECT DISTINCT semester_id
            FROM enrollment
            WHERE school_year_id = @syId
            LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@syId", latestSchoolYearId);
                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                        resolvedSemesterId = Convert.ToInt32(result);
                }
            }

            if (resolvedSemesterId == 0) return dt;

            // Step 4 - search enrolled students for resolved period
            string query = @"
        SELECT
            s.id            AS student_id,
            s.student_id    AS student_no,
            s.student_name  AS full_name,
            c.course_name,
            yl.level_name   AS year_level,
            e.id            AS enrollment_id,
            sy.year_label   AS school_year,
            sem.semester_name
        FROM enrollment e
        JOIN student    s   ON s.id   = e.student_id
        JOIN course     c   ON c.id   = e.course_id
        JOIN year_level yl  ON yl.id  = e.year_level_id
        JOIN school_year sy ON sy.id  = e.school_year_id
        JOIN semester   sem ON sem.id = e.semester_id
        WHERE e.school_year_id = @schoolYearId
        AND   e.semester_id    = @semesterId
        AND   e.status         = 'enrolled'
        AND (
            s.student_name LIKE @keyword OR
            s.student_id   LIKE @keyword
        )
        ORDER BY s.student_name
        LIMIT 50";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@schoolYearId", latestSchoolYearId);
                cmd.Parameters.AddWithValue("@semesterId", resolvedSemesterId);
                cmd.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                adapter.Fill(dt);
            }

            return dt;
        }

        public DataTable SearchAvailableBooks(string keyword)
        {
            var dt = new DataTable();

            string query = @"
                SELECT
                    b.id,
                    b.book_title,
                    b.author,
                    c.category_name,
                    COALESCE(SUM(
                        CASE
                            WHEN bi.action IN ('add','returned','correction') THEN  bi.qty
                            WHEN bi.action IN ('lost','damaged')              THEN -bi.qty
                            ELSE 0
                        END
                    ), 0) AS current_qty
                FROM books b
                JOIN category c         ON c.id  = b.category_id
                LEFT JOIN book_inventory bi ON bi.book_id = b.id
                WHERE b.is_archived = 0
                AND (
                    b.book_title LIKE @keyword OR
                    b.author     LIKE @keyword
                )
                GROUP BY b.id, b.book_title, b.author, c.category_name
                HAVING current_qty > 0
                ORDER BY b.book_title
                LIMIT 50";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@keyword", $"%{keyword}%");
                adapter.Fill(dt);
            }

            return dt;
        }

        public DataTable GetLibrarians()
        {
            var dt = new DataTable();
            string query = "SELECT id, full_name, shift FROM librarian ORDER BY full_name";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
                adapter.Fill(dt);

            return dt;
        }

        public void BorrowBook(int enrollmentId, int bookId, int librarianId, DateTime dueDate, string remarks)
        {
            using (var conn = GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    string checkStock = @"
                        SELECT COALESCE(SUM(
                            CASE
                                WHEN action IN ('add','returned','correction') THEN  qty
                                WHEN action IN ('lost','damaged')              THEN -qty
                                ELSE 0
                            END
                        ), 0)
                        FROM book_inventory
                        WHERE book_id = @bookId";

                    using (var cmd = new MySqlCommand(checkStock, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@bookId", bookId);
                        int stock = Convert.ToInt32(cmd.ExecuteScalar());
                        if (stock <= 0)
                            throw new Exception("Book is no longer available.");
                    }

                    string insertBorrow = @"
                        INSERT INTO book_transactions
                            (enrollment_id, book_id, librarian_id, borrow_date, due_date, status, remarks)
                        VALUES
                            (@enrollmentId, @bookId, @librarianId, CURRENT_TIMESTAMP, @dueDate, 'borrowed', @remarks)";

                    using (var cmd = new MySqlCommand(insertBorrow, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@enrollmentId", enrollmentId);
                        cmd.Parameters.AddWithValue("@bookId", bookId);
                        cmd.Parameters.AddWithValue("@librarianId", librarianId);
                        cmd.Parameters.AddWithValue("@dueDate", dueDate.Date);
                        cmd.Parameters.AddWithValue("@remarks",
                            string.IsNullOrWhiteSpace(remarks) ? DBNull.Value : (object)remarks);
                        cmd.ExecuteNonQuery();
                    }

                    string inventoryOut = @"
                        INSERT INTO book_inventory (book_id, action, qty, remarks, recorded_by)
                        VALUES (@bookId, 'lost', 1, 'Borrowed by student', @librarianId)";

                    using (var cmd = new MySqlCommand(inventoryOut, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@bookId", bookId);
                        cmd.Parameters.AddWithValue("@librarianId", librarianId);
                        cmd.ExecuteNonQuery();
                    }

                    tran.Commit();
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }

        public void ReturnBook(int transactionId, string remarks)
        {
            using (var conn = GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    int bookId, librarianId, studentId;
                    DateTime dueDate;
                    string currentStatus;

                    string getQuery = @"
                        SELECT bt.book_id, bt.librarian_id, bt.due_date,
                               bt.status, e.student_id
                        FROM book_transactions bt
                        JOIN enrollment e ON e.id = bt.enrollment_id
                        WHERE bt.id = @id
                        LIMIT 1";

                    using (var cmd = new MySqlCommand(getQuery, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@id", transactionId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (!reader.Read())
                                throw new Exception("Transaction not found.");

                            bookId = Convert.ToInt32(reader["book_id"]);
                            librarianId = Convert.ToInt32(reader["librarian_id"]);
                            studentId = Convert.ToInt32(reader["student_id"]);
                            dueDate = Convert.ToDateTime(reader["due_date"]);
                            currentStatus = reader["status"].ToString();
                        }
                    }

                    string updateTran = @"
                        UPDATE book_transactions
                        SET status        = 'returned',
                            returned_date = CURRENT_TIMESTAMP,
                            remarks       = @remarks
                        WHERE id     = @id
                        AND status IN ('borrowed','overdue')";

                    using (var cmd = new MySqlCommand(updateTran, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@id", transactionId);
                        cmd.Parameters.AddWithValue("@remarks",
                            string.IsNullOrWhiteSpace(remarks) ? DBNull.Value : (object)remarks);

                        if (cmd.ExecuteNonQuery() == 0)
                            throw new Exception("Transaction is already returned or does not exist.");
                    }

                    string inventoryQuery = @"
                        INSERT INTO book_inventory (book_id, action, qty, remarks, recorded_by)
                        VALUES (@bookId, 'returned', 1, @inv, @recordedBy)";

                    using (var cmd = new MySqlCommand(inventoryQuery, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@bookId", bookId);
                        cmd.Parameters.AddWithValue("@inv",
                            string.IsNullOrWhiteSpace(remarks) ? "Book returned" : remarks);
                        cmd.Parameters.AddWithValue("@recordedBy", librarianId);
                        cmd.ExecuteNonQuery();
                    }

                    // Fine — only if overdue
                    int daysOverdue = (DateTime.Today - dueDate.Date).Days;
                    if (daysOverdue > 0)
                    {
                        decimal amount = daysOverdue * FINE_RATE_PER_DAY;

                        string upsertFine = @"
                            INSERT INTO fines (transaction_id, student_id, days_overdue, amount, status)
                            VALUES (@transactionId, @studentId, @days, @amount, 'unpaid')
                            ON DUPLICATE KEY UPDATE
                                days_overdue = @days,
                                amount       = @amount";

                        using (var cmd = new MySqlCommand(upsertFine, conn, tran))
                        {
                            cmd.Parameters.AddWithValue("@transactionId", transactionId);
                            cmd.Parameters.AddWithValue("@studentId", studentId);
                            cmd.Parameters.AddWithValue("@days", daysOverdue);
                            cmd.Parameters.AddWithValue("@amount", amount);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    tran.Commit();
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }

        public DataTable GetTransactionsPaged(string search, string statusFilter,
            int page, int pageSize, out int totalCount)
        {
            MarkOverdueAndUpdateFines();

            totalCount = 0;
            var dt = new DataTable();

            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasStatus = !string.IsNullOrWhiteSpace(statusFilter);

            string whereClause = " WHERE 1=1 ";
            if (hasSearch)
                whereClause += @" AND (
                    s.student_name LIKE @search OR
                    s.student_id   LIKE @search OR
                    b.book_title   LIKE @search)";
            if (hasStatus)
                whereClause += " AND bt.status = @status ";

            string fromClause = @"
                FROM book_transactions bt
                JOIN enrollment e   ON e.id   = bt.enrollment_id
                JOIN student s      ON s.id   = e.student_id
                JOIN books b        ON b.id   = bt.book_id
                JOIN librarian lib  ON lib.id = bt.librarian_id";

            using (var conn = GetConnection())
            {
                string countQuery = "SELECT COUNT(*) " + fromClause + whereClause;

                using (var cmd = new MySqlCommand(countQuery, conn))
                {
                    if (hasSearch) cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    if (hasStatus) cmd.Parameters.AddWithValue("@status", statusFilter);
                    totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string query = @"
                    SELECT
                        bt.id,
                        s.student_id    AS student_no,
                        s.student_name,
                        b.book_title,
                        lib.full_name   AS librarian_name,
                        bt.borrow_date,
                        bt.due_date,
                        bt.returned_date,
                        bt.status,
                        bt.remarks "
                    + fromClause + whereClause +
                    @" ORDER BY bt.borrow_date DESC
                    LIMIT @pageSize OFFSET @offset";

                using (var cmd = new MySqlCommand(query, conn))
                using (var adapter = new MySqlDataAdapter(cmd))
                {
                    if (hasSearch) cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    if (hasStatus) cmd.Parameters.AddWithValue("@status", statusFilter);
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                    cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                    adapter.Fill(dt);
                }
            }

            return dt;
        }

        public DataTable GetTransactionById(int transactionId)
        {
            var dt = new DataTable();

            string query = @"
                SELECT
                    bt.id,
                    s.student_id    AS student_no,
                    s.student_name,
                    b.book_title,
                    lib.full_name   AS librarian_name,
                    bt.borrow_date,
                    bt.due_date,
                    bt.returned_date,
                    bt.status,
                    bt.remarks,
                    f.days_overdue,
                    f.amount        AS fine_amount,
                    f.status        AS fine_status,
                    f.paid_at
                FROM book_transactions bt
                JOIN enrollment e   ON e.id   = bt.enrollment_id
                JOIN student s      ON s.id   = e.student_id
                JOIN books b        ON b.id   = bt.book_id
                JOIN librarian lib  ON lib.id = bt.librarian_id
                LEFT JOIN fines f   ON f.transaction_id = bt.id
                WHERE bt.id = @id
                LIMIT 1";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@id", transactionId);
                adapter.Fill(dt);
            }

            return dt;
        }

        public DataTable GetFinesPaged(string search, string statusFilter,
            int page, int pageSize, out int totalCount)
        {
            totalCount = 0;
            var dt = new DataTable();

            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasStatus = !string.IsNullOrWhiteSpace(statusFilter);

            string whereClause = " WHERE 1=1 ";
            if (hasSearch)
                whereClause += @" AND (
                    s.student_name LIKE @search OR
                    s.student_id   LIKE @search OR
                    b.book_title   LIKE @search)";
            if (hasStatus)
                whereClause += " AND f.status = @status ";

            string fromClause = @"
                FROM fines f
                JOIN book_transactions bt ON bt.id  = f.transaction_id
                JOIN enrollment e         ON e.id   = bt.enrollment_id
                JOIN student s            ON s.id   = f.student_id
                JOIN books b              ON b.id   = bt.book_id";

            using (var conn = GetConnection())
            {
                string countQuery = "SELECT COUNT(*) " + fromClause + whereClause;

                using (var cmd = new MySqlCommand(countQuery, conn))
                {
                    if (hasSearch) cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    if (hasStatus) cmd.Parameters.AddWithValue("@status", statusFilter);
                    totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string query = @"
                    SELECT
                        f.id            AS fine_id,
                        s.student_id    AS student_no,
                        s.student_name,
                        b.book_title,
                        bt.due_date,
                        bt.returned_date,
                        f.days_overdue,
                        f.amount,
                        f.status        AS fine_status,
                        f.payment_method,
                        f.paid_at,
                        f.recorded_at "
                    + fromClause + whereClause +
                    @" ORDER BY f.recorded_at DESC
                    LIMIT @pageSize OFFSET @offset";

                using (var cmd = new MySqlCommand(query, conn))
                using (var adapter = new MySqlDataAdapter(cmd))
                {
                    if (hasSearch) cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    if (hasStatus) cmd.Parameters.AddWithValue("@status", statusFilter);
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                    cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                    adapter.Fill(dt);
                }
            }

            return dt;
        }

        public void MarkFinePaid(int fineId)
        {
            using (var conn = GetConnection())
            {
                string query = @"
                    UPDATE fines
                    SET status         = 'paid',
                        payment_method = 'cash',
                        paid_at        = CURRENT_TIMESTAMP
                    WHERE id     = @id
                    AND   status = 'unpaid'";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", fineId);
                    if (cmd.ExecuteNonQuery() == 0)
                        throw new Exception("Fine is already paid or does not exist.");
                }
            }
        }

        public int? GetFineAmountForReturn(int transactionId)
        {
            using (var conn = GetConnection())
            {
                string query = @"
                    SELECT DATEDIFF(CURDATE(), due_date)
                    FROM book_transactions
                    WHERE id = @id
                    AND status IN ('borrowed','overdue')
                    AND due_date < CURDATE()
                    LIMIT 1";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", transactionId);
                    var result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value) return null;

                    int days = Convert.ToInt32(result);
                    return days > 0 ? (int?)(days * (int)FINE_RATE_PER_DAY) : null;
                }
            }
        }

        private void MarkOverdueAndUpdateFines()
        {
            try
            {
                using (var conn = GetConnection())
                {
                    using (var cmd = new MySqlCommand(@"
                        UPDATE book_transactions
                        SET status = 'overdue'
                        WHERE status   = 'borrowed'
                        AND   due_date < CURDATE()", conn))
                        cmd.ExecuteNonQuery();

                    using (var cmd = new MySqlCommand(@"
                        INSERT INTO fines (transaction_id, student_id, days_overdue, amount, status)
                        SELECT
                            bt.id,
                            e.student_id,
                            DATEDIFF(CURDATE(), bt.due_date),
                            DATEDIFF(CURDATE(), bt.due_date) * 10,
                            'unpaid'
                        FROM book_transactions bt
                        JOIN enrollment e ON e.id = bt.enrollment_id
                        WHERE bt.status = 'overdue'
                        AND bt.id NOT IN (SELECT transaction_id FROM fines)
                        ON DUPLICATE KEY UPDATE
                            days_overdue = DATEDIFF(CURDATE(), bt.due_date),
                            amount       = DATEDIFF(CURDATE(), bt.due_date) * 10", conn))
                        cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }
    }
}