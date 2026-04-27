namespace LibrarySystem.Models
{
    public class User
    {
        public int Id { get; set; }
        public int LibrarianId { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
    }
}