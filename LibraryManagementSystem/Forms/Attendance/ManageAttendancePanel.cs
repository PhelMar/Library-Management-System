using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Attendance
{
    public class ManageAttendancePanel : Panel
    {
        private DataGridView dgvAttendance;
        private TextBox txtStudentId;
        private Button btnCheckIn, btnCheckOut, btnSearch, btnRefresh;
        private Label lblStatus, lblPagination;
        private Button btnPrev, btnNext;
        private DateTimePicker dtpFromDate, dtpToDate;
        private Label lblAttendanceInfo;

        private readonly LibraryAttendanceRepository _attendanceRepo;

        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PAGE_SIZE = 10;

        private bool _isViewingDateRange = false;
        private DateTime _fromDate = DateTime.Today;
        private DateTime _toDate = DateTime.Today;

        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        public ManageAttendancePanel()
        {
            _attendanceRepo = new LibraryAttendanceRepository();
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(245, 245, 255);
            this.Padding = new Padding(25, 20, 25, 20);
            BuildUI();
            LoadAttendance();
        }

        private void BuildUI()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                BackColor = Color.Transparent
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));      // Title
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));      // Check-in/out controls
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));      // Filter controls
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));      // DataGrid
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));      // Status message
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));      // Pagination
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // ===== ROW 0: TITLE =====
            var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblTitle = new Label
            {
                Text = "Library Attendance",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(0, 10),
                AutoSize = true
            };
            titlePanel.Controls.Add(lblTitle);

            // ===== ROW 1: CHECK-IN/OUT =====
            var checkInPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            txtStudentId = new TextBox
            {
                Font = new Font("Segoe UI", 11f),
                Location = new Point(0, 11),
                Size = new Size(200, 34),
                BorderStyle = BorderStyle.FixedSingle
            };
            SetCueBanner(txtStudentId, "Student ID");
            txtStudentId.KeyPress += (s, e) => { if (e.KeyChar == (char)Keys.Return) { BtnCheckIn_Click(null, null); e.Handled = true; } };

            btnCheckIn = new Button
            {
                Text = "✓ Check In",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(46, 204, 113),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Size = new Size(110, 34),
                Location = new Point(208, 9)
            };
            btnCheckIn.FlatAppearance.BorderSize = 0;
            btnCheckIn.Click += BtnCheckIn_Click;

            btnCheckOut = new Button
            {
                Text = "✗ Check Out",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Size = new Size(110, 34),
                Location = new Point(326, 9)
            };
            btnCheckOut.FlatAppearance.BorderSize = 0;
            btnCheckOut.Click += BtnCheckOut_Click;

            btnRefresh = new Button
            {
                Text = "🔄 Refresh",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 152, 219),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Size = new Size(110, 34),
                Location = new Point(444, 9)
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => { _currentPage = 1; LoadAttendance(); };

            checkInPanel.Controls.Add(txtStudentId);
            checkInPanel.Controls.Add(btnCheckIn);
            checkInPanel.Controls.Add(btnCheckOut);
            checkInPanel.Controls.Add(btnRefresh);

            // ===== ROW 2: DATE FILTER =====
            var filterPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            var lblFrom = new Label
            {
                Text = "From:",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(0, 15),
                AutoSize = true
            };

            dtpFromDate = new DateTimePicker
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(40, 11),
                Size = new Size(140, 32),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            dtpFromDate.ValueChanged += (s, e) => { _fromDate = dtpFromDate.Value; _currentPage = 1; LoadAttendanceByRange(); };

            var lblTo = new Label
            {
                Text = "To:",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(188, 15),
                AutoSize = true
            };

            dtpToDate = new DateTimePicker
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(210, 11),
                Size = new Size(140, 32),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today
            };
            dtpToDate.ValueChanged += (s, e) => { _toDate = dtpToDate.Value; _currentPage = 1; LoadAttendanceByRange(); };

            btnSearch = new Button
            {
                Text = "🔍 Search",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(155, 89, 182),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Size = new Size(100, 34),
                Location = new Point(358, 9)
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += (s, e) => { _currentPage = 1; LoadAttendanceByRange(); };

            filterPanel.Controls.Add(lblFrom);
            filterPanel.Controls.Add(dtpFromDate);
            filterPanel.Controls.Add(lblTo);
            filterPanel.Controls.Add(dtpToDate);
            filterPanel.Controls.Add(btnSearch);

            // ===== ROW 3: DATAGRIDVIEW =====
            dgvAttendance = new DataGridView
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

            dgvAttendance.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };

            dgvAttendance.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 30, 80),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80),
                Padding = new Padding(5, 0, 0, 0)
            };

            dgvAttendance.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 247, 255),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80)
            };

            // Add columns
            dgvAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNo", HeaderText = "No.", FillWeight = 5, MinimumWidth = 40 });
            dgvAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStudentId", HeaderText = "Student ID", FillWeight = 12 });
            dgvAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStudentName", HeaderText = "Name", FillWeight = 20 });
            dgvAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCourse", HeaderText = "Course", FillWeight = 18 });
            dgvAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLevel", HeaderText = "Level", FillWeight = 10 });
            dgvAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTimeIn", HeaderText = "Check In", FillWeight = 13 });
            dgvAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTimeOut", HeaderText = "Check Out", FillWeight = 13 });
            dgvAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDuration", HeaderText = "Duration", FillWeight = 12 });
            dgvAttendance.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Status", FillWeight = 12 });

            dgvAttendance.CellFormatting += DgvAttendance_CellFormatting;

            // ===== ROW 4: STATUS MESSAGE =====
            lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(52, 152, 219),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };

            // ===== ROW 5: PAGINATION =====
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
            btnPrev.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; LoadAttendance(); } };

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
                Location = new Point(235, 8),
                Cursor = Cursors.Hand
            };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; LoadAttendance(); } };

            paginationPanel.Controls.Add(btnPrev);
            paginationPanel.Controls.Add(lblPagination);
            paginationPanel.Controls.Add(btnNext);

            // Add all rows to main layout
            mainLayout.Controls.Add(titlePanel, 0, 0);
            mainLayout.Controls.Add(checkInPanel, 0, 1);
            mainLayout.Controls.Add(filterPanel, 0, 2);
            mainLayout.Controls.Add(dgvAttendance, 0, 3);
            mainLayout.Controls.Add(lblStatus, 0, 4);
            mainLayout.Controls.Add(paginationPanel, 0, 5);

            this.Controls.Add(mainLayout);
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
                void Handler(object s, EventArgs e)
                {
                    tb.HandleCreated -= Handler;
                    SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, cue);
                }
                tb.HandleCreated += Handler;
            }
        }

        private void BtnCheckIn_Click(object sender, EventArgs e)
        {
            string studentId = txtStudentId.Text.Trim();

            if (string.IsNullOrEmpty(studentId))
            {
                MessageHelper.ShowWarning("Please enter a Student ID");
                txtStudentId.Focus();
                return;
            }

            if (_attendanceRepo.CheckInStudent(studentId, out string message))
            {
                ShowStatusMessage(message, Color.FromArgb(46, 204, 113)); // Green
                txtStudentId.Clear();
                txtStudentId.Focus();
                LoadAttendance();
            }
            else
            {
                ShowStatusMessage(message, Color.FromArgb(231, 76, 60)); // Red
                txtStudentId.Clear();
                txtStudentId.Focus();
            }
        }

        private void BtnCheckOut_Click(object sender, EventArgs e)
        {
            string studentId = txtStudentId.Text.Trim();

            if (string.IsNullOrEmpty(studentId))
            {
                MessageHelper.ShowWarning("Please enter a Student ID");
                txtStudentId.Focus();
                return;
            }

            if (_attendanceRepo.CheckOutStudent(studentId, out string message))
            {
                ShowStatusMessage(message, Color.FromArgb(46, 204, 113)); // Green
                txtStudentId.Clear();
                txtStudentId.Focus();
                LoadAttendance();
            }
            else
            {
                ShowStatusMessage(message, Color.FromArgb(231, 76, 60)); // Red
                txtStudentId.Clear();
                txtStudentId.Focus();
            }
        }

        public void LoadAttendance()
        {
            try
            {
                _isViewingDateRange = false;
                string search = txtStudentId.Text.Trim();

                var result = _attendanceRepo.GetTodayAttendancePaged(
                    search,
                    _currentPage,
                    PAGE_SIZE,
                    out int totalCount
                );

                _totalPages = (int)Math.Ceiling((double)totalCount / PAGE_SIZE);
                if (_totalPages < 1) _totalPages = 1;

                dgvAttendance.Rows.Clear();

                int rowNo = (_currentPage - 1) * PAGE_SIZE + 1;
                foreach (DataRow row in result.Rows)
                {
                    dgvAttendance.Rows.Add(
                        rowNo++,
                        row["student_id"],
                        row["student_name"],
                        row["course_name"],
                        row["level_name"],
                        Convert.ToDateTime(row["time_in"]).ToString("HH:mm:ss"),
                        row["time_out"] == DBNull.Value ? "Active" : Convert.ToDateTime(row["time_out"]).ToString("HH:mm:ss"),
                        row["duration"],
                        row["status"]
                    );
                }

                lblPagination.Text = $"Page {_currentPage} of {_totalPages} | Total: {totalCount}";
                btnPrev.Enabled = _currentPage > 1;
                btnNext.Enabled = _currentPage < _totalPages;

                ShowStatusMessage($"Showing {result.Rows.Count} records", Color.FromArgb(52, 152, 219));
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError($"Unable to load attendance. Please try again.\n\nDetails: {ex.Message}");
            }
        }

        private void LoadAttendanceByRange()
        {
            try
            {
                _isViewingDateRange = true;
                string search = txtStudentId.Text.Trim();

                var result = _attendanceRepo.GetAttendanceByDateRangePaged(
                    _fromDate,
                    _toDate,
                    search,
                    _currentPage,
                    PAGE_SIZE,
                    out int totalCount
                );

                _totalPages = (int)Math.Ceiling((double)totalCount / PAGE_SIZE);
                if (_totalPages < 1) _totalPages = 1;

                dgvAttendance.Rows.Clear();

                int rowNo = (_currentPage - 1) * PAGE_SIZE + 1;
                foreach (DataRow row in result.Rows)
                {
                    dgvAttendance.Rows.Add(
                        rowNo++,
                        row["student_id"],
                        row["student_name"],
                        row["course_name"],
                        row["level_name"],
                        Convert.ToDateTime(row["time_in"]).ToString("HH:mm:ss"),
                        row["time_out"] == DBNull.Value ? "Active" : Convert.ToDateTime(row["time_out"]).ToString("HH:mm:ss"),
                        row["duration"],
                        row["status"]
                    );
                }

                lblPagination.Text = $"Page {_currentPage} of {_totalPages} | Total: {totalCount}";
                btnPrev.Enabled = _currentPage > 1;
                btnNext.Enabled = _currentPage < _totalPages;

                ShowStatusMessage($"Showing {result.Rows.Count} records from {_fromDate:MMM dd, yyyy} to {_toDate:MMM dd, yyyy}", Color.FromArgb(52, 152, 219));
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError($"Unable to load attendance. Please try again.\n\nDetails: {ex.Message}");
            }
        }

        private void DgvAttendance_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string col = dgvAttendance.Columns[e.ColumnIndex].Name;

            if (col == "colStatus")
            {
                string status = dgvAttendance.Rows[e.RowIndex].Cells[e.ColumnIndex].Value?.ToString();
                if (status == "In Library")
                {
                    dgvAttendance.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.BackColor = Color.FromArgb(236, 240, 241);
                    dgvAttendance.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = Color.FromArgb(46, 204, 113);
                    dgvAttendance.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                }
                else if (status == "Checked Out")
                {
                    dgvAttendance.Rows[e.RowIndex].Cells[e.ColumnIndex].Style.ForeColor = Color.FromArgb(149, 165, 166);
                }
            }
        }

        private void ShowStatusMessage(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
        }
    }
}