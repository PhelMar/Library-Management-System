using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Borrow
{
    public class BorrowBooksPanel : Panel
    {
        // ── Controls ──────────────────────────────────────────────────────────
        private DataGridView dgvTransactions;
        private TextBox txtSearch;
        private ComboBox cmbStatusFilter;
        private Button btnBorrowBook;
        private Button btnPrev, btnNext;
        private Label lblPagination;

        // ── State ─────────────────────────────────────────────────────────────
        private readonly TransactionRepository _repo;
        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PAGE_SIZE = 10;

        // ── Win32 placeholder ─────────────────────────────────────────────────
        private const int EM_SETCUEBANNER = 0x1501;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        // ─────────────────────────────────────────────────────────────────────
        public BorrowBooksPanel()
        {
            _repo = new TransactionRepository();
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(245, 245, 255);
            this.Padding = new Padding(25, 20, 25, 20);
            BuildUI();
            LoadTransactions();
        }

        private void BuildUI()
        {
            TableLayoutPanel mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // ── Row 0 : Title ─────────────────────────────────────────────────
            Panel titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            Label lblTitle = new Label
            {
                Text = "Borrow Books",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(0, 10),
                AutoSize = true
            };
            titlePanel.Controls.Add(lblTitle);

            // ── Row 1 : Toolbar ───────────────────────────────────────────────
            Panel toolbar = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(0, 11),
                Size = new Size(280, 32),
                BorderStyle = BorderStyle.FixedSingle
            };
            SetCueBanner(txtSearch, "Search student or book title...");
            txtSearch.TextChanged += (s, e) => { _currentPage = 1; LoadTransactions(); };

            cmbStatusFilter = new ComboBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(290, 11),
                Size = new Size(140, 32),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbStatusFilter.Items.AddRange(new object[] { "All Status", "borrowed", "returned", "overdue" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += (s, e) => { _currentPage = 1; LoadTransactions(); };

            btnBorrowBook = new Button
            {
                Text = "+ Borrow Book",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Size = new Size(145, 34),
                Location = new Point(440, 9)
            };
            btnBorrowBook.FlatAppearance.BorderSize = 0;
            btnBorrowBook.Click += BtnBorrowBook_Click;

            toolbar.Controls.Add(txtSearch);
            toolbar.Controls.Add(cmbStatusFilter);
            toolbar.Controls.Add(btnBorrowBook);

            // ── Row 2 : DataGridView ──────────────────────────────────────────
            dgvTransactions = new DataGridView
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

            dgvTransactions.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };

            dgvTransactions.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 30, 80),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80),
                Padding = new Padding(5, 0, 0, 0)
            };

            dgvTransactions.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 247, 255),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80)
            };

            // Columns
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNo", HeaderText = "No.", FillWeight = 4, MinimumWidth = 45 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStudentNo", HeaderText = "Student No.", FillWeight = 10 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStudentName", HeaderText = "Student Name", FillWeight = 20 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBookTitle", HeaderText = "Book Title", FillWeight = 22 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLibrarian", HeaderText = "Librarian", FillWeight = 14 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBorrowDate", HeaderText = "Borrow Date", FillWeight = 12 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDueDate", HeaderText = "Due Date", FillWeight = 11 });
            dgvTransactions.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "Status", FillWeight = 9 });

            dgvTransactions.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colReturn",
                HeaderText = "",
                Text = "Return",
                UseColumnTextForButtonValue = true,
                FillWeight = 8
            });
            dgvTransactions.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colView",
                HeaderText = "",
                Text = "View",
                UseColumnTextForButtonValue = true,
                FillWeight = 8
            });

            dgvTransactions.CellContentClick += DgvTransactions_CellContentClick;
            dgvTransactions.CellFormatting += DgvTransactions_CellFormatting;

            // ── Row 3 : Pagination ────────────────────────────────────────────
            Panel paginationPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            btnPrev = new Button
            {
                Text = "<- Prev",
                Font = new Font("Segoe UI", 9f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Size = new Size(80, 30),
                Location = new Point(0, 8),
                Cursor = Cursors.Hand
            };
            btnPrev.FlatAppearance.BorderSize = 0;
            btnPrev.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; LoadTransactions(); } };

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
                Text = "Next ->",
                Font = new Font("Segoe UI", 9f),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Size = new Size(80, 30),
                Location = new Point(200, 8),
                Cursor = Cursors.Hand
            };
            btnNext.FlatAppearance.BorderSize = 0;
            btnNext.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; LoadTransactions(); } };

            paginationPanel.Controls.Add(btnPrev);
            paginationPanel.Controls.Add(lblPagination);
            paginationPanel.Controls.Add(btnNext);

            mainLayout.Controls.Add(titlePanel, 0, 0);
            mainLayout.Controls.Add(toolbar, 0, 1);
            mainLayout.Controls.Add(dgvTransactions, 0, 2);
            mainLayout.Controls.Add(paginationPanel, 0, 3);

            this.Controls.Add(mainLayout);
        }

        public void LoadTransactions()
        {
            try
            {
                string search = txtSearch != null ? txtSearch.Text.Trim() : "";
                string status = (cmbStatusFilter != null && cmbStatusFilter.SelectedIndex > 0)
                    ? cmbStatusFilter.SelectedItem.ToString()
                    : "";

                int totalCount;
                DataTable result = _repo.GetTransactionsPaged(
                    search, status, _currentPage, PAGE_SIZE, out totalCount);

                _totalPages = (int)Math.Ceiling((double)totalCount / PAGE_SIZE);
                if (_totalPages < 1) _totalPages = 1;

                dgvTransactions.Rows.Clear();

                int rowNo = (_currentPage - 1) * PAGE_SIZE + 1;
                foreach (DataRow row in result.Rows)
                {
                    string statusVal = row["status"].ToString();
                    string borrowDate = Convert.ToDateTime(row["borrow_date"]).ToString("MMM dd, yyyy");
                    string dueDate = Convert.ToDateTime(row["due_date"]).ToString("MMM dd, yyyy");

                    dgvTransactions.Rows.Add(
                        rowNo++,
                        row["student_no"],
                        row["student_name"],
                        row["book_title"],
                        row["librarian_name"],
                        borrowDate,
                        dueDate,
                        statusVal.ToUpper()
                    );

                    DataGridViewRow dgvRow = dgvTransactions.Rows[dgvTransactions.Rows.Count - 1];
                    dgvRow.Tag = row["id"];

                    if (statusVal == "returned")
                    {
                        DataGridViewButtonCell cell =
                            dgvRow.Cells["colReturn"] as DataGridViewButtonCell;
                        if (cell != null)
                        {
                            cell.Value = "--";
                            cell.Style.BackColor = Color.FromArgb(200, 200, 200);
                            cell.Style.ForeColor = Color.Gray;
                            cell.ReadOnly = true;
                        }
                    }
                }

                lblPagination.Text = "Page " + _currentPage + " of " + _totalPages;
                btnPrev.Enabled = _currentPage > 1;
                btnNext.Enabled = _currentPage < _totalPages;
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Failed to load transactions: " + ex.Message);
            }
        }

        private void DgvTransactions_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string col = dgvTransactions.Columns[e.ColumnIndex].Name;

            if (col == "colStatus")
            {
                string val = e.Value != null ? e.Value.ToString().ToLower() : "";

                if (val == "borrowed")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(39, 174, 96);
                    e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
                else if (val == "overdue")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(231, 76, 60);
                    e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
                else if (val == "returned")
                {
                    e.CellStyle.ForeColor = Color.FromArgb(52, 152, 219);
                    e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                }
            }

            if (col == "colReturn")
            {
                DataGridViewButtonCell cell =
                    dgvTransactions.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null && cell.Value != null && cell.Value.ToString() != "--")
                {
                    cell.Style.BackColor = Color.FromArgb(39, 174, 96);
                    cell.Style.ForeColor = Color.White;
                }
            }

            if (col == "colView")
            {
                DataGridViewButtonCell cell =
                    dgvTransactions.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null)
                {
                    cell.Style.BackColor = Color.FromArgb(52, 152, 219);
                    cell.Style.ForeColor = Color.White;
                }
            }
        }

        private void DgvTransactions_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string col = dgvTransactions.Columns[e.ColumnIndex].Name;
            int transactionId = Convert.ToInt32(dgvTransactions.Rows[e.RowIndex].Tag);

            if (col == "colReturn")
            {
                DataGridViewButtonCell btnCell =
                    dgvTransactions.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (btnCell != null && btnCell.Value != null && btnCell.Value.ToString() == "--")
                    return;

                DialogResult confirmResult = MessageHelper.ShowConfirm(
                    "Mark this book as returned?\nThis will restore the book's available quantity.");

                if (confirmResult == DialogResult.Yes)
                {
                    try
                    {
                        _repo.ReturnBook(transactionId, null);
                        MessageHelper.ShowSuccess("Book marked as returned successfully.");
                        LoadTransactions();
                    }
                    catch (Exception ex)
                    {
                        MessageHelper.ShowError("Failed to process return: " + ex.Message);
                    }
                }
            }
            else if (col == "colView")
            {
                BorrowViewForm form = new BorrowViewForm(transactionId);
                form.ShowDialog();
            }
        }

        private void BtnBorrowBook_Click(object sender, EventArgs e)
        {
            BorrowBookForm form = new BorrowBookForm();
            form.FormClosed += (s, ev) => LoadTransactions();
            form.ShowDialog();
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
                tb.HandleCreated += (s, e) =>
                {
                    SendMessage(tb.Handle, EM_SETCUEBANNER, (IntPtr)1, cue);
                };
            }
        }
    }
}