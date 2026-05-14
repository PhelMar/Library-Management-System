using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;

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

        private ComboBox cmbCourseFilter;
        private List<int> _courseIds = new List<int>();

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

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            var lblTitle = new Label
            {
                Text = "Manage Students",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(0, 5),
                AutoSize = true
            };

            var period = _studentRepo.GetActivePeriod();
            lblActivePeriod = new Label
            {
                Text = period.schoolYearId > 0
                    ? $"Active: {period.schoolYearLabel}  |  {period.semesterName}"
                    : "⚠ No active school year found!",
                Font = new Font("Segoe UI", 9f),
                ForeColor = period.schoolYearId > 0
                    ? Color.FromArgb(39, 174, 96)
                    : Color.FromArgb(231, 76, 60),
                Location = new Point(2, 45),
                AutoSize = true
            };

            titlePanel.Controls.Add(lblTitle);
            titlePanel.Controls.Add(lblActivePeriod);

            var topBar = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(0, 10),
                Size = new Size(320, 32),
                BorderStyle = BorderStyle.FixedSingle
            };
            SetCueBanner(txtSearch, "Search by student ID, name or course...");
            txtSearch.TextChanged += (s, e) => { _currentPage = 1; LoadStudents(); };

            cmbCourseFilter = new ComboBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(330, 10),
                Size = new Size(200, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };

            cmbCourseFilter = new ComboBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(330, 10),
                Size = new Size(200, 32),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat
            };

            var courses = _studentRepo.GetCourses();

            _courseIds.Clear();
            _courseIds.Add(0);
            cmbCourseFilter.Items.Add("All Courses");

            foreach (DataRow row in courses.Rows)
            {
                _courseIds.Add(Convert.ToInt32(row["id"]));
                cmbCourseFilter.Items.Add(row["display_name"].ToString());
            }

            cmbCourseFilter.SelectedIndex = 0;
            cmbCourseFilter.SelectedIndexChanged += (s, e) => { _currentPage = 1; LoadStudents(); };



            btnAddStudent = new Button
            {
                Text = "+ Add Student",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Size = new Size(140, 34),
                Location = new Point(540, 8)
            };
            btnAddStudent.FlatAppearance.BorderSize = 0;
            btnAddStudent.Click += BtnAddStudent_Click;

            topBar.Controls.Add(txtSearch);
            topBar.Controls.Add(cmbCourseFilter);
            topBar.Controls.Add(btnAddStudent);

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

            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNo", HeaderText = "No.", FillWeight = 4 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStudentId", HeaderText = "Student ID", FillWeight = 12 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Full Name", FillWeight = 22 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCourse", HeaderText = "Course", FillWeight = 12 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colYear", HeaderText = "Year Level", FillWeight = 10 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSemester", HeaderText = "Semester", FillWeight = 11 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSY", HeaderText = "School Year", FillWeight = 10 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Status", FillWeight = 10 });

            dgvStudents.Columns.Add(new DataGridViewButtonColumn { Name = "colEdit", HeaderText = "", Text = "Edit", UseColumnTextForButtonValue = true, FillWeight = 7 });
            dgvStudents.Columns.Add(new DataGridViewButtonColumn { Name = "colDrop", HeaderText = "", Text = "Drop", UseColumnTextForButtonValue = true, FillWeight = 7 });
            dgvStudents.Columns.Add(new DataGridViewButtonColumn { Name = "colGraduate", HeaderText = "", Text = "Graduate", UseColumnTextForButtonValue = true, FillWeight = 9 });

            dgvStudents.CellContentClick += DgvStudents_CellContentClick;
            dgvStudents.CellFormatting += DgvStudents_CellFormatting;

            var paginationPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

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

                int courseId = 0;
                if (cmbCourseFilter != null && cmbCourseFilter.SelectedIndex >= 0)
                    courseId = _courseIds[cmbCourseFilter.SelectedIndex];

                var result = _studentRepo.GetStudentsPaged(
                    search, _currentPage, PAGE_SIZE, out int totalCount, courseId);

                _totalPages = (int)Math.Ceiling((double)totalCount / PAGE_SIZE);
                if (_totalPages < 1) _totalPages = 1;

                dgvStudents.Rows.Clear();

                int rowNo = (_currentPage - 1) * PAGE_SIZE + 1;
                foreach (DataRow row in result.Rows)
                {
                    string status = row["status"].ToString();

                    int index = dgvStudents.Rows.Add(
                        rowNo++,
                        row["student_code"],
                        row["student_name"],
                        row["course_code"],
                        row["level_name"],
                        row["semester_name"],
                        row["year_label"],
                        status,
                        "Edit", "Drop", "Graduate"
                    );

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
                MessageHelper.ShowError("Unable to load students. Please try again.\n\nDetails: " + ex.Message);
            }
        }

        private void DgvStudents_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string col = dgvStudents.Columns[e.ColumnIndex].Name;

            if (col == "colStatus")
            {
                string status = dgvStudents.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString() ?? "";

                if (status == "enrolled")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(39, 174, 96);
                    e.CellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                }
                else if (status == "dropped")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(231, 76, 60);
                    e.CellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                }
                else if (status == "graduated")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(52, 152, 219);
                    e.CellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                }
            }
            else if (col == "colEdit")
            {
                var cell = dgvStudents.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null) { cell.Style.BackColor = Color.FromArgb(230, 126, 34); cell.Style.ForeColor = Color.White; }
            }
            else if (col == "colDrop")
            {
                var cell = dgvStudents.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null) { cell.Style.BackColor = Color.FromArgb(231, 76, 60); cell.Style.ForeColor = Color.White; }
            }
            else if (col == "colGraduate")
            {
                var cell = dgvStudents.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null) { cell.Style.BackColor = Color.FromArgb(52, 152, 219); cell.Style.ForeColor = Color.White; }
            }
        }

        private void DgvStudents_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var ids = (int[])dgvStudents.Rows[e.RowIndex].Tag;
            int studentDbId = ids[0];
            int enrollmentId = ids[1];
            string studentName = dgvStudents.Rows[e.RowIndex].Cells["colName"].Value?.ToString();
            string col = dgvStudents.Columns[e.ColumnIndex].Name;

            if (col == "colEdit")
            {
                var form = new StudentAddEditForm(studentDbId, enrollmentId);
                form.FormClosed += (s, ev) => LoadStudents();
                form.ShowDialog();
            }
            else if (col == "colDrop")
            {
                string currentStatus = dgvStudents.Rows[e.RowIndex].Cells["colStatus"].Value?.ToString();
                if (currentStatus == "dropped")
                {
                    MessageHelper.ShowWarning($"{studentName} is already marked as dropped.");
                    return;
                }

                var confirm = MessageHelper.ShowConfirm(
                    $"Mark \"{studentName}\" as Dropped?\n\nThis will update their enrollment status for this semester.");

                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        _studentRepo.UpdateEnrollmentStatus(enrollmentId, "dropped");
                        MessageHelper.ShowSuccess($"{studentName} has been marked as dropped.");
                        LoadStudents();
                    }
                    catch (Exception ex)
                    {
                        MessageHelper.ShowError("Unable to update status. Please try again.\n\nDetails: " + ex.Message);
                    }
                }
            }
            else if (col == "colGraduate")
            {
                string currentStatus = dgvStudents.Rows[e.RowIndex].Cells["colStatus"].Value?.ToString();
                if (currentStatus == "graduated")
                {
                    MessageHelper.ShowWarning($"{studentName} is already marked as graduated.");
                    return;
                }

                var confirm = MessageHelper.ShowConfirm(
                    $"Mark \"{studentName}\" as Graduated?\n\nThis will update their enrollment status for this semester.");

                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        _studentRepo.UpdateEnrollmentStatus(enrollmentId, "graduated");
                        MessageHelper.ShowSuccess($"{studentName} has been marked as graduated.");
                        LoadStudents();
                    }
                    catch (Exception ex)
                    {
                        MessageHelper.ShowError("Unable to update status. Please try again.\n\nDetails: " + ex.Message);
                    }
                }
            }
        }

        private void SetCueBanner(TextBox tb, string cue)
        {
            if (tb == null) return;
            if (tb.IsHandleCreated)
                SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, cue);
            else
            {
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