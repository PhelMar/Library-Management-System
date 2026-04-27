using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Books
{
    public class BookAddEditForm : Form
    {
        private readonly int _bookId;
        private readonly BookRepository _bookRepo;

        private TextBox txtTitle, txtAuthor;
        private ComboBox cboCategory;
        private Button btnSave, btnCancel;

        public BookAddEditForm(int bookId)
        {
            _bookId = bookId;
            _bookRepo = new BookRepository();
            InitializeForm();
            LoadCategories();
            if (_bookId > 0) LoadBookData();
        }

        private void InitializeForm()
        {
            this.Text = _bookId == 0 ? "Add New Book" : "Edit Book";
            this.Size = new Size(420, 320);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            // Title
            AddLabel("Book Title", 20, 20);
            txtTitle = AddTextBox(20, 45, 360);

            // Author
            AddLabel("Author", 20, 90);
            txtAuthor = AddTextBox(20, 115, 360);

            // Category
            AddLabel("Category", 20, 160);
            cboCategory = new ComboBox
            {
                Location = new Point(20, 185),
                Size = new Size(360, 30),
                Font = new Font("Segoe UI", 10f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(cboCategory);

            // Buttons
            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(200, 235),
                Size = new Size(85, 33),
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(295, 235),
                Size = new Size(85, 33),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10f)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
        }

        private void LoadCategories()
        {
            var categories = _bookRepo.GetCategories();
            cboCategory.DataSource = categories;
            cboCategory.DisplayMember = "category_name";
            cboCategory.ValueMember = "id";
        }

        private void LoadBookData()
        {
            var book = _bookRepo.GetBookById(_bookId);
            if (book == null) return;
            txtTitle.Text = book["book_title"].ToString();
            txtAuthor.Text = book["author"].ToString();
            cboCategory.SelectedValue = Convert.ToInt32(book["category_id"]);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageHelper.ShowWarning("Book title is required.");
                return;
            }

            if (cboCategory.SelectedValue == null)
            {
                MessageHelper.ShowWarning("Please select a category.");
                return;
            }

            try
            {
                int categoryId = Convert.ToInt32(cboCategory.SelectedValue);

                if (_bookId == 0)
                    _bookRepo.AddBook(txtTitle.Text.Trim(), txtAuthor.Text.Trim(), categoryId);
                else
                    _bookRepo.UpdateBook(_bookId, txtTitle.Text.Trim(), txtAuthor.Text.Trim(), categoryId);

                MessageHelper.ShowSuccess(_bookId == 0 ? "Book added successfully!" : "Book updated successfully!");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Failed to save book: " + ex.Message);
            }
        }

        // ── Helpers ───────────────────────────────────────
        private void AddLabel(string text, int x, int y)
        {
            this.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.Gray
            });
        }

        private TextBox AddTextBox(int x, int y, int width)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 30),
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txt);
            return txt;
        }
    }
}