using LibrarySystem.Models;

namespace LibrarySystem.Core
{
    public static class Session
    {
        public static User CurrentUser { get; set; }

        public static bool IsAdmin => CurrentUser?.Role == "admin";

        public static void Clear()
        {
            CurrentUser = null;
        }
    }
}