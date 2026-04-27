using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Borrow
{
    public class BorrowBookForm : Form
    {
        private readonly TransactionRepository _repo;

        private int _selectedEnrollmentId = 0;
        private int _selectedBookId = 0;
        private int _selectedLibrarianId = 0;

        private TextBox txtStudentSearch;
        private DataGridView dgvStudents;
        private Panel pnlStudentInfo;
        private Label lblSelectedStudent;

        private TextBox txtBookSearch;
        private DataGridView dgvBooks;
        private Label lblSelectedBook;

        private ComboBox cmbLibrarian;
        private DateTimePicker dtpDueDate;
        private TextBox txtRemarks;
        private Label lblSummaryBody;

        private Button btnSave;

        private const int EM_SETCUEBANNER = 0x1501;
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        public BorrowBookForm()
        {
            _repo = new TransactionRepository();
            InitializeForm();
            LoadLibrarians();
        }

        private void InitializeForm()
        {
            this.Text = "Borrow a Book";
            this.Size = new Size(980, 700);
            this.MinimumSize = new Size(980, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 255);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            Panel outer = new Panel { Dock = DockStyle.Fill, Padding = new Padding(25, 20, 25, 15) };
            this.Controls.Add(outer);

            Label lblFormTitle = new Label
            {
                Text = "Process Book Borrow",
                Font = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(25, 20),
                AutoSize = true
            };
            outer.Controls.Add(lblFormTitle);

            Panel divider = new Panel
            {
                Location = new Point(25, 52),
                Size = new Size(910, 1),
                BackColor = Color.FromArgb(200, 195, 230)
            };
            outer.Controls.Add(divider);

            // Two-column layout
            TableLayoutPanel columns = new TableLayoutPanel
            {
                Location = new Point(25, 62),
                Size = new Size(910, 580),
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            outer.Controls.Add(columns);

            Panel leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            columns.Controls.Add(leftPanel, 0, 0);
            BuildLeftColumn(leftPanel);

            Panel rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(15, 0, 0, 0)
            };
            columns.Controls.Add(rightPanel, 1, 0);
            BuildRightColumn(rightPanel);
        }

        private void BuildLeftColumn(Panel parent)
        {
            int y = 0;

            // Step 1 header
            parent.Controls.Add(MakeSectionLabel("Step 1 - Select Student", y));
            y += 30;

            txtStudentSearch = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(0, y),
                Size = new Size(540, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            SetCueBanner(txtStudentSearch, "Type student name or ID number...");
            txtStudentSearch.TextChanged += TxtStudentSearch_TextChanged;
            parent.Controls.Add(txtStudentSearch);
            y += 36;

            dgvStudents = BuildMiniGrid(new Point(0, y), new Size(540, 155));
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSNo", HeaderText = "Student No.", FillWeight = 22 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSName", HeaderText = "Name", FillWeight = 38 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCourse", HeaderText = "Course", FillWeight = 25 });
            dgvStudents.Columns.Add(new DataGridViewTextBoxColumn { Name = "colYear", HeaderText = "Year", FillWeight = 15 });
            dgvStudents.CellClick += DgvStudents_CellClick;
            parent.Controls.Add(dgvStudents);
            y += 163;

            // Selected student banner
            pnlStudentInfo = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(540, 34),
                BackColor = Color.FromArgb(235, 250, 240),
                Visible = false
            };
            lblSelectedStudent = new Label
            {
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(39, 120, 60),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0)
            };
            pnlStudentInfo.Controls.Add(lblSelectedStudent);
            parent.Controls.Add(pnlStudentInfo);
            y += 42;

            // Step 2 header
            parent.Controls.Add(MakeSectionLabel("Step 2 - Select Book", y));
            y += 30;

            txtBookSearch = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(0, y),
                Size = new Size(540, 30),
                BorderStyle = BorderStyle.FixedSingle
            };
            SetCueBanner(txtBookSearch, "Type book title or author...");
            txtBookSearch.TextChanged += TxtBookSearch_TextChanged;
            parent.Controls.Add(txtBookSearch);
            y += 36;

            dgvBooks = BuildMiniGrid(new Point(0, y), new Size(540, 155));
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBTitle", HeaderText = "Title", FillWeight = 40 });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBAuthor", HeaderText = "Author", FillWeight = 28 });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBCategory", HeaderText = "Category", FillWeight = 22 });
            dgvBooks.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBQty", HeaderText = "Avail.", FillWeight = 10 });
            dgvBooks.CellClick += DgvBooks_CellClick;
            parent.Controls.Add(dgvBooks);
            y += 163;

            // Selected book banner
            Panel pnlBookInfo = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(540, 34),
                BackColor = Color.FromArgb(235, 245, 255)
            };
            lblSelectedBook = new Label
            {
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(30, 90, 160),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Text = "No book selected yet."
            };
            pnlBookInfo.Controls.Add(lblSelectedBook);
            parent.Controls.Add(pnlBookInfo);
        }

        private void BuildRightColumn(Panel parent)
        {
            int y = 0;

            parent.Controls.Add(MakeSectionLabel("Step 3 - Transaction Details", y));
            y += 36;

            // Librarian
            parent.Controls.Add(MakeFieldLabel("Librarian on Duty", y));
            y += 22;

            cmbLibrarian = new ComboBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(0, y),
                Size = new Size(320, 30),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            parent.Controls.Add(cmbLibrarian);
            y += 42;

            // Due Date
            parent.Controls.Add(MakeFieldLabel("Return Due Date  (max 14 days)", y));
            y += 22;

            dtpDueDate = new DateTimePicker
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(0, y),
                Size = new Size(320, 30),
                Format = DateTimePickerFormat.Long,
                MinDate = DateTime.Today.AddDays(1),
                MaxDate = DateTime.Today.AddDays(14),
                Value = DateTime.Today.AddDays(7)
            };
            dtpDueDate.ValueChanged += (s, e) => RefreshSummary();
            parent.Controls.Add(dtpDueDate);
            y += 42;

            Label lblDueHint = new Label
            {
                Text = "Max return date: " + DateTime.Today.AddDays(14).ToString("MMMM dd, yyyy"),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),
                ForeColor = Color.FromArgb(140, 130, 180),
                Location = new Point(0, y),
                Size = new Size(320, 20)
            };
            parent.Controls.Add(lblDueHint);
            y += 32;

            // Remarks
            parent.Controls.Add(MakeFieldLabel("Remarks (optional)", y));
            y += 22;

            txtRemarks = new TextBox
            {
                Font = new Font("Segoe UI", 10f),
                Location = new Point(0, y),
                Size = new Size(320, 80),
                Multiline = true,
                BorderStyle = BorderStyle.FixedSingle,
                ScrollBars = ScrollBars.Vertical
            };
            SetCueBanner(txtRemarks, "Any notes about this transaction...");
            parent.Controls.Add(txtRemarks);
            y += 96;

            // Summary card
            Panel summaryCard = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(320, 110),
                BackColor = Color.FromArgb(240, 238, 255)
            };
            Label lblSummaryTitle = new Label
            {
                Text = "Transaction Summary",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 50, 120),
                Location = new Point(10, 10),
                AutoSize = true
            };
            lblSummaryBody = new Label
            {
                Text = "Select a student and a book to see the summary.",
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(80, 70, 140),
                Location = new Point(10, 32),
                Size = new Size(300, 70),
                AutoSize = false
            };
            summaryCard.Controls.Add(lblSummaryTitle);
            summaryCard.Controls.Add(lblSummaryBody);
            parent.Controls.Add(summaryCard);
            y += 120;

            // Save button
            btnSave = new Button
            {
                Text = "Confirm Borrow",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Location = new Point(0, y),
                Size = new Size(320, 40)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;
            parent.Controls.Add(btnSave);
            y += 48;

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(200, 195, 220),
                ForeColor = Color.FromArgb(40, 30, 80),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Location = new Point(0, y),
                Size = new Size(320, 36)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();
            parent.Controls.Add(btnCancel);
        }

        private void LoadLibrarians()
        {
            try
            {
                DataTable dt = _repo.GetLibrarians();
                cmbLibrarian.Items.Clear();
                cmbLibrarian.Items.Add(new ComboItem { Id = 0, Display = "-- Select librarian --" });

                foreach (DataRow row in dt.Rows)
                {
                    cmbLibrarian.Items.Add(new ComboItem
                    {
                        Id = Convert.ToInt32(row["id"]),
                        Display = row["full_name"] + "  (" + row["shift"] + " shift)"
                    });
                }

                cmbLibrarian.DisplayMember = "Display";
                cmbLibrarian.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Could not load librarians: " + ex.Message);
            }
        }

        private void TxtStudentSearch_TextChanged(object sender, EventArgs e)
        {
            string kw = txtStudentSearch.Text.Trim();
            if (kw.Length < 2)
            {
                dgvStudents.Rows.Clear();
                return;
            }

            try
            {
                DataTable dt = _repo.SearchStudents(kw);
                dgvStudents.Rows.Clear();

                foreach (DataRow row in dt.Rows)
                {
                    int idx = dgvStudents.Rows.Add(
                        row["student_no"],
                        row["full_name"],
                        row["course_name"],
                        row["year_level"]
                    );
                    dgvStudents.Rows[idx].Tag = new StudentTag
                    {
                        EnrollmentId = Convert.ToInt32(row["enrollment_id"]),
                        FullName = row["full_name"].ToString(),
                        StudentNo = row["student_no"].ToString(),
                        CourseName = row["course_name"].ToString(),
                        YearLevel = row["year_level"].ToString(),
                        SchoolYear = row["school_year"].ToString(),
                        Semester = row["semester_name"].ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Student search failed: " + ex.Message);
            }
        }

        private void TxtBookSearch_TextChanged(object sender, EventArgs e)
        {
            string kw = txtBookSearch.Text.Trim();
            if (kw.Length < 2)
            {
                dgvBooks.Rows.Clear();
                return;
            }

            try
            {
                DataTable dt = _repo.SearchAvailableBooks(kw);
                dgvBooks.Rows.Clear();

                foreach (DataRow row in dt.Rows)
                {
                    int idx = dgvBooks.Rows.Add(
                        row["book_title"],
                        row["author"],
                        row["category_name"],
                        row["current_qty"]
                    );
                    dgvBooks.Rows[idx].Tag = new BookTag
                    {
                        BookId = Convert.ToInt32(row["id"]),
                        BookTitle = row["book_title"].ToString(),
                        Author = row["author"].ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Book search failed: " + ex.Message);
            }
        }

        private void DgvStudents_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            StudentTag tag = dgvStudents.Rows[e.RowIndex].Tag as StudentTag;
            if (tag == null) return;

            _selectedEnrollmentId = tag.EnrollmentId;
            lblSelectedStudent.Text = tag.FullName + "  |  " + tag.StudentNo +
                                      "  |  " + tag.CourseName + " " + tag.YearLevel +
                                      "  |  " + tag.SchoolYear + " - " + tag.Semester;
            pnlStudentInfo.Visible = true;

            RefreshSummary();
        }

        private void DgvBooks_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            BookTag tag = dgvBooks.Rows[e.RowIndex].Tag as BookTag;
            if (tag == null) return;

            _selectedBookId = tag.BookId;
            lblSelectedBook.Text = tag.BookTitle + "  by  " + tag.Author;

            RefreshSummary();
        }

        private void RefreshSummary()
        {
            if (lblSummaryBody == null) return;

            if (_selectedEnrollmentId == 0 || _selectedBookId == 0)
            {
                lblSummaryBody.Text = "Select a student and a book to see the summary.";
                return;
            }

            lblSummaryBody.Text =
                "Enrollment  : #" + _selectedEnrollmentId + "\r\n" +
                "Book ID     : #" + _selectedBookId + "\r\n" +
                "Borrow Date : " + DateTime.Now.ToString("MMM dd, yyyy") + "\r\n" +
                "Due Date    : " + dtpDueDate.Value.ToString("MMM dd, yyyy");
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_selectedEnrollmentId == 0)
            {
                MessageHelper.ShowWarning("Please select a student first.");
                return;
            }
            if (_selectedBookId == 0)
            {
                MessageHelper.ShowWarning("Please select a book first.");
                return;
            }

            ComboItem libItem = cmbLibrarian.SelectedItem as ComboItem;
            if (libItem == null || libItem.Id == 0)
            {
                MessageHelper.ShowWarning("Please select the librarian on duty.");
                return;
            }

            _selectedLibrarianId = libItem.Id;

            DateTime dueDate = dtpDueDate.Value.Date;
            if (dueDate < DateTime.Today.AddDays(1))
            {
                MessageHelper.ShowWarning("Due date cannot be today or in the past.");
                return;
            }
            if (dueDate > DateTime.Today.AddDays(14))
            {
                MessageHelper.ShowWarning("Due date cannot exceed 14 days from today.");
                return;
            }

            DialogResult confirm = MessageHelper.ShowConfirm(
                "Confirm borrow transaction?\r\n\r\n" +
                "Enrollment : #" + _selectedEnrollmentId + "\r\n" +
                "Book       : #" + _selectedBookId + "\r\n" +
                "Due Date   : " + dueDate.ToString("MMMM dd, yyyy") + "\r\n" +
                "Librarian  : " + libItem.Display);

            if (confirm != DialogResult.Yes) return;

            try
            {
                btnSave.Enabled = false;
                btnSave.Text = "Saving...";

                _repo.BorrowBook(
                    _selectedEnrollmentId,
                    _selectedBookId,
                    _selectedLibrarianId,
                    dueDate,
                    txtRemarks.Text.Trim()
                );

                MessageHelper.ShowSuccess("Book borrowed successfully!");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Failed to process borrow: " + ex.Message);
                btnSave.Enabled = true;
                btnSave.Text = "Confirm Borrow";
            }
        }

        private DataGridView BuildMiniGrid(Point location, Size size)
        {
            DataGridView dgv = new DataGridView
            {
                Location = location,
                Size = size,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9.5f),
                GridColor = Color.FromArgb(230, 230, 240),
                ColumnHeadersHeight = 32,
                RowTemplate = { Height = 30 },
                MultiSelect = false,
                Cursor = Cursors.Hand
            };

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 0, 0)
            };

            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 30, 80),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80),
                Padding = new Padding(4, 0, 0, 0)
            };

            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 247, 255),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80)
            };

            return dgv;
        }

        private Label MakeSectionLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(95, 75, 180),
                Location = new Point(0, y),
                AutoSize = true
            };
        }

        private Label MakeFieldLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(70, 60, 120),
                Location = new Point(0, y),
                AutoSize = true
            };
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

        private class ComboItem
        {
            public int Id { get; set; }
            public string Display { get; set; }

            public override string ToString()
            {
                return Display;
            }
        }

        private class StudentTag
        {
            public int EnrollmentId { get; set; }
            public string FullName { get; set; }
            public string StudentNo { get; set; }
            public string CourseName { get; set; }
            public string YearLevel { get; set; }
            public string SchoolYear { get; set; }
            public string Semester { get; set; }
        }

        private class BookTag
        {
            public int BookId { get; set; }
            public string BookTitle { get; set; }
            public string Author { get; set; }
        }
    }
}