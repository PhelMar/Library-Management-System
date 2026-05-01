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

        private TextBox txtTitle, txtAuthor, txtIsbn, txtEdition, txtPublishedYear;
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
            this.Size = new Size(440, 530);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            int x = 20;
            int fieldWidth = 380;

            AddLabel("Book Title *", x, 20);
            txtTitle = AddTextBox(x, 42, fieldWidth);

            AddLabel("Author", x, 87);
            txtAuthor = AddTextBox(x, 109, fieldWidth);

            AddLabel("Category *", x, 154);
            cboCategory = new ComboBox
            {
                Location = new Point(x, 176),
                Size = new Size(fieldWidth, 30),
                Font = new Font("Segoe UI", 10f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(cboCategory);

            AddLabel("ISBN", x, 221);
            txtIsbn = AddTextBox(x, 243, fieldWidth);

            AddLabel("Edition", x, 288);
            txtEdition = AddTextBox(x, 310, fieldWidth);

            AddLabel("Published Year", x, 355);
            txtPublishedYear = AddTextBox(x, 377, 120);

            btnSave = new Button
            {
                Text = "Save",
                Location = new Point(215, 435),
                Size = new Size(90, 34),
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
                Location = new Point(313, 435),
                Size = new Size(90, 34),
                BackColor = Color.FromArgb(180, 180, 180),
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
            txtIsbn.Text = book["isbn"] == DBNull.Value ? "" : book["isbn"].ToString();
            txtEdition.Text = book["edition"] == DBNull.Value ? "" : book["edition"].ToString();
            txtPublishedYear.Text = book["published_year"] == DBNull.Value ? "" : book["published_year"].ToString();
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTitle.Text))
            {
                MessageHelper.ShowWarning("Book title is required.");
                txtTitle.Focus();
                return;
            }

            if (cboCategory.SelectedValue == null)
            {
                MessageHelper.ShowWarning("Please select a category.");
                cboCategory.Focus();
                return;
            }

            int? publishedYear = null;
            if (!string.IsNullOrWhiteSpace(txtPublishedYear.Text))
            {
                if (!int.TryParse(txtPublishedYear.Text.Trim(), out int parsedYear)
                    || parsedYear < 1000 || parsedYear > DateTime.Now.Year)
                {
                    MessageHelper.ShowWarning($"Published year must be a valid year between 1000 and {DateTime.Now.Year}.");
                    txtPublishedYear.Focus();
                    return;
                }
                publishedYear = parsedYear;
            }

            try
            {
                int categoryId = Convert.ToInt32(cboCategory.SelectedValue);
                string title = txtTitle.Text.Trim();
                string author = txtAuthor.Text.Trim();
                string isbn = string.IsNullOrWhiteSpace(txtIsbn.Text) ? null : txtIsbn.Text.Trim();
                string edition = string.IsNullOrWhiteSpace(txtEdition.Text) ? null : txtEdition.Text.Trim();

                if (_bookId == 0)
                    _bookRepo.AddBook(title, author, categoryId, isbn, edition, publishedYear);
                else
                    _bookRepo.UpdateBook(_bookId, title, author, categoryId, isbn, edition, publishedYear);

                MessageHelper.ShowSuccess(_bookId == 0 ? "Book added successfully." : "Book updated successfully.");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Unable to save the book. Please try again.\n\nDetails: " + ex.Message);
            }
        }

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