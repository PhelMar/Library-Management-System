using LibrarySystem.Core;
using LibrarySystem.Forms.Admin.Dashboard;
using LibrarySystem.Forms.Borrow;
using LibrarySystem.Forms.Books;
using LibrarySystem.Forms.Students;
using System;
using System.Drawing;
using System.Windows.Forms;
using LibrarySystem.Forms.Reports;
using LibrarySystem.Forms.Settings;
using LibrarySystem.Repositories;
using LibrarySystem.Forms;
using LibrarySystem.Forms.Attendance;

namespace LibrarySystem.Forms
{
    public class AdminMain : Form
    {
        private Panel sidebar;
        private Panel contentArea;
        private Panel activeIndicator;
        private Button _activeButton;

        public AdminMain()
        {
            InitializeShell();
            SetActiveButton(btnDashboard);
            this.Load += (s, e) =>
            {
                var transactionRepo = new TransactionRepository();
                transactionRepo.MarkOverdueAndUpdateFines();
                LoadPanel(new DashboardPanel());
            };
        }

        // ── Sidebar Buttons ────────────────────────────────
        private Button btnDashboard;
        private Button btnBooks;
        private Button btnUsers;
        private Button btnBorrow;
        private Button btnReports;
        private Button btnSettings;
        private Button btnLogout;
        private Button btnArchivedBooks;
        private Button btnFines;
        private Button btnAttendance;

        private void InitializeShell()
        {

            // ── Form Setup ─────────────────────────────────
            this.Text = "Library Management System";
            this.Size = new Size(1100, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(245, 245, 255);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimumSize = new Size(1100, 680);
            this.WindowState = FormWindowState.Maximized;

            // ── Sidebar ────────────────────────────────────
            sidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(30, 20, 60)
            };

            // Library Name Title
            var lblLibrary = new Label
            {
                Text = "📚",
                Font = new Font("Segoe UI", 22f),
                ForeColor = Color.White,
                Location = new Point(0, 25),
                Size = new Size(220, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblAppName = new Label
            {
                Text = "Library",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(0, 65),
                Size = new Size(220, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblAppSub = new Label
            {
                Text = "Management System",
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(150, 140, 200),
                Location = new Point(0, 90),
                Size = new Size(220, 20),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Divider
            var divider = new Panel
            {
                Location = new Point(20, 120),
                Size = new Size(180, 1),
                BackColor = Color.FromArgb(60, 50, 100)
            };

            // ── Nav Buttons ────────────────────────────────
            btnDashboard = CreateNavButton("⊞  Dashboard", 140);
            btnBooks = CreateNavButton("📖  Manage Books", 190);
            btnArchivedBooks = CreateNavButton("🗄  Archived Books", 240);
            btnUsers = CreateNavButton("👤  Manage Students", 290);
            btnAttendance = CreateNavButton("📋 Attendance", 340);
            btnBorrow = CreateNavButton("↗  Borrow Books", 390);
            btnFines = CreateNavButton("💰  Fines & Overdue", 440);
            btnReports = CreateNavButton("📊  Reports", 490);
            btnSettings = CreateNavButton("⚙  Settings", 540);

            // Logout at bottom
            btnLogout = CreateNavButton("⬅  Logout", 580);
            btnLogout.ForeColor = Color.FromArgb(231, 76, 60);

            // Wire up clicks
            btnDashboard.Click += (s, e) => { 
                LoadPanel(new DashboardPanel()); 
                SetActiveButton(btnDashboard); };
            btnBooks.Click += (s, e) => {
                LoadPanel(new ManageBooksPanel());
                SetActiveButton(btnBooks);
            };
            btnArchivedBooks.Click += (s, e) => {
                LoadPanel(new ArchivedBooksPanel());
                SetActiveButton(btnArchivedBooks);
            };
            btnUsers.Click += (s, e) => {
                LoadPanel(new ManageStudentsPanel());
                SetActiveButton(btnUsers);
            };
            btnAttendance.Click += (s, e) => {
                LoadPanel(new ManageAttendancePanel());
                SetActiveButton(btnAttendance);
            };
            btnLogout.Click += BtnLogout_Click;

            btnBorrow.Click += (s, e) => {
                LoadPanel(new BorrowBooksPanel());
                SetActiveButton(btnBorrow);
            };
            btnFines.Click += (s, e) => {
                LoadPanel(new FinesPanel());
                SetActiveButton(btnFines);
            };
            btnReports.Click += (s, e) => {
                LoadPanel(new ReportsPanel());
                SetActiveButton(btnReports);
            };

            btnSettings.Click += (s, e) => {
                LoadPanel(new SettingsPanel());
                SetActiveButton(btnSettings);
            };

            // ── Add to Sidebar ─────────────────────────────
            sidebar.Controls.Add(lblLibrary);
            sidebar.Controls.Add(lblAppName);
            sidebar.Controls.Add(lblAppSub);
            sidebar.Controls.Add(divider);
            sidebar.Controls.Add(btnDashboard);
            sidebar.Controls.Add(btnBooks);
            sidebar.Controls.Add(btnArchivedBooks);
            sidebar.Controls.Add(btnUsers);
            sidebar.Controls.Add(btnAttendance);
            sidebar.Controls.Add(btnBorrow);
            sidebar.Controls.Add(btnReports);
            sidebar.Controls.Add(btnFines);
            sidebar.Controls.Add(btnSettings);
            sidebar.Controls.Add(btnLogout);

            // ── Content Area ───────────────────────────────
            contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 255),
                Padding = new Padding(10)
            };

            // ── Add to Form ────────────────────────────────
            this.Controls.Add(contentArea);
            this.Controls.Add(sidebar);

            if (!Session.IsAdmin)
            {
                btnSettings.Visible = false;
                btnSettings.Enabled = false;
            }
        }

        // ── Helpers ────────────────────────────────────────
        private Button CreateNavButton(string text, int top)
        {
            return new Button
            {
                Text = text,
                Location = new Point(0, top),
                Size = new Size(220, 45),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 170, 220),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(50, 40, 90) }
            };
        }

        private void SetActiveButton(Button btn)
        {
            // Reset previous
            if (_activeButton != null)
            {
                _activeButton.BackColor = Color.Transparent;
                _activeButton.ForeColor = Color.FromArgb(180, 170, 220);
            }

            // Highlight active
            btn.BackColor = Color.FromArgb(95, 75, 180);
            btn.ForeColor = Color.White;
            _activeButton = btn;
        }

        public void LoadPanel(Panel panel)
        {
            contentArea.Controls.Clear();
            panel.Dock = DockStyle.Fill;
            contentArea.Controls.Add(panel);
        }

        private void BtnLogout_Click(object sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to logout?",
                "Logout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (confirm == DialogResult.Yes)
            {
                Session.Clear();
                Login login = new Login();
                login.Show();
                this.Close();
            }
        }
    }
}