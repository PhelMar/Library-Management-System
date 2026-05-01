using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Books
{
    public class ArchivedBooksPanel : Panel
    {
        private DataGridView dgvBooks;
        private TextBox txtSearch;
        private Label lblPagination;
        private Button btnPrev, btnNext;

        private readonly BookRepository _bookRepo;

        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PAGE_SIZE = 10;

        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        public ArchivedBooksPanel()
        {
            _bookRepo = new BookRepository();
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(245, 245, 255);
            this.Padding = new Padding(25, 20, 25, 20);
            BuildUI();
            LoadBooks();
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

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 55f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblTitle = new Label
            {
                Text = "Archived Books",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(0, 10),
                AutoSize = true
            };
            titlePanel.Controls.Add(lblTitle);

            var topBar = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            txtSearch = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(0, 11),
                Size = new Size(280, 32),
                BorderStyle = BorderStyle.FixedSingle
            };
            SetCueBanner(txtSearch, "Search title, author or category...");
            txtSearch.TextChanged += (s, e) => { _currentPage = 1; LoadBooks(); };

            topBar.Controls.Add(txtSearch);

            dgvBooks = new DataGridView
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

            dgvBooks.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };

            dgvBooks.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 30, 80),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80),
                Padding = new Padding(5, 0, 0, 0)
            };

            dgvBooks.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 247, 255),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80)
            };

            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNo", HeaderText = "No.", FillWeight = 5, MinimumWidth = 50 });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTitle", HeaderText = "Title", FillWeight = 28 });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAuthor", HeaderText = "Author", FillWeight = 18 });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCategory", HeaderText = "Category", FillWeight = 13 });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { Name = "colIsbn", HeaderText = "ISBN", FillWeight = 12 });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEdition", HeaderText = "Edition", FillWeight = 10 });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { Name = "colYear", HeaderText = "Year", FillWeight = 8 });

            dgvBooks.Columns.Add(new DataGridViewButtonColumn { Name = "colRestore", HeaderText = "", Text = "Restore", UseColumnTextForButtonValue = true, FillWeight = 10 });

            dgvBooks.CellContentClick += DgvBooks_CellContentClick;
            dgvBooks.CellFormatting += DgvBooks_CellFormatting;

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
            btnPrev.Click += (s, e) => { if (_currentPage > 1) { _currentPage--; LoadBooks(); } };

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
            btnNext.Click += (s, e) => { if (_currentPage < _totalPages) { _currentPage++; LoadBooks(); } };

            paginationPanel.Controls.Add(btnPrev);
            paginationPanel.Controls.Add(lblPagination);
            paginationPanel.Controls.Add(btnNext);

            mainLayout.Controls.Add(titlePanel, 0, 0);
            mainLayout.Controls.Add(topBar, 0, 1);
            mainLayout.Controls.Add(dgvBooks, 0, 2);
            mainLayout.Controls.Add(paginationPanel, 0, 3);

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

        public void LoadBooks()
        {
            try
            {
                string search = txtSearch?.Text.Trim() ?? "";
                var result = _bookRepo.GetArchivedBooksPaged(search, _currentPage, PAGE_SIZE, out int totalCount);

                _totalPages = (int)Math.Ceiling((double)totalCount / PAGE_SIZE);
                if (_totalPages < 1) _totalPages = 1;

                dgvBooks.Rows.Clear();

                int rowNo = (_currentPage - 1) * PAGE_SIZE + 1;
                foreach (DataRow row in result.Rows)
                {
                    dgvBooks.Rows.Add(
                        rowNo++,
                        row["book_title"],
                        row["author"],
                        row["category_name"],
                        row["isbn"] == DBNull.Value ? "" : row["isbn"],
                        row["edition"] == DBNull.Value ? "" : row["edition"],
                        row["published_year"] == DBNull.Value ? "" : row["published_year"],
                        "Restore"
                    );
                    dgvBooks.Rows[dgvBooks.Rows.Count - 1].Tag = row["id"];
                }

                lblPagination.Text = $"Page {_currentPage} of {_totalPages}";
                btnPrev.Enabled = _currentPage > 1;
                btnNext.Enabled = _currentPage < _totalPages;
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Unable to load archived books. Please try again.\n\nDetails: " + ex.Message);
            }
        }

        private void DgvBooks_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dgvBooks.Columns[e.ColumnIndex].Name == "colRestore")
            {
                var cell = dgvBooks.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null)
                {
                    cell.Style.BackColor = Color.FromArgb(39, 174, 96);
                    cell.Style.ForeColor = Color.White;
                }
            }
        }

        private void DgvBooks_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dgvBooks.Columns[e.ColumnIndex].Name == "colRestore")
            {
                int bookId = Convert.ToInt32(dgvBooks.Rows[e.RowIndex].Tag);
                string title = dgvBooks.Rows[e.RowIndex].Cells["colTitle"].Value?.ToString();

                var confirm = MessageHelper.ShowConfirm(
                    $"Restore \"{title}\"?\n\nThis book will be moved back to the active books list.");

                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        _bookRepo.RestoreBook(bookId);
                        MessageHelper.ShowSuccess($"\"{title}\" has been restored successfully.");
                        LoadBooks();
                    }
                    catch (Exception ex)
                    {
                        MessageHelper.ShowError("Unable to restore the book. Please try again.\n\nDetails: " + ex.Message);
                    }
                }
            }
        }
    }
}