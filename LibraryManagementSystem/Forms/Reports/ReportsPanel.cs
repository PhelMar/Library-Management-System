using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Reports
{
    public class ReportsPanel : Panel
    {
        private ComboBox cboReportType, cboSchoolYear, cboSemester, cboMonth;
        private Button btnGenerate, btnPrint;
        private Panel pnlPreview;
        private Label lblNoData;

        private readonly ReportRepository _repo;
        private DataTable _reportData;
        private DataTable _secondaryData;
        private string _reportTitle = "";
        private string _schoolYearLabel = "";
        private string _semesterLabel = "";
        private string _monthLabel = "";

        private const string SCHOOL_NAME = "Legacy College of Compostela Library";

        public ReportsPanel()
        {
            _repo = new ReportRepository();
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(245, 245, 255);
            this.Padding = new Padding(25, 20, 25, 20);
            BuildUI();
            LoadDropdowns();
        }

        private void BuildUI()
        {
            var mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var lblTitle = new Label
            {
                Text = "Reports",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(0, 10),
                AutoSize = true
            };
            titlePanel.Controls.Add(lblTitle);

            var filterBar = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            cboReportType = MakeCombo(0, 15, 170);
            cboReportType.Items.AddRange(new object[]
            {
                "Books Report",
                "Borrow & Return Report",
                "Fines Report",
                "Overdue Report"
            });
            cboReportType.SelectedIndex = 0;
            cboReportType.SelectedIndexChanged += (s, e) => ToggleFilters();

            AddFilterLabel("School Year", 180, filterBar);
            cboSchoolYear = MakeCombo(180, 15, 130);

            AddFilterLabel("Semester", 320, filterBar);
            cboSemester = MakeCombo(320, 15, 130);

            AddFilterLabel("Month", 460, filterBar);
            cboMonth = MakeCombo(460, 15, 140);
            cboMonth.Items.Add("All Months");
            string[] months = { "January","February","March","April","May","June",
                                 "July","August","September","October","November","December" };
            foreach (var m in months) cboMonth.Items.Add(m);
            cboMonth.SelectedIndex = 0;

            btnGenerate = new Button
            {
                Text = "Generate",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Location = new Point(615, 13),
                Size = new Size(110, 34)
            };
            btnGenerate.FlatAppearance.BorderSize = 0;
            btnGenerate.Click += BtnGenerate_Click;

            btnPrint = new Button
            {
                Text = "🖨 Print",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Location = new Point(735, 13),
                Size = new Size(110, 34),
                Enabled = false
            };
            btnPrint.FlatAppearance.BorderSize = 0;
            btnPrint.Click += BtnPrint_Click;

            filterBar.Controls.Add(cboReportType);
            filterBar.Controls.Add(cboSchoolYear);
            filterBar.Controls.Add(cboSemester);
            filterBar.Controls.Add(cboMonth);
            filterBar.Controls.Add(btnGenerate);
            filterBar.Controls.Add(btnPrint);

            pnlPreview = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            lblNoData = new Label
            {
                Text = "Select a report type and click Generate to preview.",
                Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(160, 150, 200),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlPreview.Controls.Add(lblNoData);

            mainLayout.Controls.Add(titlePanel, 0, 0);
            mainLayout.Controls.Add(filterBar, 0, 1);
            mainLayout.Controls.Add(pnlPreview, 0, 2);

            this.Controls.Add(mainLayout);
        }

        private void LoadDropdowns()
        {
            var schoolYears = _repo.GetSchoolYears();
            cboSchoolYear.DataSource = schoolYears;
            cboSchoolYear.DisplayMember = "display_name";
            cboSchoolYear.ValueMember = "id";

            var semesters = _repo.GetSemesters();
            cboSemester.DataSource = semesters;
            cboSemester.DisplayMember = "display_name";
            cboSemester.ValueMember = "id";

            ToggleFilters();
        }

        private void ToggleFilters()
        {
            bool isBooksReport = cboReportType.SelectedIndex == 0;
            cboSchoolYear.Enabled = !isBooksReport;
            cboSemester.Enabled = !isBooksReport;
            cboMonth.Enabled = !isBooksReport;
        }

        private void BtnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                _reportData = null;
                _secondaryData = null;
                pnlPreview.Controls.Clear();

                int reportIndex = cboReportType.SelectedIndex;
                _reportTitle = cboReportType.SelectedItem.ToString();

                int schoolYearId = 0;
                int semesterId = 0;
                int? month = null;

                if (reportIndex != 0)
                {
                    if (cboSchoolYear.SelectedValue == null)
                    { ShowNoData("Please select a school year."); return; }
                    if (cboSemester.SelectedValue == null)
                    { ShowNoData("Please select a semester."); return; }

                    schoolYearId = Convert.ToInt32(cboSchoolYear.SelectedValue);
                    semesterId = Convert.ToInt32(cboSemester.SelectedValue);

                    _schoolYearLabel = ((DataRowView)cboSchoolYear.SelectedItem)["display_name"].ToString();
                    _semesterLabel = ((DataRowView)cboSemester.SelectedItem)["display_name"].ToString();
                    _monthLabel = cboMonth.SelectedIndex == 0
                        ? "All Months"
                        : cboMonth.SelectedItem.ToString();

                    if (cboMonth.SelectedIndex > 0)
                        month = cboMonth.SelectedIndex;
                }
                else
                {
                    _schoolYearLabel = "N/A";
                    _semesterLabel = "N/A";
                    _monthLabel = "N/A";
                }

                switch (reportIndex)
                {
                    case 0:
                        _reportData = _repo.GetBooksReport();
                        _secondaryData = _repo.GetMostBorrowedBooks();
                        break;
                    case 1:
                        _reportData = _repo.GetBorrowReturnReport(schoolYearId, semesterId, month);
                        break;
                    case 2:
                        _reportData = _repo.GetFinesReport(schoolYearId, semesterId, month);
                        break;
                    case 3:
                        _reportData = _repo.GetOverdueReport(schoolYearId, semesterId, month);
                        break;
                }

                if (_reportData == null || _reportData.Rows.Count == 0)
                {
                    ShowNoData("No data found for the selected filters.");
                    btnPrint.Enabled = false;
                    return;
                }

                RenderPreview();
                btnPrint.Enabled = true;
            }
            catch (Exception ex)
            {
                ShowNoData("Error generating report: " + ex.Message);
            }
        }

        private void RenderPreview()
        {
            pnlPreview.Controls.Clear();

            int contentWidth = pnlPreview.Width > 0 ? pnlPreview.Width - 2 : 900;

            var previewContent = new Panel
            {
                BackColor = Color.White,
                Width = contentWidth,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Location = new Point(0, 0)
            };

            int y = 30;
            int innerWidth = contentWidth - 80;

            // School name
            AddPreviewLabel(previewContent, SCHOOL_NAME,
                new Font("Segoe UI", 14f, FontStyle.Bold),
                Color.FromArgb(40, 30, 80), ref y, true, innerWidth);

            AddPreviewLabel(previewContent, "Library Management System",
                new Font("Segoe UI", 10f),
                Color.FromArgb(100, 90, 150), ref y, true, innerWidth);

            y += 8;
            AddDivider(previewContent, ref y, innerWidth);
            y += 8;

            AddPreviewLabel(previewContent, $"Type of Report   :   {_reportTitle}",
                new Font("Segoe UI", 9f), Color.FromArgb(80, 70, 130), ref y, false, innerWidth);
            AddPreviewLabel(previewContent, $"School Year        :   {_schoolYearLabel}",
                new Font("Segoe UI", 9f), Color.FromArgb(80, 70, 130), ref y, false, innerWidth);
            AddPreviewLabel(previewContent, $"Semester             :   {_semesterLabel}",
                new Font("Segoe UI", 9f), Color.FromArgb(80, 70, 130), ref y, false, innerWidth);
            AddPreviewLabel(previewContent, $"Month                  :   {_monthLabel}",
                new Font("Segoe UI", 9f), Color.FromArgb(80, 70, 130), ref y, false, innerWidth);
            AddPreviewLabel(previewContent, $"Date Generated  :   {DateTime.Now:MMMM dd, yyyy  hh:mm tt}",
                new Font("Segoe UI", 9f), Color.FromArgb(80, 70, 130), ref y, false, innerWidth);

            y += 8;
            AddDivider(previewContent, ref y, innerWidth);
            y += 15;

            AddPreviewLabel(previewContent, _reportTitle.ToUpper(),
                new Font("Segoe UI", 10f, FontStyle.Bold),
                Color.FromArgb(95, 75, 180), ref y, false, innerWidth);
            y += 8;

            var mainGrid = BuildPreviewGrid(_reportData);
            mainGrid.Location = new Point(40, y);
            mainGrid.Width = innerWidth;
            previewContent.Controls.Add(mainGrid);
            y += mainGrid.Height + 20;

            // Fines summary
            if (cboReportType.SelectedIndex == 2 && _reportData.Rows.Count > 0)
            {
                decimal totalFines = 0, paidFines = 0;
                foreach (DataRow row in _reportData.Rows)
                {
                    decimal amt = Convert.ToDecimal(row["Fine_Amount"]);
                    totalFines += amt;
                    if (row["Payment_Status"].ToString() == "paid") paidFines += amt;
                }

                AddPreviewLabel(previewContent,
                    $"Total Fines: ₱{totalFines:N2}   |   Collected: ₱{paidFines:N2}   |   Unpaid: ₱{(totalFines - paidFines):N2}",
                    new Font("Segoe UI", 9.5f, FontStyle.Bold),
                    Color.FromArgb(231, 76, 60), ref y, false, innerWidth);
                y += 10;
            }

            // Most borrowed for books report
            if (cboReportType.SelectedIndex == 0 && _secondaryData != null && _secondaryData.Rows.Count > 0)
            {
                y += 10;
                AddPreviewLabel(previewContent, "TOP 10 MOST BORROWED BOOKS",
                    new Font("Segoe UI", 10f, FontStyle.Bold),
                    Color.FromArgb(95, 75, 180), ref y, false, innerWidth);
                y += 8;

                var secondGrid = BuildPreviewGrid(_secondaryData);
                secondGrid.Location = new Point(40, y);
                secondGrid.Width = innerWidth;
                previewContent.Controls.Add(secondGrid);
                y += secondGrid.Height + 20;
            }

            // Footer
            AddDivider(previewContent, ref y, innerWidth);
            y += 8;
            AddPreviewLabel(previewContent,
                $"Generated by Library Management System  —  {DateTime.Now:MMMM dd, yyyy}",
                new Font("Segoe UI", 8f, FontStyle.Italic),
                Color.Gray, ref y, true, innerWidth);

            previewContent.Height = y + 40;
            pnlPreview.Controls.Add(previewContent);
        }

        private DataGridView BuildPreviewGrid(DataTable dt)
        {
            var dgv = new DataGridView
            {
                DataSource = dt,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f),
                GridColor = Color.FromArgb(220, 215, 240),
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Height = Math.Min(dt.Rows.Count * 28 + 36, 400)
            };

            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Padding = new Padding(4, 0, 0, 0)
            };

            dgv.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = Color.FromArgb(40, 30, 80),
                Padding = new Padding(4, 0, 0, 0)
            };

            dgv.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(248, 247, 255)
            };

            foreach (DataGridViewColumn col in dgv.Columns)
                col.HeaderText = col.HeaderText.Replace("_", " ");

            return dgv;
        }

        private void BtnPrint_Click(object sender, EventArgs e)
        {
            var printDoc = new PrintDocument();
            printDoc.DefaultPageSettings.Landscape = true;
            printDoc.DefaultPageSettings.Margins = new Margins(50, 50, 50, 50);

            int printPage = 0;
            int rowIndex = 0;

            printDoc.PrintPage += (s, ev) =>
            {
                Graphics g = ev.Graphics;
                float pageWidth = ev.MarginBounds.Width;
                float x = ev.MarginBounds.Left;
                float y = ev.MarginBounds.Top;

                // Header
                var titleFont = new Font("Segoe UI", 14f, FontStyle.Bold);
                var subFont = new Font("Segoe UI", 9f);
                var boldFont = new Font("Segoe UI", 9f, FontStyle.Bold);
                var smallFont = new Font("Segoe UI", 8f);
                var brush = new SolidBrush(Color.FromArgb(40, 30, 80));
                var grayBrush = new SolidBrush(Color.FromArgb(80, 70, 130));
                var pen = new Pen(Color.FromArgb(95, 75, 180), 1f);

                if (printPage == 0)
                {
                    // School name centered
                    SizeF titleSize = g.MeasureString(SCHOOL_NAME, titleFont);
                    g.DrawString(SCHOOL_NAME, titleFont, brush, x + (pageWidth - titleSize.Width) / 2, y);
                    y += titleSize.Height + 4;

                    string sub = "Library Management System";
                    SizeF subSize = g.MeasureString(sub, subFont);
                    g.DrawString(sub, subFont, grayBrush, x + (pageWidth - subSize.Width) / 2, y);
                    y += subSize.Height + 10;

                    g.DrawLine(pen, x, y, x + pageWidth, y);
                    y += 12;

                    g.DrawString($"Type of Report  :  {_reportTitle}", subFont, grayBrush, x, y); y += 18;
                    g.DrawString($"School Year      :  {_schoolYearLabel}", subFont, grayBrush, x, y); y += 18;
                    g.DrawString($"Semester           :  {_semesterLabel}", subFont, grayBrush, x, y); y += 18;
                    g.DrawString($"Month                :  {_monthLabel}", subFont, grayBrush, x, y); y += 18;
                    g.DrawString($"Date Generated :  {DateTime.Now:MMMM dd, yyyy  hh:mm tt}", subFont, grayBrush, x, y); y += 18;

                    y += 8;
                    g.DrawLine(pen, x, y, x + pageWidth, y);
                    y += 15;

                    g.DrawString(_reportTitle.ToUpper(), boldFont, new SolidBrush(Color.FromArgb(95, 75, 180)), x, y);
                    y += 20;
                }

                // Column headers
                if (rowIndex == 0 || printPage > 0)
                {
                    float colWidth = pageWidth / _reportData.Columns.Count;
                    g.FillRectangle(new SolidBrush(Color.FromArgb(95, 75, 180)),
                        x, y, pageWidth, 22);

                    float cx = x;
                    foreach (DataColumn col in _reportData.Columns)
                    {
                        g.DrawString(col.ColumnName.Replace("_", " "),
                            boldFont, Brushes.White, cx + 4, y + 4);
                        cx += colWidth;
                    }
                    y += 24;

                    // Data rows
                    bool alt = false;
                    while (rowIndex < _reportData.Rows.Count)
                    {
                        if (y + 22 > ev.MarginBounds.Bottom - 30)
                        {
                            ev.HasMorePages = true;
                            printPage++;
                            return;
                        }

                        DataRow row = _reportData.Rows[rowIndex];
                        if (alt)
                            g.FillRectangle(new SolidBrush(Color.FromArgb(248, 247, 255)),
                                x, y, pageWidth, 20);

                        cx = x;
                        foreach (DataColumn col in _reportData.Columns)
                        {
                            g.DrawString(row[col].ToString(), smallFont,
                                new SolidBrush(Color.FromArgb(40, 30, 80)), cx + 4, y + 3);
                            cx += colWidth;
                        }

                        y += 20;
                        alt = !alt;
                        rowIndex++;
                    }

                    // Fines summary
                    if (cboReportType.SelectedIndex == 2)
                    {
                        y += 10;
                        decimal total = 0, paid = 0;
                        foreach (DataRow row in _reportData.Rows)
                        {
                            decimal amt = Convert.ToDecimal(row["Fine_Amount"]);
                            total += amt;
                            if (row["Payment_Status"].ToString() == "paid") paid += amt;
                        }
                        g.DrawString(
                            $"Total: ₱{total:N2}   Collected: ₱{paid:N2}   Unpaid: ₱{(total - paid):N2}",
                            boldFont, new SolidBrush(Color.FromArgb(231, 76, 60)), x, y);
                        y += 20;
                    }

                    // Footer
                    y = ev.MarginBounds.Bottom - 20;
                    g.DrawLine(pen, x, y, x + pageWidth, y);
                    y += 4;
                    g.DrawString(
                        $"Generated by Library Management System  —  {DateTime.Now:MMMM dd, yyyy}",
                        new Font("Segoe UI", 7.5f, FontStyle.Italic),
                        Brushes.Gray, x, y);

                    ev.HasMorePages = false;
                    printPage = 0;
                    rowIndex = 0;
                }
            };

            var printDialog = new PrintDialog
            {
                Document = printDoc,
                UseEXDialog = true
            };

            if (printDialog.ShowDialog() == DialogResult.OK)
                printDoc.Print();
        }

        private void ShowNoData(string message)
        {
            pnlPreview.Controls.Clear();
            lblNoData = new Label
            {
                Text = message,
                Font = new Font("Segoe UI", 11f),
                ForeColor = Color.FromArgb(160, 150, 200),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlPreview.Controls.Add(lblNoData);
            btnPrint.Enabled = false;
        }

        private void AddPreviewLabel(Panel parent, string text, Font font,
    Color color, ref int y, bool centered, int width)
        {
            var lbl = new Label
            {
                Text = text,
                Font = font,
                ForeColor = color,
                AutoSize = false,
                Size = new Size(width, 22),
                Location = new Point(40, y),
                TextAlign = centered
                    ? ContentAlignment.MiddleCenter
                    : ContentAlignment.MiddleLeft
            };
            parent.Controls.Add(lbl);
            y += 24;
        }

        private void AddDivider(Panel parent, ref int y, int width)
        {
            var line = new Panel
            {
                Location = new Point(40, y),
                Size = new Size(width, 1),
                BackColor = Color.FromArgb(200, 195, 230)
            };
            parent.Controls.Add(line);
            y += 4;
        }

        private ComboBox MakeCombo(int x, int y, int width)
        {
            return new ComboBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 32),
                Font = new Font("Segoe UI", 10f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
        }

        private void AddFilterLabel(string text, int x, Panel parent)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.Gray,
                Location = new Point(x, 0),
                AutoSize = true
            });
        }
    }
}