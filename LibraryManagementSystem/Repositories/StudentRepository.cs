using LibrarySystem.Core.Database;
using LibrarySystem.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace LibrarySystem.Repositories
{
    public class StudentRepository
    {
        // ── Get paged students based on active school year and semester ──
        public DataTable GetStudentsPaged(string search, int page, int pageSize, out int totalCount)
        {
            totalCount = 0;
            var dt = new DataTable();

            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                string countQuery = @"
                    SELECT COUNT(*) 
                    FROM enrollment e
                    JOIN student s      ON e.student_id     = s.id
                    JOIN course c       ON e.course_id      = c.id
                    JOIN year_level yl  ON e.year_level_id  = yl.id
                    JOIN school_year sy ON e.school_year_id = sy.id
                    JOIN semester sem   ON e.semester_id    = sem.id
                    WHERE sy.is_active  = 1
                    AND   sem.is_active = 1
                    AND (
                        s.student_id   LIKE @search OR
                        s.student_name LIKE @search OR
                        c.course_code  LIKE @search
                    )";

                using (var cmd = new MySqlCommand(countQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string query = @"
                    SELECT 
                        s.id,
                        s.student_id    AS student_code,
                        s.student_name,
                        s.contact_no,
                        s.email,
                        c.id            AS course_id,
                        c.course_code,
                        c.course_name,
                        yl.id           AS year_level_id,
                        yl.level_name,
                        sy.id           AS school_year_id,
                        sy.year_label,
                        sem.id          AS semester_id,
                        sem.semester_name,
                        e.id            AS enrollment_id,
                        e.enrolled_at
                    FROM enrollment e
                    JOIN student s      ON e.student_id     = s.id
                    JOIN course c       ON e.course_id      = c.id
                    JOIN year_level yl  ON e.year_level_id  = yl.id
                    JOIN school_year sy ON e.school_year_id = sy.id
                    JOIN semester sem   ON e.semester_id    = sem.id
                    WHERE sy.is_active  = 1
                    AND   sem.is_active = 1
                    AND (
                        s.student_id   LIKE @search OR
                        s.student_name LIKE @search OR
                        c.course_code  LIKE @search
                    )
                    ORDER BY e.enrolled_at DESC
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

        // ── Check if student exists by student_id ──
        public Student GetStudentByCode(string studentCode)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM student WHERE student_id = @code LIMIT 1";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@code", studentCode);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Student
                            {
                                Id = reader.GetInt32("id"),
                                StudentId = reader.GetString("student_id"),
                                StudentName = reader.GetString("student_name"),
                                ContactNo = reader.GetString("contact_no"),
                                Email = reader.IsDBNull(reader.GetOrdinal("email"))
                                              ? "" : reader.GetString("email")
                            };
                        }
                    }
                }
            }

            return null;
        }

        // ── Check if student already enrolled in active school year + semester ──
        public bool IsAlreadyEnrolled(int studentDbId)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT COUNT(*) FROM enrollment e
                    JOIN school_year sy ON e.school_year_id = sy.id
                    JOIN semester sem   ON e.semester_id    = sem.id
                    WHERE e.student_id  = @studentId
                    AND   sy.is_active  = 1
                    AND   sem.is_active = 1";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentDbId);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        // ── Get active school year and semester ids ──
        public (int schoolYearId, string schoolYearLabel, int semesterId, string semesterName) GetActivePeriod()
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT sy.id, sy.year_label, sem.id AS sem_id, sem.semester_name
                    FROM school_year sy, semester sem
                    WHERE sy.is_active  = 1
                    AND   sem.is_active = 1
                    LIMIT 1";

                using (var cmd = new MySqlCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        return (
                            reader.GetInt32("id"),
                            reader.GetString("year_label"),
                            reader.GetInt32("sem_id"),
                            reader.GetString("semester_name")
                        );
                }
            }

            return (0, "", 0, "");
        }

        // ── Get dropdowns ──
        public DataTable GetCourses()
        {
            return GetLookup("SELECT id, CONCAT(course_code, ' - ', course_name) AS display_name FROM course ORDER BY course_code");
        }

        public DataTable GetYearLevels()
        {
            return GetLookup("SELECT id, level_name AS display_name FROM year_level ORDER BY id");
        }

        private DataTable GetLookup(string query)
        {
            var dt = new DataTable();
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (var cmd = new MySqlCommand(query, conn))
                using (var adapter = new MySqlDataAdapter(cmd))
                    adapter.Fill(dt);
            }
            return dt;
        }

        // ── Add new student then enroll ──
        public void AddStudentAndEnroll(Student student, int courseId, int yearLevelId)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                // Transaction — both must succeed or both rollback
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Step 1: Insert student
                        string insertStudent = @"
                            INSERT INTO student (student_id, student_name, contact_no, email)
                            VALUES (@studentId, @name, @contact, @email)";

                        int newStudentDbId;

                        using (var cmd = new MySqlCommand(insertStudent, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@studentId", student.StudentId);
                            cmd.Parameters.AddWithValue("@name", student.StudentName);
                            cmd.Parameters.AddWithValue("@contact", student.ContactNo);
                            cmd.Parameters.AddWithValue("@email", student.Email);
                            cmd.ExecuteNonQuery();
                            newStudentDbId = Convert.ToInt32(cmd.LastInsertedId);
                        }

                        // Step 2: Get active period
                        var period = GetActivePeriodWithConn(conn, transaction);

                        // Step 3: Enroll
                        EnrollStudent(conn, transaction, newStudentDbId, courseId, yearLevelId, period.schoolYearId, period.semesterId);

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // ── Enroll existing student ──
        public void EnrollExistingStudent(int studentDbId, int courseId, int yearLevelId)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        var period = GetActivePeriodWithConn(conn, transaction);
                        EnrollStudent(conn, transaction, studentDbId, courseId, yearLevelId, period.schoolYearId, period.semesterId);
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // ── Update student info ──
        public void UpdateStudent(Student student, int courseId, int yearLevelId, int enrollmentId)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string updateStudent = @"
                            UPDATE student 
                            SET student_id   = @studentId,
                                student_name = @name,
                                contact_no   = @contact,
                                email        = @email
                            WHERE id = @id";

                        using (var cmd = new MySqlCommand(updateStudent, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@studentId", student.StudentId);
                            cmd.Parameters.AddWithValue("@name", student.StudentName);
                            cmd.Parameters.AddWithValue("@contact", student.ContactNo);
                            cmd.Parameters.AddWithValue("@email", student.Email);
                            cmd.Parameters.AddWithValue("@id", student.Id);
                            cmd.ExecuteNonQuery();
                        }

                        string updateEnrollment = @"
                            UPDATE enrollment
                            SET course_id     = @courseId,
                                year_level_id = @yearLevelId
                            WHERE id = @enrollmentId";

                        using (var cmd = new MySqlCommand(updateEnrollment, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@courseId", courseId);
                            cmd.Parameters.AddWithValue("@yearLevelId", yearLevelId);
                            cmd.Parameters.AddWithValue("@enrollmentId", enrollmentId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // ── Delete student and all enrollments ──
        public void DeleteStudent(int studentDbId)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string deleteEnrollments = "DELETE FROM enrollment WHERE student_id = @id";
                        string deleteStudent = "DELETE FROM student WHERE id = @id";

                        using (var cmd = new MySqlCommand(deleteEnrollments, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", studentDbId);
                            cmd.ExecuteNonQuery();
                        }

                        using (var cmd = new MySqlCommand(deleteStudent, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@id", studentDbId);
                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        // ── Private helpers ──
        private void EnrollStudent(MySqlConnection conn, MySqlTransaction transaction,
            int studentDbId, int courseId, int yearLevelId, int schoolYearId, int semesterId)
        {
            string query = @"
                INSERT INTO enrollment 
                    (student_id, course_id, year_level_id, school_year_id, semester_id)
                VALUES 
                    (@studentId, @courseId, @yearLevelId, @schoolYearId, @semesterId)";

            using (var cmd = new MySqlCommand(query, conn, transaction))
            {
                cmd.Parameters.AddWithValue("@studentId", studentDbId);
                cmd.Parameters.AddWithValue("@courseId", courseId);
                cmd.Parameters.AddWithValue("@yearLevelId", yearLevelId);
                cmd.Parameters.AddWithValue("@schoolYearId", schoolYearId);
                cmd.Parameters.AddWithValue("@semesterId", semesterId);
                cmd.ExecuteNonQuery();
            }
        }

        private (int schoolYearId, int semesterId) GetActivePeriodWithConn(
            MySqlConnection conn, MySqlTransaction transaction)
        {
            string query = @"
                SELECT sy.id AS sy_id, sem.id AS sem_id
                FROM school_year sy, semester sem
                WHERE sy.is_active = 1 AND sem.is_active = 1
                LIMIT 1";

            using (var cmd = new MySqlCommand(query, conn, transaction))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                    return (reader.GetInt32("sy_id"), reader.GetInt32("sem_id"));
            }

            throw new Exception("No active school year or semester found. Please set them in Settings.");
        }
    }
}