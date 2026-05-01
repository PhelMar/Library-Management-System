using LibrarySystem.Core.Database;
using LibrarySystem.Models;
using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace LibrarySystem.Repositories
{
    public class StudentRepository
    {
        public (int schoolYearId, string schoolYearLabel, int semesterId, string semesterName) GetActivePeriod()
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                return ResolveActivePeriod(conn, null);
            }
        }

        public DataTable GetStudentsPaged(string search, int page, int pageSize, out int totalCount)
        {
            totalCount = 0;
            var dt = new DataTable();

            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();

                var period = ResolveActivePeriod(conn, null);
                if (period.schoolYearId == 0)
                    return dt;

                string baseWhere = @"
                    WHERE e.school_year_id = @schoolYearId
                    AND   e.semester_id    = @semesterId
                    AND (
                        s.student_id   LIKE @search OR
                        s.student_name LIKE @search OR
                        c.course_code  LIKE @search
                    )";

                string countQuery = $@"
                    SELECT COUNT(*)
                    FROM enrollment e
                    JOIN student    s   ON e.student_id    = s.id
                    JOIN course     c   ON e.course_id     = c.id
                    JOIN year_level yl  ON e.year_level_id = yl.id
                    JOIN school_year sy ON e.school_year_id= sy.id
                    JOIN semester   sem ON e.semester_id   = sem.id
                    {baseWhere}";

                using (var cmd = new MySqlCommand(countQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@schoolYearId", period.schoolYearId);
                    cmd.Parameters.AddWithValue("@semesterId", period.semesterId);
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    totalCount = Convert.ToInt32(cmd.ExecuteScalar());
                }

                string query = $@"
                    SELECT
                        s.id,
                        s.student_id        AS student_code,
                        s.student_name,
                        s.contact_no,
                        s.email,
                        c.id                AS course_id,
                        c.course_code,
                        c.course_name,
                        yl.id               AS year_level_id,
                        yl.level_name,
                        sy.id               AS school_year_id,
                        sy.year_label,
                        sem.id              AS semester_id,
                        sem.semester_name,
                        e.id                AS enrollment_id,
                        e.status,
                        e.enrolled_at
                    FROM enrollment e
                    JOIN student    s   ON e.student_id     = s.id
                    JOIN course     c   ON e.course_id      = c.id
                    JOIN year_level yl  ON e.year_level_id  = yl.id
                    JOIN school_year sy ON e.school_year_id = sy.id
                    JOIN semester   sem ON e.semester_id    = sem.id
                    {baseWhere}
                    ORDER BY e.enrolled_at DESC
                    LIMIT @pageSize OFFSET @offset";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@schoolYearId", period.schoolYearId);
                    cmd.Parameters.AddWithValue("@semesterId", period.semesterId);
                    cmd.Parameters.AddWithValue("@search", $"%{search}%");
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                    cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

                    using (var adapter = new MySqlDataAdapter(cmd))
                        adapter.Fill(dt);
                }
            }

            return dt;
        }

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
                                ContactNo = reader.IsDBNull(reader.GetOrdinal("contact_no")) ? "" : reader.GetString("contact_no"),
                                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString("email")
                            };
                        }
                    }
                }
            }

            return null;
        }

        public bool IsAlreadyEnrolled(int studentDbId, int schoolYearId, int semesterId)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = @"
            SELECT COUNT(*) FROM enrollment
            WHERE student_id     = @studentId
            AND   school_year_id = @schoolYearId
            AND   semester_id    = @semesterId";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@studentId", studentDbId);
                    cmd.Parameters.AddWithValue("@schoolYearId", schoolYearId);
                    cmd.Parameters.AddWithValue("@semesterId", semesterId);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public DataTable GetCourses()
        {
            return GetLookup("SELECT id, CONCAT(course_code, ' - ', course_name) AS display_name FROM course ORDER BY course_code");
        }

        public DataTable GetYearLevels()
        {
            return GetLookup("SELECT id, level_name AS display_name FROM year_level ORDER BY id");
        }

        public void AddStudentAndEnroll(Student student, int courseId, int yearLevelId, int schoolYearId, int semesterId)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string insertStudent = @"
                    INSERT INTO student (student_id, student_name, contact_no, email)
                    VALUES (@studentId, @name, @contact, @email)";

                        int newStudentDbId;

                        using (var cmd = new MySqlCommand(insertStudent, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@studentId", student.StudentId);
                            cmd.Parameters.AddWithValue("@name", student.StudentName);
                            cmd.Parameters.AddWithValue("@contact", student.ContactNo);
                            cmd.Parameters.AddWithValue("@email", (object)student.Email ?? DBNull.Value);
                            cmd.ExecuteNonQuery();
                            newStudentDbId = Convert.ToInt32(cmd.LastInsertedId);
                        }

                        EnrollStudent(conn, transaction, newStudentDbId, courseId, yearLevelId, schoolYearId, semesterId);
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

        public void EnrollExistingStudent(int studentDbId, int courseId, int yearLevelId, int schoolYearId, int semesterId)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        EnrollStudent(conn, transaction, studentDbId, courseId, yearLevelId, schoolYearId, semesterId);
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

        public void UpdateStudent(Student student, int courseId, int yearLevelId, int schoolYearId, int semesterId, int enrollmentId)
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
                            cmd.Parameters.AddWithValue("@email", (object)student.Email ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@id", student.Id);
                            cmd.ExecuteNonQuery();
                        }

                        string updateEnrollment = @"
                    UPDATE enrollment
                    SET course_id     = @courseId,
                        year_level_id = @yearLevelId,
                        school_year_id = @schoolYearId,
                        semester_id   = @semesterId
                    WHERE id = @enrollmentId";

                        using (var cmd = new MySqlCommand(updateEnrollment, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@courseId", courseId);
                            cmd.Parameters.AddWithValue("@yearLevelId", yearLevelId);
                            cmd.Parameters.AddWithValue("@schoolYearId", schoolYearId);
                            cmd.Parameters.AddWithValue("@semesterId", semesterId);
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

        public void UpdateEnrollmentStatus(int enrollmentId, string status)
        {
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = "UPDATE enrollment SET status = @status WHERE id = @id";

                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@status", status);
                    cmd.Parameters.AddWithValue("@id", enrollmentId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void EnrollStudent(MySqlConnection conn, MySqlTransaction transaction,
            int studentDbId, int courseId, int yearLevelId, int schoolYearId, int semesterId)
        {
            string query = @"
                INSERT INTO enrollment (student_id, course_id, year_level_id, school_year_id, semester_id, status)
                VALUES (@studentId, @courseId, @yearLevelId, @schoolYearId, @semesterId, 'enrolled')";

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

        private (int schoolYearId, string schoolYearLabel, int semesterId, string semesterName) ResolveActivePeriod(
            MySqlConnection conn, MySqlTransaction transaction)
        {
            // Step 1 — get the latest school year
            string syQuery = "SELECT id, year_label FROM school_year ORDER BY year_label DESC LIMIT 1";

            int schoolYearId = 0;
            string schoolYearLabel = "";

            using (var cmd = transaction != null
                ? new MySqlCommand(syQuery, conn, transaction)
                : new MySqlCommand(syQuery, conn))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    schoolYearId = reader.GetInt32("id");
                    schoolYearLabel = reader.GetString("year_label");
                }
            }

            if (schoolYearId == 0)
                return (0, "", 0, "");

            // Step 2 — check if 2nd Semester exists for that school year
            string semQuery = @"
                SELECT sem.id, sem.semester_name
                FROM enrollment e
                JOIN semester sem ON e.semester_id = sem.id
                WHERE e.school_year_id = @schoolYearId
                AND   sem.semester_name LIKE '%2nd%'
                LIMIT 1";

            int semesterId = 0;
            string semesterName = "";

            using (var cmd = transaction != null
                ? new MySqlCommand(semQuery, conn, transaction)
                : new MySqlCommand(semQuery, conn))
            {
                cmd.Parameters.AddWithValue("@schoolYearId", schoolYearId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        semesterId = reader.GetInt32("id");
                        semesterName = reader.GetString("semester_name");
                    }
                }
            }

            // Step 3 — fallback to 1st Semester
            if (semesterId == 0)
            {
                string fallbackQuery = @"
                    SELECT sem.id, sem.semester_name
                    FROM enrollment e
                    JOIN semester sem ON e.semester_id = sem.id
                    WHERE e.school_year_id = @schoolYearId
                    LIMIT 1";

                using (var cmd = transaction != null
                    ? new MySqlCommand(fallbackQuery, conn, transaction)
                    : new MySqlCommand(fallbackQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@schoolYearId", schoolYearId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            semesterId = reader.GetInt32("id");
                            semesterName = reader.GetString("semester_name");
                        }
                    }
                }
            }

            if (semesterId == 0)
                return (0, "", 0, "");

            return (schoolYearId, schoolYearLabel, semesterId, semesterName);
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

        public DataRow GetEnrollmentById(int enrollmentId)
        {
            var dt = new DataTable();
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM enrollment WHERE id = @id LIMIT 1";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", enrollmentId);
                    using (var adapter = new MySqlDataAdapter(cmd))
                        adapter.Fill(dt);
                }
            }
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        public DataTable GetSemesters()
        {
            return GetLookup("SELECT id, semester_name AS display_name FROM semester ORDER BY id");
        }

        public DataTable GetSchoolYears()
        {
            return GetLookup("SELECT id, year_label AS display_name FROM school_year ORDER BY year_label DESC");
        }

        public DataRow GetStudentRawById(int studentDbId)
        {
            var dt = new DataTable();
            using (var conn = DatabaseConnection.GetConnection())
            {
                conn.Open();
                string query = "SELECT * FROM student WHERE id = @id LIMIT 1";
                using (var cmd = new MySqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", studentDbId);
                    using (var adapter = new MySqlDataAdapter(cmd))
                        adapter.Fill(dt);
                }
            }
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }
    }
}