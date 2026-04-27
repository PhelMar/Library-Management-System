using System;

namespace LibrarySystem.Models
{
    public class Enrollment
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int YearLevelId { get; set; }
        public int SchoolYearId { get; set; }
        public int SemesterId { get; set; }
        public DateTime EnrolledAt { get; set; }

        // Display fields
        public string StudentName { get; set; }
        public string StudentCode { get; set; }
        public string CourseName { get; set; }
        public string YearLevel { get; set; }
        public string SchoolYear { get; set; }
        public string SemesterName { get; set; }
    }
}