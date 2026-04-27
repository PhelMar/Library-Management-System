using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Books
{
    public class ManageBooksPanel : Panel
    {
        private DataGridView dgvBooks;
        private TextBox txtSearch;
        private Button btnAddBook;
        private Label lblPagination;
        private Button btnPrev, btnNext;
        private Label lblTitle;

        private readonly BookRepository _bookRepo;

        private int _currentPage = 1;
        private int _totalPages = 1;
        private const int PAGE_SIZE = 10;

        private const int EM_SETCUEBANNER = 0x1501;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        public ManageBooksPanel()
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
            // ── Main Layout ───────────────────────────────
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));  // Title
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

            lblTitle = new Label
            {
                Text = "Manage Books",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(0, 10),
                AutoSize = true
            };

            titlePanel.Controls.Add(lblTitle);

            // ── Row 1: Search + Add Button ────────────────
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
            SetCueBanner(txtSearch, "Search by title, author or category...");

            txtSearch.TextChanged += (s, e) => { _currentPage = 1; LoadBooks(); };

            btnAddBook = new Button
            {
                Text = "+ Add Book",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Size = new Size(130, 34),
                Location = new Point(330, 8)
            };
            btnAddBook.FlatAppearance.BorderSize = 0;
            btnAddBook.Click += BtnAddBook_Click;

            topBar.Controls.Add(txtSearch);
            topBar.Controls.Add(btnAddBook);

            // ── Row 2: DataGridView ───────────────────────
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

            // Header style
            dgvBooks.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(5, 0, 0, 0)
            };

            // Row style
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

            // Columns
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNo",
                HeaderText = "No.",
                FillWeight = 5,
                MinimumWidth = 50
            });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTitle",
                HeaderText = "Title",
                FillWeight = 30
            });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAuthor",
                HeaderText = "Author",
                FillWeight = 20
            });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCategory",
                HeaderText = "Category",
                FillWeight = 15
            });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colQty",
                HeaderText = "Qty",
                FillWeight = 8
            });

            // Action buttons column - Quick Add Qty
            dgvBooks.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colAddQty",
                HeaderText = "+Qty",
                Text = "+ Qty",
                UseColumnTextForButtonValue = true,
                FillWeight = 8
            });

            // View button
            dgvBooks.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colView",
                HeaderText = "",
                Text = "View",
                UseColumnTextForButtonValue = true,
                FillWeight = 8
            });

            // Edit button
            dgvBooks.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colEdit",
                HeaderText = "",
                Text = "Edit",
                UseColumnTextForButtonValue = true,
                FillWeight = 8
            });

            // Delete button
            dgvBooks.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colDelete",
                HeaderText = "",
                Text = "Delete",
                UseColumnTextForButtonValue = true,
                FillWeight = 8
            });

            dgvBooks.CellContentClick += DgvBooks_CellContentClick;
            dgvBooks.CellFormatting += DgvBooks_CellFormatting;

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

            // ── Assemble ──────────────────────────────────
            mainLayout.Controls.Add(titlePanel, 0, 0);
            mainLayout.Controls.Add(topBar, 0, 1);
            mainLayout.Controls.Add(dgvBooks, 0, 2);
            mainLayout.Controls.Add(paginationPanel, 0, 3);

            this.Controls.Add(mainLayout);
        }

        // Set cue/banner text for a TextBox. Works on frameworks where TextBox.PlaceholderText is not present.
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

        // ── Load Books from DB ────────────────────────────
        public void LoadBooks()
        {
            try
            {
                string search = txtSearch?.Text.Trim() ?? "";

                var result = _bookRepo.GetBooksPaged(search, _currentPage, PAGE_SIZE, out int totalCount);

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
                        row["current_qty"],
                        "+ Qty", "View", "Edit", "Delete"
                    );

                    // Store book id in row tag
                    dgvBooks.Rows[dgvBooks.Rows.Count - 1].Tag = row["id"];
                }

                lblPagination.Text = $"Page {_currentPage} of {_totalPages}";
                btnPrev.Enabled = _currentPage > 1;
                btnNext.Enabled = _currentPage < _totalPages;
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Failed to load books: " + ex.Message);
            }
        }

        // ── Cell Formatting — Color action buttons ────────
        private void DgvBooks_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dgvBooks.Columns[e.ColumnIndex].Name == "colAddQty")
            {
                var cell = dgvBooks.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null) cell.Style.BackColor = Color.FromArgb(39, 174, 96);
                if (cell != null) cell.Style.ForeColor = Color.White;
            }
            else if (dgvBooks.Columns[e.ColumnIndex].Name == "colView")
            {
                var cell = dgvBooks.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null) cell.Style.BackColor = Color.FromArgb(52, 152, 219);
                if (cell != null) cell.Style.ForeColor = Color.White;
            }
            else if (dgvBooks.Columns[e.ColumnIndex].Name == "colEdit")
            {
                var cell = dgvBooks.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null) cell.Style.BackColor = Color.FromArgb(230, 126, 34);
                if (cell != null) cell.Style.ForeColor = Color.White;
            }
            else if (dgvBooks.Columns[e.ColumnIndex].Name == "colDelete")
            {
                var cell = dgvBooks.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null) cell.Style.BackColor = Color.FromArgb(231, 76, 60);
                if (cell != null) cell.Style.ForeColor = Color.White;
            }
        }

        // ── Button Clicks ─────────────────────────────────
        private void DgvBooks_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            int bookId = Convert.ToInt32(dgvBooks.Rows[e.RowIndex].Tag);
            string colName = dgvBooks.Columns[e.ColumnIndex].Name;

            if (colName == "colAddQty")
            {
                var form = new BookViewForm(bookId, openOnAddQty: true);
                form.FormClosed += (s, ev) => LoadBooks();
                form.ShowDialog();
            }
            else if (colName == "colView")
            {
                var form = new BookViewForm(bookId, openOnAddQty: false);
                form.FormClosed += (s, ev) => LoadBooks();
                form.ShowDialog();
            }
            else if (colName == "colEdit")
            {
                var form = new BookAddEditForm(bookId);
                form.FormClosed += (s, ev) => LoadBooks();
                form.ShowDialog();
            }
            else if (colName == "colDelete")
            {
                var confirm = MessageHelper.ShowConfirm(
                    "Are you sure you want to delete this book?\nThis cannot be undone.");

                if (confirm == DialogResult.Yes)
                {
                    try
                    {
                        _bookRepo.DeleteBook(bookId);
                        MessageHelper.ShowSuccess("Book deleted successfully.");
                        LoadBooks();
                    }
                    catch (Exception ex)
                    {
                        MessageHelper.ShowError("Failed to delete: " + ex.Message);
                    }
                }
            }
        }

        private void BtnAddBook_Click(object sender, EventArgs e)
        {
            var form = new BookAddEditForm(0); // 0 = new book
            form.FormClosed += (s, ev) => LoadBooks();
            form.ShowDialog();
        }
    }
}