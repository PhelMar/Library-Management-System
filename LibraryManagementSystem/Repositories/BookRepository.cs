using LibrarySystem.Core.Database;
using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace LibrarySystem.Repositories
{
    public class BookRepository
    {
        public DataTable GetBooksPaged(
            string search, int page, int pageSize,
            out int totalCount,
            int categoryId = 0,
            string sortBy = "created_at",
            string sortDir = "DESC")
        {
            totalCount = 0;
            var dt = new DataTable();

            var allowedSorts = new[] { "book_title", "author", "created_at" };
            if (!Array.Exists(allowedSorts, s => s == sortBy)) sortBy = "created_at";
            if (sortDir != "ASC") sortDir = "DESC";

            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                string baseWhere = @"
                    WHERE b.is_archived = 0
                    AND (@categoryId = 0 OR b.category_id = @categoryId)
                    AND (
                        b.book_title LIKE @search
                        OR b.author LIKE @search
                        OR c.category_name LIKE @search
                    )";

                string countQuery = $@"
                    SELECT COUNT(*) FROM books b
                    LEFT JOIN category c ON b.category_id = c.id
                    {baseWhere}";

                using (var cmd = new MySqlCommand(countQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string query = $@"
                    SELECT
                        b.id,
                        b.book_title,
                        b.author,
                        b.isbn,
                        b.edition,
                        b.published_year,
                        b.is_archived,
                        c.category_name,
                        b.category_id,
                        COALESCE(SUM(
                            CASE
                                WHEN bi.action = 'add'        THEN  bi.qty
                                WHEN bi.action = 'lost'       THEN -bi.qty
                                WHEN bi.action = 'damaged'    THEN -bi.qty
                                WHEN bi.action = 'correction' THEN  bi.qty
                                ELSE 0
                            END
                        ), 0) AS current_qty
                    FROM books b
                    LEFT JOIN category c ON b.category_id = c.id
                    LEFT JOIN book_inventory bi ON b.id = bi.book_id
                    {baseWhere}
                    GROUP BY b.id, b.book_title, b.author, b.isbn, b.edition,
                             b.published_year, b.is_archived, c.category_name, b.category_id
                    ORDER BY b.{sortBy} {sortDir}
                    LIMIT @pageSize OFFSET @offset";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                    cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

                    using (var adapter = new MySqlDataAdapter(cmd))
                        adapter.Fill(dt);
                }
            }

            return dt;
        }

        public DataTable GetArchivedBooksPaged(string search, int page, int pageSize, out int totalCount)
        {
            totalCount = 0;
            var dt = new DataTable();

            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                string baseWhere = @"
                    WHERE b.is_archived = 1
                    AND (
                        b.book_title LIKE @search
                        OR b.author LIKE @search
                        OR c.category_name LIKE @search
                    )";

                string countQuery = $@"
                    SELECT COUNT(*) FROM books b
                    LEFT JOIN category c ON b.category_id = c.id
                    {baseWhere}";

                using (var cmd = new MySqlCommand(countQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string query = $@"
                    SELECT
                        b.id, b.book_title, b.author,
                        b.isbn, b.edition, b.published_year,
                        c.category_name, b.category_id
                    FROM books b
                    LEFT JOIN category c ON b.category_id = c.id
                    {baseWhere}
                    ORDER BY b.book_title ASC
                    LIMIT @pageSize OFFSET @offset";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                    cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

                    using (var adapter = new MySqlDataAdapter(cmd))
                        adapter.Fill(dt);
                }
            }

            return dt;
        }

        public DataRow GetBookById(int bookId)
        {
            var dt = new DataTable();

            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT
                        b.id, b.book_title, b.author,
                        b.isbn, b.edition, b.published_year,
                        b.category_id, b.is_archived,
                        c.category_name,
                        COALESCE(SUM(
                            CASE
                                WHEN bi.action = 'add'        THEN  bi.qty
                                WHEN bi.action = 'lost'       THEN -bi.qty
                                WHEN bi.action = 'damaged'    THEN -bi.qty
                                WHEN bi.action = 'correction' THEN  bi.qty
                                ELSE 0
                            END
                        ), 0) AS current_qty
                    FROM books b
                    LEFT JOIN category c ON b.category_id = c.id
                    LEFT JOIN book_inventory bi ON b.id = bi.book_id
                    WHERE b.id = @id
                    GROUP BY b.id, b.book_title, b.author, b.isbn, b.edition,
                             b.published_year, b.is_archived, b.category_id, c.category_name";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", bookId);
                    using (var adapter = new MySqlDataAdapter(cmd))
                        adapter.Fill(dt);
                }
            }

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public DataTable GetInventoryLog(int bookId)
        {
            var dt = new DataTable();

            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                string query = @"
                    SELECT
                        bi.recorded_at, bi.action, bi.qty,
                        bi.remarks, l.full_name AS recorded_by
                    FROM book_inventory bi
                    JOIN librarian l ON bi.recorded_by = l.id
                    WHERE bi.book_id = @bookId
                    ORDER BY bi.recorded_at DESC";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@bookId", bookId);
                    using (var adapter = new MySqlDataAdapter(cmd))
                        adapter.Fill(dt);
                }
            }

            return dt;
        }

        public DataTable GetCategories()
        {
            var dt = new DataTable();

            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = "SELECT id, category_name FROM category ORDER BY category_name";
                using (var cmd = new MySqlCommand(query, conn))
                using (var adapter = new MySqlDataAdapter(cmd))
                    adapter.Fill(dt);
            }

            return dt;
        }

        public void AddBook(string title, string author, int categoryId, string isbn, string edition, int? publishedYear)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO books (book_title, author, category_id, isbn, edition, published_year)
                    VALUES (@title, @author, @categoryId, @isbn, @edition, @publishedYear)";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@author", author);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@isbn", (object)isbn ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@edition", (object)edition ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@publishedYear", (object)publishedYear ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateBook(int bookId, string title, string author, int categoryId, string isbn, string edition, int? publishedYear)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = @"
                    UPDATE books
                    SET book_title = @title,
                        author = @author,
                        category_id = @categoryId,
                        isbn = @isbn,
                        edition = @edition,
                        published_year = @publishedYear
                    WHERE id = @id";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@author", author);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@isbn", (object)isbn ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@edition", (object)edition ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@publishedYear", (object)publishedYear ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", bookId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void ArchiveBook(int bookId)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = "UPDATE books SET is_archived = 1 WHERE id = @id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", bookId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void RestoreBook(int bookId)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = "UPDATE books SET is_archived = 0 WHERE id = @id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", bookId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void RecordInventory(int bookId, string action, int qty, string remarks, int recordedBy)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO book_inventory (book_id, action, qty, remarks, recorded_by)
                    VALUES (@bookId, @action, @qty, @remarks, @recordedBy)";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@bookId", bookId);
                    cmd.Parameters.AddWithValue("@action", action);
                    cmd.Parameters.AddWithValue("@qty", qty);
                    cmd.Parameters.AddWithValue("@remarks", remarks);
                    cmd.Parameters.AddWithValue("@recordedBy", recordedBy);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}