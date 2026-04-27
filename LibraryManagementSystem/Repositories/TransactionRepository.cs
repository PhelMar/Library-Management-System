using LibrarySystem.Core.Database;
using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace LibrarySystem.Repositories
{
    public class TransactionRepository
    {
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

            string query = @"
                SELECT
                    s.id AS student_id,
                    s.student_id AS student_no,
                    s.student_name AS full_name,
                    c.course_name,
                    yl.level_name AS year_level,
                    e.id AS enrollment_id,
                    sy.year_label AS school_year,
                    sem.semester_name
                FROM enrollment e
                JOIN student s      ON s.id = e.student_id
                JOIN course c       ON c.id = e.course_id
                JOIN year_level yl  ON yl.id = e.year_level_id
                JOIN school_year sy ON sy.id = e.school_year_id
                JOIN semester sem   ON sem.id = e.semester_id
                WHERE sem.is_active = 1
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
                        WHEN bi.action IN ('add', 'returned', 'correction') THEN bi.qty
                        WHEN bi.action IN ('lost', 'damaged') THEN -bi.qty
                        ELSE 0
                    END
                ), 0) AS current_qty
            FROM books b
            JOIN category c ON c.id = b.category_id
            LEFT JOIN book_inventory bi ON bi.book_id = b.id
            WHERE
                b.book_title LIKE @keyword
                OR b.author LIKE @keyword
            GROUP BY
                b.id,
                b.book_title,
                b.author,
                c.category_name
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

            string query = @"
                SELECT id, full_name, shift
                FROM librarian
                ORDER BY full_name";

            using (var conn = GetConnection())
            using (var cmd = new MySqlCommand(query, conn))
            using (var adapter = new MySqlDataAdapter(cmd))
            {
                adapter.Fill(dt);
            }

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
                        WHEN action IN ('add','returned','correction') THEN qty
                        WHEN action IN ('lost','damaged') THEN -qty
                        ELSE 0
                    END
                ),0)
                FROM book_inventory
                WHERE book_id = @bookId";

                    int stock = 0;

                    using (var cmd = new MySqlCommand(checkStock, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@bookId", bookId);
                        stock = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    if (stock <= 0)
                        throw new Exception("Book is no longer available.");

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
                INSERT INTO book_inventory
                (book_id, action, qty, remarks, recorded_by)
                VALUES
                (@bookId, 'lost', 1, 'Borrowed by student', @librarianId)";

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

        public DataTable GetTransactionsPaged(string search, string statusFilter, int page, int pageSize, out int totalCount)
        {
            MarkOverdueInline();

            totalCount = 0;
            var dt = new DataTable();

            bool hasSearch = !string.IsNullOrWhiteSpace(search);
            bool hasStatus = !string.IsNullOrWhiteSpace(statusFilter);

            string whereClause = " WHERE 1=1 ";

            if (hasSearch)
            {
                whereClause += @"
                    AND (
                        s.student_name LIKE @search OR
                        s.student_id   LIKE @search OR
                        b.book_title   LIKE @search
                    )";
            }

            if (hasStatus)
            {
                whereClause += " AND bt.status = @status ";
            }

            string fromClause = @"
                FROM book_transactions bt
                JOIN enrollment e ON e.id = bt.enrollment_id
                JOIN student s ON s.id = e.student_id
                JOIN books b ON b.id = bt.book_id
                JOIN librarian lib ON lib.id = bt.librarian_id";

            using (var conn = GetConnection())
            {
                string countQuery = "SELECT COUNT(*) " + fromClause + whereClause;

                using (var cmd = new MySqlCommand(countQuery, conn))
                {
                    if (hasSearch)
                        cmd.Parameters.AddWithValue("@search", $"%{search}%");

                    if (hasStatus)
                        cmd.Parameters.AddWithValue("@status", statusFilter);

                    totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string query = @"
                    SELECT
                        bt.id,
                        s.student_id AS student_no,
                        s.student_name,
                        b.book_title,
                        lib.full_name AS librarian_name,
                        bt.borrow_date,
                        bt.due_date,
                        bt.returned_date,
                        bt.status,
                        bt.remarks "
                        + fromClause +
                        whereClause +
                        @"
                    ORDER BY bt.borrow_date DESC
                    LIMIT @pageSize OFFSET @offset";

                using (var cmd = new MySqlCommand(query, conn))
                using (var adapter = new MySqlDataAdapter(cmd))
                {
                    if (hasSearch)
                        cmd.Parameters.AddWithValue("@search", $"%{search}%");

                    if (hasStatus)
                        cmd.Parameters.AddWithValue("@status", statusFilter);

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
                    s.student_id AS student_no,
                    s.student_name,
                    b.book_title,
                    lib.full_name AS librarian_name,
                    bt.borrow_date,
                    bt.due_date,
                    bt.returned_date,
                    bt.status,
                    bt.remarks
                FROM book_transactions bt
                JOIN enrollment e ON e.id = bt.enrollment_id
                JOIN student s ON s.id = e.student_id
                JOIN books b ON b.id = bt.book_id
                JOIN librarian lib ON lib.id = bt.librarian_id
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

        public void ReturnBook(int transactionId, string remarks)
        {
            using (var conn = GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    int bookId;
                    int librarianId;

                    string getQuery = @"
                SELECT book_id, librarian_id
                FROM book_transactions
                WHERE id = @id
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
                        }
                    }

                    string updateQuery = @"
                UPDATE book_transactions
                SET
                    status = 'returned',
                    returned_date = CURRENT_TIMESTAMP,
                    remarks = @remarks
                WHERE id = @id
                AND status IN ('borrowed', 'overdue')";

                    using (var cmd = new MySqlCommand(updateQuery, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@id", transactionId);
                        cmd.Parameters.AddWithValue("@remarks",
                            string.IsNullOrWhiteSpace(remarks) ? DBNull.Value : (object)remarks);

                        if (cmd.ExecuteNonQuery() == 0)
                            throw new Exception("Transaction is already returned or does not exist.");
                    }

                    string inventoryQuery = @"
                INSERT INTO book_inventory
                (
                    book_id,
                    action,
                    qty,
                    remarks,
                    recorded_by
                )
                VALUES
                (
                    @bookId,
                    'returned',
                    1,
                    @inventoryRemarks,
                    @recordedBy
                )";

                    using (var cmd = new MySqlCommand(inventoryQuery, conn, tran))
                    {
                        cmd.Parameters.AddWithValue("@bookId", bookId);
                        cmd.Parameters.AddWithValue("@inventoryRemarks",
                            string.IsNullOrWhiteSpace(remarks)
                                ? "Book returned"
                                : remarks);

                        cmd.Parameters.AddWithValue("@recordedBy", librarianId);

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

        private void MarkOverdueInline()
        {
            try
            {
                using (var conn = GetConnection())
                using (var cmd = new MySqlCommand(@"
                    UPDATE book_transactions
                    SET status = 'overdue'
                    WHERE status = 'borrowed'
                    AND due_date < CURDATE()", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch
            {
            }
        }
    }
}