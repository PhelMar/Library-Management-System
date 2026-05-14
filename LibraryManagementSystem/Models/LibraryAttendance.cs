namespace LibrarySystem.Models
{
    public class LibraryAttendance
    {
        public int Id { get; set; }
        public int EnrollmentId { get; set; }
        public int StudentId { get; set; }
        public string StudentNo { get; set; }
        public string StudentName { get; set; }
        public string CourseName { get; set; }
        public string LevelName { get; set; }
        public System.DateTime TimeIn { get; set; }
        public System.DateTime? TimeOut { get; set; }
        public string Status { get; set; } // "In Library" or "Checked Out"
        public string Duration { get; set; } // HH:MM:SS format
    }
}