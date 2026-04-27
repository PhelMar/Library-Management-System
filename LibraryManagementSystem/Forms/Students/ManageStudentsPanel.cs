using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Students
{
    public class ManageStudentsPanel : Panel
    {
        private DataGridView dgvStudents;
        private TextBox txtSearch;
        private Button btnAddStudent;
        private Label lblPagination;
        private Label lblActivePeriod;
        private Button btnPrev, btnNext;

        private readonly StudentRepository _studentRepo;

        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PAGE_SIZE = 10;


        private const int EM_SETCUEBANNER = 0x1501;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        public ManageStudentsPanel()
        {
            _studentRepo = new StudentRepository();
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(245, 245, 255);
            this.Padding = new Padding(25, 20, 25, 20);
            BuildUI();
            LoadStudents();
        }

        private void BuildUI()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));  // Title + active period
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));  // Search + Add
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Table
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));  // Pagination
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // ── Row 0: Title ──────────────────────────────
            var titlePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = "Manage Students",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(0, 5),
                AutoSize = true
            };

            // Show active school year and semester
            var period = _studentRepo.GetActivePeriod();
            lblActivePeriod = new Label
            {
                Text = period.schoolYearId > 0
                            ? $"Active: {period.schoolYearLabel}  |  {period.semesterName}"
                            : "⚠ No active school year or semester set!",
                Font = new Font("Segoe UI", 9f),
                ForeColor = period.schoolYearId > 0
                            ? Color.FromArgb(39, 174, 96)
                            : Color.FromArgb(231, 76, 60),
                Location = new Point(2, 45),
                AutoSize = true
            };

            titlePanel.Controls.Add(lblTitle);
            titlePanel.Controls.Add(lblActivePeriod);

            // ── Row 1: Search + Add ───────────────────────
            var topBar = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            txtSearch = new TextBox
            {   
                Font = new Font("Segoe UI", 10f),
                Location = new Point(0, 10),
                Size = new Size(320, 32),
                BorderStyle = BorderStyle.FixedSingle
            };
            SetCueBanner(txtSearch, "Search by student ID, name or course...");

            txtSearch.TextChanged += (s, e) => { _currentPage = 1; LoadStudents(); };

            btnAddStudent = new Button
            {
                Text = "+ Add Student",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Size = new Size(140, 34),
                Location = new Point(330, 8)
            };
            btnAddStudent.FlatAppearance.BorderSize = 0;
            btnAddStudent.Click += BtnAddStudent_Click;

            topBar.Controls.Add(txtSearch);
            topBar.Controls.Add(btnAddStudent);

            // ── Row 2: DataGridView ───────────────────────
            dgvStudents = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 10f),
                GridColor = Color.FromArgb(230, 230, 240),
                ColumnHeadersHeight = 40,
                RowTemplate = { Height = 40 }
            };

            dgvStudents.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Padding(5, 0, 0, 0)
            };

            dgvStudents.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 30, 80),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80),
                Padding = new Padding(5, 0, 0, 0)
            };

            dgvStudents.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 247, 255),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80)
            };

            // Columns
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNo", HeaderText = "No.", FillWeight = 5 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStudentId", HeaderText = "Student ID", FillWeight = 15 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Full Name", FillWeight = 25 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCourse", HeaderText = "Course", FillWeight = 15 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colYear", HeaderText = "Year Level", FillWeight = 12 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSemester", HeaderText = "Semester", FillWeight = 13 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSY", HeaderText = "School Year", FillWeight = 13 });

            dgvStudents.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colEdit",
                HeaderText = "",
                Text = "Edit",
                UseColumnTextForButtonValue = true,
                FillWeight = 8
            });
            dgvStudents.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colDelete",
                HeaderText = "",
                Text = "Delete",
                UseColumnTextForButtonValue = true,
                FillWeight = 8
            });

            dgvStudents.CellContentClick += DgvStudents_CellContentClick;
            dgvStudents.CellFormatting += DgvStudents_CellFormatting;

            // ── Row 3: Pagination ─────────────────────────
            var paginationPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            btnPrev = new Button
            {
                Text = "← Prev",
                Font = new Font("Segoe UI", 9f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Size = new Size(80, 30),
                Location = new Point(0, 8),
                Cursor = Cursors.Hand
            };
            btnPrev.FlatAppearance.BorderSize = 0;
            btnPrev.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; LoadStudents(); } };

            lblPagination = new Label
            {
                Text = "Page 1 of 1",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(90, 13),
                AutoSize = true
            };

            btnNext = new Button
            {
                Text = "Next →",
                Font = new Font("Segoe UI", 9f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Size = new Size(80, 30),
                Location = new Point(200, 8),
                Cursor = Cursors.Hand
            };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; LoadStudents(); } };

            paginationPanel.Controls.Add(btnPrev);
            paginationPanel.Controls.Add(lblPagination);
            paginationPanel.Controls.Add(btnNext);

            // ── Assemble ──────────────────────────────────
            mainLayout.Controls.Add(titlePanel, 0, 0);
            mainLayout.Controls.Add(topBar, 0, 1);
            mainLayout.Controls.Add(dgvStudents, 0, 2);
            mainLayout.Controls.Add(paginationPanel, 0, 3);

            this.Controls.Add(mainLayout);
        }

        public void LoadStudents()
        {
            try
            {
                string search = txtSearch?.Text.Trim() ?? "";
                var result = _studentRepo.GetStudentsPaged(search, _currentPage, PAGE_SIZE, out int totalCount);

                _totalPages = (int)Math.Ceiling((double)totalCount / PAGE_SIZE);
                if (_totalPages < 1) _totalPages = 1;

                dgvStudents.Rows.Clear();

                int rowNo = (_currentPage - 1) * PAGE_SIZE + 1;
                foreach (DataRow row in result.Rows)
                {
                    int index = dgvStudents.Rows.Add(
                        rowNo++,
                        row["student_code"],
                        row["student_name"],
                        row["course_code"],
                        row["level_name"],
                        row["semester_name"],
                        row["year_label"],
                        "Edit", "Delete"
                    );

                    // Store both student db id and enrollment id in Tag
                    dgvStudents.Rows[index].Tag = new int[]
                    {
                        Convert.ToInt32(row["id"]),
                        Convert.ToInt32(row["enrollment_id"])
                    };
                }

                lblPagination.Text = $"Page {_currentPage} of {_totalPages}";
                btnPrev.Enabled = _currentPage > 1;
                btnNext.Enabled = _currentPage < _totalPages;
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Failed to load students: " + ex.Message);
            }
        }

        private void DgvStudents_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var colName = dgvStudents.Columns[e.ColumnIndex].Name;

            if (colName == "colEdit")
            {
                var cell = dgvStudents.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null) { cell.Style.BackColor = Color.FromArgb(230, 126, 34); cell.Style.ForeColor = Color.White; }
            }
            else if (colName == "colDelete")
            {
                var cell = dgvStudents.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null) { cell.Style.BackColor = Color.FromArgb(231, 76, 60); cell.Style.ForeColor = Color.White; }
            }
        }

        private void DgvStudents_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var ids = (int[])dgvStudents.Rows[e.RowIndex].Tag;
            int studentDbId = ids[0];
            int enrollmentId = ids[1];
            string colName = dgvStudents.Columns[e.ColumnIndex].Name;

            if (colName == "colEdit")
            {
                var form = new StudentAddEditForm(studentDbId, enrollmentId);
                form.FormClosed += (s, ev) => LoadStudents();
                form.ShowDialog();
            }
            else if (colName == "colDelete")
            {
                var confirm = MessageHelper.ShowConfirm(
                    "Delete this student?\nThis will also remove all their enrollment records.");

                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        _studentRepo.DeleteStudent(studentDbId);
                        MessageHelper.ShowSuccess("Student deleted successfully.");
                        LoadStudents();
                    }
                    catch (Exception ex)
                    {
                        MessageHelper.ShowError("Failed to delete: " + ex.Message);
                    }
                }
            }
        }

        private void SetCueBanner(TextBox tb, string cue)
        {
            if (tb == null) return;
            if (tb.IsHandleCreated)
            {
                SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, cue);
            }
            else
            {
                // If handle not created yet, set when created
                void Handler(object s, EventArgs e)
                {
                    tb.HandleCreated -= Handler;
                    SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, cue);
                }
                tb.HandleCreated += Handler;
            }
        }

        private void BtnAddStudent_Click(object sender, EventArgs e)
        {
            var form = new StudentAddEditForm(0, 0);
            form.FormClosed += (s, ev) => LoadStudents();
            form.ShowDialog();
        }


    }
}