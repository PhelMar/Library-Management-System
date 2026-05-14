using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Borrow
{
    public class FinesPanel : Panel
    {
        private DataGridView dgvFines;
        private TextBox txtSearch;
        private ComboBox cmbStatusFilter;
        private Label lblPagination;
        private Label lblSummary;
        private Button btnPrev, btnNext;

        private readonly TransactionRepository _repo;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PAGE_SIZE = 10;

        private const int EM_SETCUEBANNER = 0x1501;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        public FinesPanel()
        {
            _repo = new TransactionRepository();
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(245, 245, 255);
            this.Padding = new Padding(25, 20, 25, 20);
            BuildUI();
            LoadFines();
        }

        private void BuildUI()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblTitle = new Label
            {
                Text = "Fines & Overdue",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(0, 10),
                AutoSize = true
            };
            titlePanel.Controls.Add(lblTitle);

            var summaryPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            lblSummary = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(231, 76, 60),
                Location = new Point(0, 8),
                AutoSize = true
            };
            summaryPanel.Controls.Add(lblSummary);

            var toolbar = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(0, 11),
                Size = new Size(280, 32),
                BorderStyle = BorderStyle.FixedSingle
            };
            SetCueBanner(txtSearch, "Search student name, ID or book...");
            txtSearch.TextChanged += (s, e) => { _currentPage = 1; LoadFines(); };

            cmbStatusFilter = new ComboBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(290, 11),
                Size = new Size(140, 32),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatusFilter.Items.AddRange(new object[] { "All", "unpaid", "paid" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += (s, e) => { _currentPage = 1; LoadFines(); };

            toolbar.Controls.Add(txtSearch);
            toolbar.Controls.Add(cmbStatusFilter);

            dgvFines = new DataGridView
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

            dgvFines.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };

            dgvFines.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 30, 80),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80),
                Padding = new Padding(5, 0, 0, 0)
            };

            dgvFines.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 247, 255),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80)
            };

            dgvFines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNo", HeaderText = "No.", FillWeight = 4, MinimumWidth = 45 });
            dgvFines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStudentNo", HeaderText = "Student No.", FillWeight = 11 });
            dgvFines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStudentName", HeaderText = "Student Name", FillWeight = 20 });
            dgvFines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBook", HeaderText = "Book Title", FillWeight = 20 });
            dgvFines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDueDate", HeaderText = "Due Date", FillWeight = 11 });
            dgvFines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDays", HeaderText = "Days Overdue", FillWeight = 10 });
            dgvFines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount", HeaderText = "Fine Amount", FillWeight = 10 });
            dgvFines.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Status", FillWeight = 9 });
            dgvFines.Columns.Add(new DataGridViewButtonColumn { Name = "colPay", HeaderText = "", Text = "Mark Paid", UseColumnTextForButtonValue = true, FillWeight = 10 });

            dgvFines.CellContentClick += DgvFines_CellContentClick;
            dgvFines.CellFormatting += DgvFines_CellFormatting;

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
            btnPrev.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; LoadFines(); } };

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
            btnNext.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; LoadFines(); } };

            paginationPanel.Controls.Add(btnPrev);
            paginationPanel.Controls.Add(lblPagination);
            paginationPanel.Controls.Add(btnNext);

            mainLayout.Controls.Add(titlePanel, 0, 0);
            mainLayout.Controls.Add(summaryPanel, 0, 1);
            mainLayout.Controls.Add(toolbar, 0, 2);
            mainLayout.Controls.Add(dgvFines, 0, 3);
            mainLayout.Controls.Add(paginationPanel, 0, 4);

            this.Controls.Add(mainLayout);
        }

        public void LoadFines()
        {
            try
            {
                string search = txtSearch?.Text.Trim() ?? "";
                string status = (cmbStatusFilter != null && cmbStatusFilter.SelectedIndex > 0)
                    ? cmbStatusFilter.SelectedItem.ToString()
                    : "";

                var result = _repo.GetFinesPaged(search, status, _currentPage, PAGE_SIZE, out int totalCount);

                _totalPages = (int)Math.Ceiling((double)totalCount / PAGE_SIZE);
                if (_totalPages < 1) _totalPages = 1;

                dgvFines.Rows.Clear();

                decimal totalUnpaid = 0;
                int unpaidCount = 0;

                int rowNo = (_currentPage - 1) * PAGE_SIZE + 1;
                foreach (DataRow row in result.Rows)
                {
                    string fineStatus = row["fine_status"].ToString();
                    decimal amount = Convert.ToDecimal(row["amount"]);
                    string dueDate = Convert.ToDateTime(row["due_date"]).ToString("MMM dd, yyyy");

                    if (fineStatus == "unpaid")
                    {
                        totalUnpaid += amount;
                        unpaidCount++;
                    }

                    int index = dgvFines.Rows.Add(
                        rowNo++,
                        row["student_no"],
                        row["student_name"],
                        row["book_title"],
                        dueDate,
                        row["days_overdue"] + " day(s)",
                        "₱" + amount.ToString("N2"),
                        fineStatus,
                        fineStatus == "paid" ? "Paid ✓" : "Mark Paid"
                    );

                    dgvFines.Rows[index].Tag = Convert.ToInt32(row["fine_id"]);
                }

                lblPagination.Text = $"Page {_currentPage} of {_totalPages}";
                btnPrev.Enabled = _currentPage > 1;
                btnNext.Enabled = _currentPage < _totalPages;

                lblSummary.Text = unpaidCount > 0
                    ? $"⚠  {unpaidCount} unpaid fine(s) — Total: ₱{totalUnpaid:N2}"
                    : "✓  No unpaid fines on this page.";
                lblSummary.ForeColor = unpaidCount > 0
                    ? Color.FromArgb(231, 76, 60)
                    : Color.FromArgb(39, 174, 96);
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Unable to load fines. Please try again.\n\nDetails: " + ex.Message);
            }
        }

        private void DgvFines_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string col = dgvFines.Columns[e.ColumnIndex].Name;

            if (col == "colStatus")
            {
                string val = e.Value?.ToString() ?? "";
                if (val == "unpaid")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(231, 76, 60);
                    e.CellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                }
                else if (val == "paid")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(39, 174, 96);
                    e.CellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
                }
            }
            else if (col == "colPay")
            {
                var cell = dgvFines.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null)
                {
                    string fineStatus = dgvFines.Rows[e.RowIndex].Cells["colStatus"].Value?.ToString();
                    if (fineStatus == "paid")
                    {
                        cell.Style.BackColor = Color.FromArgb(39, 174, 96);
                        cell.Style.ForeColor = Color.White;
                    }
                    else
                    {
                        cell.Style.BackColor = Color.FromArgb(243, 156, 18);
                        cell.Style.ForeColor = Color.White;
                    }
                }
            }
        }

        private void DgvFines_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvFines.Columns[e.ColumnIndex].Name != "colPay") return;

            string fineStatus = dgvFines.Rows[e.RowIndex].Cells["colStatus"].Value?.ToString();
            if (fineStatus == "paid")
            {
                MessageHelper.ShowWarning("This fine has already been paid.");
                return;
            }

            int fineId = Convert.ToInt32(dgvFines.Rows[e.RowIndex].Tag);
            string studentName = dgvFines.Rows[e.RowIndex].Cells["colStudentName"].Value?.ToString();
            string amount = dgvFines.Rows[e.RowIndex].Cells["colAmount"].Value?.ToString();

            var confirm = MessageHelper.ShowConfirm(
                $"Mark fine as paid?\n\nStudent : {studentName}\nAmount  : {amount}\nMethod  : Cash");

            if (confirm != DialogResult.Yes) return;

            try
            {
                _repo.MarkFinePaid(fineId);
                MessageHelper.ShowSuccess($"Fine for {studentName} marked as paid successfully.");
                LoadFines();
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Unable to process payment. Please try again.\n\nDetails: " + ex.Message);
            }
        }

        public void SearchStudent(string studentName)
        {
            if (txtSearch != null)
            {
                txtSearch.Text = studentName;
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
    }
}