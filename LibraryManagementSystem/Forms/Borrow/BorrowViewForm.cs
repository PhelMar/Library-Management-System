using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Borrow
{
    public class BorrowViewForm : Form
    {
        private readonly int _transactionId;
        private readonly TransactionRepository _repo;
        private Panel _detailPanel;

        public BorrowViewForm(int transactionId)
        {
            _transactionId = transactionId;
            _repo = new TransactionRepository();

            this.Text = "Transaction Details";
            this.Size = new Size(520, 470);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 245, 255);

            BuildUI();
            LoadDetail();
        }

        private void BuildUI()
        {
            Label lblTitle = new Label
            {
                Text = "Borrow Transaction Detail",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(25, 20),
                AutoSize = true
            };
            this.Controls.Add(lblTitle);

            Panel divider = new Panel
            {
                Location = new Point(25, 52),
                Size = new Size(455, 1),
                BackColor = Color.FromArgb(200, 195, 230)
            };
            this.Controls.Add(divider);

            _detailPanel = new Panel
            {
                Location = new Point(25, 65),
                Size = new Size(455, 330),
                BackColor = Color.White
            };
            this.Controls.Add(_detailPanel);

            Button btnClose = new Button
            {
                Text = "Close",
                Font = new Font("Segoe UI", 10f),
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Location = new Point(350, 405),
                Size = new Size(130, 36)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => this.Close();
            this.Controls.Add(btnClose);
        }

        private void LoadDetail()
        {
            try
            {
                DataTable dt = _repo.GetTransactionById(_transactionId);
                if (dt == null || dt.Rows.Count == 0)
                {
                    MessageBox.Show("Transaction not found.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                DataRow row = dt.Rows[0];

                string statusVal = row["status"].ToString();
                Color statusColor;
                if (statusVal == "borrowed")
                    statusColor = Color.FromArgb(39, 174, 96);
                else if (statusVal == "overdue")
                    statusColor = Color.FromArgb(231, 76, 60);
                else if (statusVal == "returned")
                    statusColor = Color.FromArgb(52, 152, 219);
                else
                    statusColor = Color.Gray;

                string returnedDate = (row["returned_date"] == DBNull.Value)
                    ? "Not yet returned"
                    : Convert.ToDateTime(row["returned_date"]).ToString("MMM dd, yyyy  hh:mm tt");

                int y = 15;
                AddDetailRow("Transaction ID", "#" + row["id"], Color.Empty, ref y);
                AddDetailRow("Student No.", row["student_no"].ToString(), Color.Empty, ref y);
                AddDetailRow("Student Name", row["student_name"].ToString(), Color.Empty, ref y);
                AddDetailRow("Book Title", row["book_title"].ToString(), Color.Empty, ref y);
                AddDetailRow("Librarian", row["librarian_name"].ToString(), Color.Empty, ref y);
                AddDetailRow("Borrow Date", Convert.ToDateTime(row["borrow_date"]).ToString("MMM dd, yyyy  hh:mm tt"), Color.Empty, ref y);
                AddDetailRow("Due Date", Convert.ToDateTime(row["due_date"]).ToString("MMM dd, yyyy"), Color.Empty, ref y);
                AddDetailRow("Returned Date", returnedDate, Color.Empty, ref y);
                AddDetailRow("Status", statusVal.ToUpper(), statusColor, ref y);
                AddDetailRow("Remarks", string.IsNullOrEmpty(row["remarks"].ToString()) ? "--" : row["remarks"].ToString(), Color.Empty, ref y);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load detail: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddDetailRow(string labelText, string valueText, Color valueColor, ref int y)
        {
            Label lbl = new Label
            {
                Text = labelText,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 90, 150),
                Location = new Point(15, y),
                Size = new Size(155, 24)
            };

            Label val = new Label
            {
                Text = valueText,
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = valueColor == Color.Empty ? Color.FromArgb(40, 30, 80) : valueColor,
                Location = new Point(175, y),
                Size = new Size(265, 24)
            };

            _detailPanel.Controls.Add(lbl);
            _detailPanel.Controls.Add(val);
            y += 28;

            Panel line = new Panel
            {
                Location = new Point(15, y - 4),
                Size = new Size(425, 1),
                BackColor = Color.FromArgb(240, 238, 252)
            };
            _detailPanel.Controls.Add(line);
        }
    }
}