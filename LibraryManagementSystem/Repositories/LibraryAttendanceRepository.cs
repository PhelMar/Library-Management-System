using LibrarySystem.Core.Database;
using LibrarySystem.Models;
using System;
using System.Data;
using MySql.Data.MySqlClient;

namespace LibrarySystem.Repositories
{
    public class LibraryAttendanceRepository
    {
        public bool CheckInStudent(string studentId, out string message)
        {
            message = "";
            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string enrollmentQuery = @"
                SELECT e.id, s.id AS student_pk, s.student_name
                FROM enrollment e
                JOIN student s ON e.student_id = s.id
                JOIN semester sem ON e.semester_id = sem.id
                WHERE s.student_id = @studentId
                AND sem.is_active = 1
                AND e.status = 'enrolled'
                LIMIT 1";

                    int enrollmentId = 0;
                    int studentPk = 0;
                    string studentName = "";

                    using (var cmd = new MySqlCommand(enrollmentQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@studentId", studentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                enrollmentId = Convert.ToInt32(reader["id"]);
                                studentPk = Convert.ToInt32(reader["student_pk"]);
                                studentName = reader["student_name"].ToString();
                            }
                            else
                            {
                                message = "Student not found or not enrolled in active semester";
                                return false;
                            }
                        }
                    }

                    string checkTodayQuery = @"
                SELECT id FROM library_attendance
                WHERE student_id = @studentPk
                AND DATE(time_in) = CURDATE()
                AND time_out IS NULL
                LIMIT 1";

                    using (var cmd = new MySqlCommand(checkTodayQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@studentPk", studentPk);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            message = $"{studentName} is already checked in. Please check them out first.";
                            return false;
                        }
                    }

                    string insertQuery = @"
                INSERT INTO library_attendance (enrollment_id, student_id, time_in)
                VALUES (@enrollmentId, @studentPk, CURRENT_TIMESTAMP)";

                    using (var cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@enrollmentId", enrollmentId);
                        cmd.Parameters.AddWithValue("@studentPk", studentPk);
                        int result = cmd.ExecuteNonQuery();

                        if (result > 0)
                        {
                            message = $"✓ {studentName} checked in successfully at {DateTime.Now:HH:mm:ss}";
                            return true;
                        }
                    }

