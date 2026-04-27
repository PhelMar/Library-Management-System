using LibrarySystem.Core;
using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Books
{
    public class BookViewForm : Form
    {
        private readonly int _bookId;
        private readonly bool _openOnAddQty;
        private readonly BookRepository _bookRepo;
        private DataGridView dgvLog;
        private Label lblQty;

        public BookViewForm(int bookId, bool openOnAddQty)
        {
            _bookId = bookId;
            _openOnAddQty = openOnAddQty;
            _bookRepo = new BookRepository();
            InitializeForm();
            LoadData();

            // Auto open add qty dialog if came from +Qty button
            if (_openOnAddQty)
                this.Shown += (s, e) => ShowInventoryAction("add");
        }

        private void InitializeForm()
        {
            this.Text = "Book Details";
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
        }

        private void LoadData()
        {
            this.Controls.Clear();

            var book = _bookRepo.GetBookById(_bookId);
            if (book == null) return;

            // ── Book Info ─────────────────────────────────
            AddLabel("Title:", 20, 20, bold: true);
            AddLabel(book["book_title"].ToString(), 80, 20);

            AddLabel("Author:", 20, 45, bold: true);
            AddLabel(book["author"].ToString(), 80, 45);

            AddLabel("Category:", 20, 70, bold: true);
            AddLabel(book["category_name"].ToString(), 100, 70);

            AddLabel("Current Qty:", 20, 95, bold: true);
            lblQty = new Label
            {
                Text = book["current_qty"].ToString(),
                Location = new Point(115, 95),
                AutoSize = true,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Color.FromArgb(95, 75, 180)
            };
            this.Controls.Add(lblQty);

            // ── Action Buttons ────────────────────────────
            var btnAddQty = CreateActionButton("+ Add Qty", Color.FromArgb(39, 174, 96), 20, 135);
            var btnLost = CreateActionButton("Record Lost", Color.FromArgb(231, 76, 60), 160, 135);
            var btnDamage = CreateActionButton("Record Damaged", Color.FromArgb(230, 126, 34), 300, 135);
            var btnCorrect = CreateActionButton("Correction", Color.FromArgb(52, 152, 219), 460, 135);

            btnAddQty.Click += (s, e) => ShowInventoryAction("add");
            btnLost.Click += (s, e) => ShowInventoryAction("lost");
            btnDamage.Click += (s, e) => ShowInventoryAction("damaged");
            btnCorrect.Click += (s, e) => ShowInventoryAction("correction");

            this.Controls.Add(btnAddQty);
            this.Controls.Add(btnLost);
            this.Controls.Add(btnDamage);
            this.Controls.Add(btnCorrect);

            // ── Divider ───────────────────────────────────
            var divider = new Panel
            {
                Location = new Point(20, 185),
                Size = new Size(640, 1),
                BackColor = Color.FromArgb(220, 220, 230)
            };
            this.Controls.Add(divider);

            // ── Log Label ─────────────────────────────────
            AddLabel("Inventory Log", 20, 195, bold: true, size: 12f);

            // ── Log Table ─────────────────────────────────
            dgvLog = new DataGridView
            {
                Location = new Point(20, 220),
                Size = new Size(645, 270),
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Font = new Font("Segoe UI", 9f),
                ColumnHeadersHeight = 35,
                RowTemplate = { Height = 32 }
            };

            dgvLog.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };

            dgvLog.Columns.Add("colDate", "Date");
            dgvLog.Columns.Add("colAction", "Action");
            dgvLog.Columns.Add("colQty", "Qty");
            dgvLog.Columns.Add("colRemarks", "Remarks");
            dgvLog.Columns.Add("colRecordedBy", "Recorded By");

            this.Controls.Add(dgvLog);

            LoadLog();
        }

        private void LoadLog()
        {
            dgvLog.Rows.Clear();
            var logs = _bookRepo.GetInventoryLog(_bookId);

            foreach (DataRow row in logs.Rows)
            {
                int index = dgvLog.Rows.Add(
                    Convert.ToDateTime(row["recorded_at"]).ToString("MMM dd, yyyy hh:mm tt"),
                    row["action"].ToString().ToUpper(),
                    row["qty"],
                    row["remarks"],
                    row["recorded_by"]
                );

                // Color code action
                var actionCell = dgvLog.Rows[index].Cells["colAction"];
                switch (row["action"].ToString())
                {
                    case "add":
                        actionCell.Style.ForeColor = Color.FromArgb(39, 174, 96);
                        break;
                    case "lost":
                    case "damaged":
                        actionCell.Style.ForeColor = Color.FromArgb(231, 76, 60);
                        break;
                    case "correction":
                        actionCell.Style.ForeColor = Color.FromArgb(52, 152, 219);
                        break;
                }
            }
        }

        private void ShowInventoryAction(string action)
        {
            string title = action == "add" ? "Add Quantity" :
                           action == "lost" ? "Record Lost" :
                           action == "damaged" ? "Record Damaged" : "Correction";

            // Small inline dialog
            var dialog = new Form
            {
                Text = title,
                Size = new Size(350, 230),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                BackColor = Color.White
            };

            dialog.Controls.Add(new Label
            {
                Text = action == "correction"
                           ? "Qty (use negative to deduct e.g. -3):"
                           : "Quantity:",
                Location = new Point(20, 20),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f)
            });

            var txtQty = new TextBox
            {
                Location = new Point(20, 45),
                Size = new Size(290, 30),
                Font = new Font("Segoe UI", 11f)
            };

            dialog.Controls.Add(new Label
            {
                Text = "Remarks (optional):",
                Location = new Point(20, 85),
                AutoSize = true,
                Font = new Font("Segoe UI", 10f)
            });

            var txtRemarks = new TextBox
            {
                Location = new Point(20, 108),
                Size = new Size(290, 30),
                Font = new Font("Segoe UI", 10f)
            };

            var btnConfirm = new Button
            {
                Text = "Confirm",
                Location = new Point(130, 150),
                Size = new Size(85, 33),
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnConfirm.FlatAppearance.BorderSize = 0;

            btnConfirm.Click += (s, e) =>
            {
                if (!int.TryParse(txtQty.Text.Trim(), out int qty) || qty == 0)
                {
                    MessageHelper.ShowWarning("Please enter a valid quantity.");
                    return;
                }

                try
                {
                    _bookRepo.RecordInventory(
                        _bookId, action, qty,
                        txtRemarks.Text.Trim(),
                        Session.CurrentUser.LibrarianId
                    );

                    MessageHelper.ShowSuccess("Recorded successfully!");
                    dialog.Close();
                    LoadData(); // Refresh view
                }
                catch (Exception ex)
                {
                    MessageHelper.ShowError("Failed to record: " + ex.Message);
                }
            };

            var btnClose = new Button
            {
                Text = "Cancel",
                Location = new Point(225, 150),
                Size = new Size(85, 33),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => dialog.Close();

            dialog.Controls.Add(txtQty);
            dialog.Controls.Add(txtRemarks);
            dialog.Controls.Add(btnConfirm);
            dialog.Controls.Add(btnClose);

            dialog.ShowDialog(this);
        }

        private Button CreateActionButton(string text, Color color, int x, int y)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(130, 35),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void AddLabel(string text, int x, int y, bool bold = false, float size = 10f)
        {
            this.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = Color.FromArgb(40, 30, 80)
            });
        }
    }
}