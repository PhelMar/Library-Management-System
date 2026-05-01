using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Settings
{
    public class SettingsPanel : Panel
    {
        private Panel pnlLeftNav;
        private Panel pnlContent;
        private Button _activeNavBtn;
        private readonly SettingsRepository _repo;

        public SettingsPanel()
        {
            _repo = new SettingsRepository();
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(245, 245, 255);
            this.Padding = new Padding(25, 20, 25, 20);
            BuildUI();
            LoadSection("School Year");
        }

        private void BuildUI()
        {
            var titlePanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = "Settings",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(0, 10),
                AutoSize = true
            };
            titlePanel.Controls.Add(lblTitle);

            var bodyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 185f));
            bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            pnlLeftNav = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(30, 20, 60),
                Padding = new Padding(0, 10, 0, 10)
            };

            string[] sections = {
                "School Year", "Semester", "Categories",
                "Courses", "Year Levels", "Librarians & Users"
            };

            int top = 10;
            foreach (var section in sections)
            {
                var btn = CreateNavBtn(section, top);
                btn.Click += (s, e) =>
                {
                    SetActiveNav(btn);
                    LoadSection(section);
                };
                pnlLeftNav.Controls.Add(btn);
                top += 48;
            }

            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(25, 20, 25, 20)
            };

            bodyLayout.Controls.Add(pnlLeftNav, 0, 0);
            bodyLayout.Controls.Add(pnlContent, 1, 0);

            this.Controls.Add(bodyLayout);
            this.Controls.Add(titlePanel);
        }

        private Button CreateNavBtn(string text, int top)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(0, top),
                Size = new Size(185, 42),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(180, 170, 220),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 10f),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(15, 0, 0, 0),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0, MouseOverBackColor = Color.FromArgb(50, 40, 90) }
            };
            return btn;
        }

        private void SetActiveNav(Button btn)
        {
            if (_activeNavBtn != null)
            {
                _activeNavBtn.BackColor = Color.Transparent;
                _activeNavBtn.ForeColor = Color.FromArgb(180, 170, 220);
            }
            btn.BackColor = Color.FromArgb(95, 75, 180);
            btn.ForeColor = Color.White;
            _activeNavBtn = btn;
        }

        private void LoadSection(string section)
        {
            pnlContent.Controls.Clear();

            Panel sectionPanel = null;

            switch (section)
            {
                case "School Year": sectionPanel = new SchoolYearSection(_repo); break;
                case "Semester": sectionPanel = new SemesterSection(_repo); break;
                case "Categories": sectionPanel = new CategorySection(_repo); break;
                case "Courses": sectionPanel = new CourseSection(_repo); break;
                case "Year Levels": sectionPanel = new YearLevelSection(_repo); break;
                case "Librarians & Users": sectionPanel = new LibrarianUserSection(_repo); break;
            }

            if (sectionPanel == null) return;
            sectionPanel.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(sectionPanel);

            // Auto-activate first nav button
            if (_activeNavBtn == null)
            {
                foreach (Control c in pnlLeftNav.Controls)
                {
                    if (c is Button b && b.Text == section)
                    {
                        SetActiveNav(b);
                        break;
                    }
                }
            }
        }
    }
}