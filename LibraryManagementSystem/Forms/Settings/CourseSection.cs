using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Settings
{
    public class CourseSection : SettingsSectionBase
    {
        private TextBox txtCode, txtName;
        private int _editingId = 0;

        public CourseSection(SettingsRepository repo) : base(repo)
        {
            BuildBaseLayout("Courses", "+ Add",
                new[] { "colCode", "colName" },
                new[] { "Course Code", "Course Name" });
            LoadData();
        }

        protected override Panel BuildFormPanel(string addButtonText)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent };

            panel.Controls.Add(MakeLabel("Course Code", 0, 0));
            txtCode = MakeTextBox(0, 18, 120);

            panel.Controls.Add(MakeLabel("Course Name", 135, 0));
            txtName = MakeTextBox(135, 18, 250);

            var btnSave = MakeButton("+ Add", System.Drawing.Color.FromArgb(95, 75, 180), 395, 16);
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtCode.Text) || string.IsNullOrWhiteSpace(txtName.Text))
                { MessageHelper.ShowWarning("Both course code and name are required."); return; }

                try
                {
                    if (_editingId == 0)
                    {
                        Repo.AddCourse(txtCode.Text.Trim(), txtName.Text.Trim());
                        MessageHelper.ShowSuccess("Course added.");
                    }
                    else
                    {
                        Repo.UpdateCourse(_editingId, txtCode.Text.Trim(), txtName.Text.Trim());
                        MessageHelper.ShowSuccess("Course updated.");
                        _editingId = 0;
                        btnSave.Text = "+ Add";
                    }
                    txtCode.Clear();
                    txtName.Clear();
                    LoadData();
                }
                catch (Exception ex) { MessageHelper.ShowError(ex.Message); }
            };

            panel.Controls.Add(txtCode);
            panel.Controls.Add(txtName);
            panel.Controls.Add(btnSave);
            return panel;
        }

        protected override void LoadData()
        {
            var data = Repo.GetCourses();
            dgv.Rows.Clear();
            foreach (System.Data.DataRow row in data.Rows)
            {
                dgv.Rows.Add(row["course_code"], row["course_name"]);
                dgv.Rows[dgv.Rows.Count - 1].Tag = row["id"];
            }
        }

        protected override void OnEdit(int id, int rowIndex)
        {
            _editingId = id;
            txtCode.Text = dgv.Rows[rowIndex].Cells["colCode"].Value?.ToString();
            txtName.Text = dgv.Rows[rowIndex].Cells["colName"].Value?.ToString();
        }

        protected override void OnDelete(int id, int rowIndex)
        {
            if (MessageHelper.ShowConfirm("Delete this course?") != DialogResult.Yes) return;
            try
            {
                Repo.DeleteCourse(id);
                MessageHelper.ShowSuccess("Course deleted.");
                LoadData();
            }
            catch (Exception ex) { MessageHelper.ShowError("Cannot delete. It may be in use.\n\n" + ex.Message); }
        }
    }
}