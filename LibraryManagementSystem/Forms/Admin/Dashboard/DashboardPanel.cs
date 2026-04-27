using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using LibrarySystem.Repositories;

namespace LibrarySystem.Forms.Admin.Dashboard
{
    public class DashboardPanel : Panel
    {
        private FlowLayoutPanel cardPanel;
        private Chart chart;
        private Label lblTitle;
        private Label lblSub;
        private TableLayoutPanel mainLayout;
        private DashboardRepository repository;

        public DashboardPanel()
        {
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.FromArgb(245, 245, 255);
            this.Padding = new Padding(25, 20, 25, 20);
            this.HandleCreated += async (s, e) =>
            {
                ResizeCards();
                await LoadDashboardData();
            };
            this.Resize += DashboardPanel_Resize;
            BuildUI();
            repository = new DashboardRepository();
        }

        private bool dataLoaded = false;

        private void BuildUI()
        {
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = Color.Transparent
            };

            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 140f));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            var titlePanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            lblTitle = new Label
            {
                Text = "Dashboard",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Location = new Point(0, 0),
                AutoSize = true
            };

            lblSub = new Label
            {
                Text = "Welcome back, Admin!",
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.Gray,
                Location = new Point(2, 38),
                AutoSize = true
            };

            titlePanel.Controls.Add(lblTitle);
            titlePanel.Controls.Add(lblSub);

            cardPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 5, 0, 5)
            };

            cardPanel.Controls.Add(CreateCard("Total Students", "0", Color.FromArgb(95, 75, 180)));
            cardPanel.Controls.Add(CreateCard("Total Books", "0", Color.FromArgb(52, 152, 219)));
            cardPanel.Controls.Add(CreateCard("Books Available", "0", Color.FromArgb(39, 174, 96)));
            cardPanel.Controls.Add(CreateCard("Students Due", "0", Color.FromArgb(231, 76, 60)));

            var chartContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 0)
            };

            var lblChart = new Label
            {
                Text = "Books Overview",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(40, 30, 80),
                Dock = DockStyle.Top,
                Height = 30
            };

            chart = new Chart
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                MinimumSize = new Size(100, 100)
            };

            var chartArea = new ChartArea("BookStats");
            chartArea.BackColor = Color.White;
            chartArea.AxisX.LabelStyle.Font = new Font("Segoe UI", 9f);
            chartArea.AxisY.LabelStyle.Font = new Font("Segoe UI", 9f);
            chartArea.AxisX.MajorGrid.LineColor = Color.FromArgb(230, 230, 230);
            chartArea.AxisY.MajorGrid.LineColor = Color.FromArgb(230, 230, 230);
            chart.ChartAreas.Add(chartArea);

            var series = new Series("Books")
            {
                ChartType = SeriesChartType.Bar,
                IsValueShownAsLabel = true,
                Font = new Font("Segoe UI", 9f)
            };

            series.Points.AddXY("Total Books", 0);
            series.Points.AddXY("Books Destroyed", 0);
            series.Points.AddXY("Books Lost", 0);

            series.Points[0].Color = Color.FromArgb(95, 75, 180);
            series.Points[1].Color = Color.FromArgb(231, 76, 60);
            series.Points[2].Color = Color.FromArgb(230, 126, 34);

            chart.Series.Add(series);
            chart.Legends.Add(new Legend { Font = new Font("Segoe UI", 9f), BackColor = Color.White });

            chartContainer.Controls.Add(chart);
            chartContainer.Controls.Add(lblChart);

            mainLayout.Controls.Add(titlePanel, 0, 0);
            mainLayout.Controls.Add(cardPanel, 0, 1);
            mainLayout.Controls.Add(chartContainer, 0, 2);

            this.Controls.Add(mainLayout);
        }

        private void DashboardPanel_Resize(object sender, EventArgs e)
        {
            ResizeCards();
        }

        private void ResizeCards()
        {
            if (cardPanel == null) return;
            if (cardPanel.Width <= 0 || cardPanel.Height <= 0) return;

            int totalWidth = cardPanel.Width - 20;
            int cardCount = cardPanel.Controls.Count;
            if (cardCount == 0) return;

            int gap = 15;
            int cardWidth = (totalWidth - (gap * (cardCount - 1))) / cardCount;
            int cardHeight = 110;

            foreach (Control ctrl in cardPanel.Controls)
            {
                ctrl.Size = new Size(cardWidth < 150 ? 150 : cardWidth, cardHeight);
                ctrl.Margin = new Padding(0, 0, gap, 0);
            }
        }

        private Panel CreateCard(string title, string value, Color color)
        {
            var card = new Panel
            {
                Size = new Size(200, 110),
                BackColor = color,
                Margin = new Padding(0, 0, 15, 0)
            };

            var lblValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 26f, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 12),
                AutoSize = true
            };

            var lblName = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(220, 220, 255),
                Location = new Point(15, 68),
                AutoSize = true
            };

            card.Controls.Add(lblValue);
            card.Controls.Add(lblName);

            return card;
        }

        private async System.Threading.Tasks.Task LoadDashboardData()
        {
            try
            {
                ShowLoadingState();

                var totalStudents = await System.Threading.Tasks.Task.Run(() => repository.GetTotalStudents());
                var totalBooks = await System.Threading.Tasks.Task.Run(() => repository.GetTotalBooks());
                var availableBooks = await System.Threading.Tasks.Task.Run(() => repository.GetAvailableBooks());
                var studentsDue = await System.Threading.Tasks.Task.Run(() => repository.GetStudentsDue());
                var booksLost = await System.Threading.Tasks.Task.Run(() => repository.GetBooksLost());
                var booksDamaged = await System.Threading.Tasks.Task.Run(() => repository.GetBooksDamaged());

                var stats = new DashboardStats
                {
                    TotalStudents = totalStudents,
                    TotalBooks = totalBooks,
                    AvailableBooks = availableBooks,
                    StudentsDue = studentsDue,
                    BooksLost = booksLost,
                    BooksDamaged = booksDamaged
                };

                UpdateCards(stats);
                UpdateChart(stats);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                HideLoadingState();
            }
        }

        private void UpdateCards(DashboardStats stats)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateCards(stats)));
                return;
            }

            var cards = cardPanel.Controls.Cast<Panel>().ToArray();

            if (cards.Length >= 4)
            {
                SetCardValue(cards[0], stats.TotalStudents.ToString("N0"));
                SetCardValue(cards[1], stats.TotalBooks.ToString("N0"));
                SetCardValue(cards[2], stats.AvailableBooks.ToString("N0"));
                SetCardValue(cards[3], stats.StudentsDue.ToString("N0"));
            }
        }

        private void SetCardValue(Panel card, string value)
        {
            foreach (Control control in card.Controls)
            {
                if (control is Label label && label.Font.Bold && label.Font.Size > 20)
                {
                    label.Text = value;
                    break;
                }
            }
        }

        private void UpdateChart(DashboardStats stats)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateChart(stats)));
                return;
            }

            if (chart.Series.Count == 0) return;

            var series = chart.Series["Books"];
            series.Points.Clear();

            series.Points.AddXY("Total Books", stats.TotalBooks);
            series.Points.AddXY("Books Destroyed", stats.BooksLost + stats.BooksDamaged);
            series.Points.AddXY("Books Lost", stats.BooksLost);

            series.Points[0].Color = Color.FromArgb(95, 75, 180);
            series.Points[1].Color = Color.FromArgb(231, 76, 60);
            series.Points[2].Color = Color.FromArgb(230, 126, 34);

            chart.Invalidate();
        }

        private void ShowLoadingState()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ShowLoadingState));
                return;
            }

            foreach (Control card in cardPanel.Controls)
            {
                SetCardValue(card as Panel, "...");
            }
            this.Cursor = Cursors.WaitCursor;
        }

        private void HideLoadingState()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(HideLoadingState));
                return;
            }

            this.Cursor = Cursors.Default;
        }
    }
}