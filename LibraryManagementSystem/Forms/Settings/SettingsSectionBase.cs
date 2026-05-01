using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Settings
{
    public abstract class SettingsSectionBase : Panel
    {
        protected DataGridView dgv;
        protected readonly SettingsRepository Repo;

        protected SettingsSectionBase(SettingsRepository repo)
        {
            Repo = repo;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
        }

        protected void BuildBaseLayout(string title, string addButtonText,
            string[] columnNames, string[] columnHeaders, bool hasActiveCol = false)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            // Title row
            var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            titlePanel.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(0, 10),
                AutoSize = true
            });

            // Form row
            var formPanel = BuildFormPanel(addButtonText);

            // Grid
            dgv = new DataGridView
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
                ColumnHeadersHeight = 38,
                RowTemplate = { Height = 38 }
            };

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Padding = new Padding(5, 0, 0, 0)
            };

            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 30, 80),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80),
                Padding = new Padding(5, 0, 0, 0)
            };

            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 247, 255),
                SelectionBackColor = Color.FromArgb(220, 215, 245),
                SelectionForeColor = Color.FromArgb(40, 30, 80)
            };

            for (int i = 0; i < columnNames.Length; i++)
            {
                dgv.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = columnNames[i],
                    HeaderText = columnHeaders[i],
                    FillWeight = 30
                });
            }

            if (hasActiveCol)
            {
                dgv.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = "colActive",
                    HeaderText = "Active",
                    FillWeight = 10
                });
                dgv.Columns.Add(new DataGridViewButtonColumn
                {
                    Name = "colSetActive",
                    HeaderText = "",
                    Text = "Set Active",
                    UseColumnTextForButtonValue = true,
                    FillWeight = 12
                });
            }

            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colEdit",
                HeaderText = "",
                Text = "Edit",
                UseColumnTextForButtonValue = true,
                FillWeight = 8
            });
            dgv.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "colDelete",
                HeaderText = "",
                Text = "Delete",
                UseColumnTextForButtonValue = true,
                FillWeight = 8
            });

            dgv.CellFormatting += DgvCellFormatting;
            dgv.CellContentClick += DgvCellContentClick;

            layout.Controls.Add(titlePanel, 0, 0);
            layout.Controls.Add(formPanel, 0, 1);
            layout.Controls.Add(dgv, 0, 2);

            this.Controls.Add(layout);
        }

        private void DgvCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string col = dgv.Columns[e.ColumnIndex].Name;

            if (col == "colSetActive")
            {
                var cell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null)
                {
                    cell.Style.BackColor = Color.FromArgb(39, 174, 96);
                    cell.Style.ForeColor = Color.White;
                }
            }
            else if (col == "colEdit")
            {
                var cell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null)
                {
                    cell.Style.BackColor = Color.FromArgb(230, 126, 34);
                    cell.Style.ForeColor = Color.White;
                }
            }
            else if (col == "colDelete")
            {
                var cell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex] as DataGridViewButtonCell;
                if (cell != null)
                {
                    cell.Style.BackColor = Color.FromArgb(231, 76, 60);
                    cell.Style.ForeColor = Color.White;
                }
            }
            else if (col == "colActive")
            {
                string val = e.Value?.ToString() ?? "";
                e.CellStyle.ForeColor = val == "Yes"
                    ? Color.FromArgb(39, 174, 96)
                    : Color.FromArgb(180, 180, 180);
                e.CellStyle.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            }
        }

        private void DgvCellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string col = dgv.Columns[e.ColumnIndex].Name;
            int id = Convert.ToInt32(dgv.Rows[e.RowIndex].Tag);

            if (col == "colSetActive") OnSetActive(id, e.RowIndex);
            else if (col == "colEdit") OnEdit(id, e.RowIndex);
            else if (col == "colDelete") OnDelete(id, e.RowIndex);
        }

        protected abstract Panel BuildFormPanel(string addButtonText);
        protected abstract void LoadData();
        protected virtual void OnSetActive(int id, int rowIndex) { }
        protected abstract void OnEdit(int id, int rowIndex);
        protected abstract void OnDelete(int id, int rowIndex);

        protected Button MakeButton(string text, Color backColor, int x, int y, int width = 110)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 34),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        protected TextBox MakeTextBox(int x, int y, int width = 200)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 30),
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        protected ComboBox MakeComboBox(int x, int y, int width = 200)
        {
            return new ComboBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 30),
                Font = new Font("Segoe UI", 10f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        protected Label MakeLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.Gray
            };
        }
    }
}