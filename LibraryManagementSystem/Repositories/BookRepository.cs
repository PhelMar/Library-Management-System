using LibrarySystem.Core.Database;
using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace LibrarySystem.Repositories
{
    public class BookRepository
    {
        public DataTable GetBooksPaged(string search, int page, int pageSize, out int totalCount)
        {
            totalCount = 0;
            var dt = new DataTable();

            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                string countQuery = @"
                    SELECT COUNT(*) FROM books b
                    LEFT JOIN category c ON b.category_id = c.id
                    WHERE b.book_title LIKE @search
                    OR b.author LIKE @search
                    OR c.category_name LIKE @search";

                using (var cmd = new MySqlCommand(countQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string query = @"
                    SELECT 
                        b.id,
                        b.book_title,
                        b.author,
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
                    WHERE b.book_title LIKE @search
                    OR b.author LIKE @search
                    OR c.category_name LIKE @search
                    GROUP BY b.id, b.book_title, b.author, c.category_name, b.category_id
                    ORDER BY b.created_at DESC
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
                        b.id, b.book_title, b.author, b.category_id,
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
                    GROUP BY b.id, b.book_title, b.author, b.category_id, c.category_name";

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

        public void AddBook(string title, string author, int categoryId)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = @"INSERT INTO books (book_title, author, category_id) 
                                 VALUES (@title, @author, @categoryId)";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@author", author);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void UpdateBook(int bookId, string title, string author, int categoryId)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = @"UPDATE books 
                                 SET book_title = @title, author = @author, category_id = @categoryId 
                                 WHERE id = @id";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@title", title);
                    cmd.Parameters.AddWithValue("@author", author);
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    cmd.Parameters.AddWithValue("@id", bookId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void DeleteBook(int bookId)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                // Delete logs first then book
                string deleteLogs = "DELETE FROM book_inventory WHERE book_id = @id";
                string deleteBook = "DELETE FROM books WHERE id = @id";

                using (var cmd = new MySqlCommand(deleteLogs, conn))
                {
                    cmd.Parameters.AddWithValue("@id", bookId);
                    cmd.ExecuteNonQuery();
                }
                using (var cmd = new MySqlCommand(deleteBook, conn))
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
                string query = @"INSERT INTO book_inventory 
                                    (book_id, action, qty, remarks, recorded_by) 
                                 VALUES 
                                    (@bookId, @action, @qty, @remarks, @recordedBy)";
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