                    message = "Failed to check in student";
                    return false;
                }
            }
            catch (Exception ex)
            {
                message = $"Error: {ex.Message}";
                return false;
            }
        }

        public bool CheckOutStudent(string studentId, out string message)
        {
            message = "";

            try
            {
                using (var conn = DatabaseConnection.GetConnection())
                {
                    conn.Open();

                    string query = @"
                SELECT la.id, s.student_name, la.time_in
                FROM library_attendance la
                JOIN student s ON la.student_id = s.id
                WHERE s.student_id = @studentId
                AND la.time_out IS NULL
                ORDER BY la.time_in DESC
                LIMIT 1";

                    int attendanceId = 0;
                    string studentName = "";
                    DateTime timeIn = DateTime.Now;

                    using (var cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@studentId", studentId);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                attendanceId = Convert.ToInt32(reader["id"]);
                                studentName = reader["student_name"].ToString();
                                timeIn = Convert.ToDateTime(reader["time_in"]);
                            }
                            else
                            {
                                message = "No active check-in found for this student";
                                return false;
                            }
                        }
                    }

                    string updateQuery = @"
                UPDATE library_attendance
                SET time_out = CURRENT_TIMESTAMP
                WHERE id = @id";

                    using (var cmd = new MySqlCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", attendanceId);
                        int result = cmd.ExecuteNonQuery();

                        if (result > 0)
                        {
                            TimeSpan duration = DateTime.Now - timeIn;
                            string durationStr = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
                            message = $"✓ {studentName} checked out. Duration: {durationStr}";
                            return true;
                        }
                    }

                    message = "Failed to check out student";
                    return false;
                }
            }
            catch (Exception ex)
            {
                message = $"Error: {ex.Message}";
                return false;
            }
        }

        public DataTable GetTodayAttendancePaged(
            string searchStudentId,
            int page,
            int pageSize,
            out int totalCount)
        {
            totalCount = 0;
            var dt = new DataTable();

            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                string countQuery = @"
                    SELECT COUNT(*) FROM library_attendance la
                    INNER JOIN enrollment e ON la.enrollment_id = e.id
                    INNER JOIN student s ON la.student_id = s.id
                    INNER JOIN semester sem ON e.semester_id = sem.id
                    WHERE sem.is_active = 1
                    AND DATE(la.time_in) = CURDATE()
                    AND (@search = '' OR s.student_id LIKE @search OR s.student_name LIKE @search)";

                using (var cmd = new MySqlCommand(countQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@search", $"%{searchStudentId}%");
                    totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string query = @"
                    SELECT
                        la.id,
                        s.student_id,
                        s.student_name,
                        c.course_name,
                        yl.level_name,
                        la.time_in,
                        la.time_out,
                        CASE
                            WHEN la.time_out IS NULL THEN 'In Library'
                            ELSE 'Checked Out'
                        END AS status,
                        COALESCE(
                            TIME_FORMAT(TIMEDIFF(la.time_out, la.time_in), '%H:%i:%s'),
                            '--:--:--'
                        ) AS duration
                    FROM library_attendance la
                    INNER JOIN enrollment e ON la.enrollment_id = e.id
                    INNER JOIN student s ON la.student_id = s.id
                    INNER JOIN course c ON e.course_id = c.id
                    INNER JOIN year_level yl ON e.year_level_id = yl.id
                    INNER JOIN semester sem ON e.semester_id = sem.id
                    WHERE sem.is_active = 1
                    AND DATE(la.time_in) = CURDATE()
                    AND (@search = '' OR s.student_id LIKE @search OR s.student_name LIKE @search)
                    ORDER BY la.time_in DESC
                    LIMIT @pageSize OFFSET @offset";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@search", $"%{searchStudentId}%");
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                    cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

                    using (var adapter = new MySqlDataAdapter(cmd))
                        adapter.Fill(dt);
                }
            }

            return dt;
        }

        public DataTable GetAttendanceByDateRangePaged(
            DateTime fromDate,
            DateTime toDate,
            string searchStudentId,
            int page,
            int pageSize,
            out int totalCount)
        {
            totalCount = 0;
            var dt = new DataTable();

            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                string countQuery = @"
                    SELECT COUNT(*) FROM library_attendance la
                    INNER JOIN enrollment e ON la.enrollment_id = e.id
                    INNER JOIN student s ON la.student_id = s.id
                    INNER JOIN semester sem ON e.semester_id = sem.id
                    WHERE sem.is_active = 1
                    AND DATE(la.time_in) BETWEEN @fromDate AND @toDate
                    AND (@search = '' OR s.student_id LIKE @search OR s.student_name LIKE @search)";

                using (var cmd = new MySqlCommand(countQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@fromDate", fromDate.Date);
                    cmd.Parameters.AddWithValue("@toDate", toDate.Date);
                    cmd.Parameters.AddWithValue("@search", $"%{searchStudentId}%");
                    totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string query = @"
                    SELECT
                        la.id,
                        s.student_id,
                        s.student_name,
                        c.course_name,
                        yl.level_name,
                        la.time_in,
                        la.time_out,
                        CASE
                            WHEN la.time_out IS NULL THEN 'In Library'
                            ELSE 'Checked Out'
                        END AS status,
                        COALESCE(
                            TIME_FORMAT(TIMEDIFF(la.time_out, la.time_in), '%H:%i:%s'),
                            '--:--:--'
                        ) AS duration
                    FROM library_attendance la
                    INNER JOIN enrollment e ON la.enrollment_id = e.id
                    INNER JOIN student s ON la.student_id = s.id
                    INNER JOIN course c ON e.course_id = c.id
                    INNER JOIN year_level yl ON e.year_level_id = yl.id
                    INNER JOIN semester sem ON e.semester_id = sem.id
                    WHERE sem.is_active = 1
                    AND DATE(la.time_in) BETWEEN @fromDate AND @toDate
                    AND (@search = '' OR s.student_id LIKE @search OR s.student_name LIKE @search)
                    ORDER BY la.time_in DESC
                    LIMIT @pageSize OFFSET @offset";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@fromDate", fromDate.Date);
                    cmd.Parameters.AddWithValue("@toDate", toDate.Date);
                    cmd.Parameters.AddWithValue("@search", $"%{searchStudentId}%");
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                    cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

                    using (var adapter = new MySqlDataAdapter(cmd))
                        adapter.Fill(dt);
                }
            }

            return dt;
        }
    }
}