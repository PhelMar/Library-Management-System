using LibraryManagementSystem;
using LibrarySystem.Core;
using LibrarySystem.Core.Helpers;
using LibrarySystem.Core.Security;
using LibrarySystem.Repositories;
using System;
using System.Windows.Forms;

namespace LibrarySystem.Forms
{
    public partial class Login : Form
    {
        private readonly UserRepository _userRepository;

        public Login()
        {
            InitializeComponent();
            _userRepository = new UserRepository();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageHelper.ShowWarning("Please enter both username and password.");
                return;
            }

            try
            {
                var user = _userRepository.GetByUsername(username);

                if (user == null)
                {
                    MessageHelper.ShowError("Invalid username or password.");
                    return;
                }

                if (!PasswordHelper.VerifyPassword(password, user.Password))
                {
                    MessageHelper.ShowError("Invalid username or password.");
                    return;
                }

                Session.CurrentUser = user;
                MessageHelper.ShowSuccess($"Welcome, {user.Username}!");

                AdminMain adminMain = new AdminMain();
                adminMain.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Something went wrong: " + ex.Message);
            }
        }
    }
}