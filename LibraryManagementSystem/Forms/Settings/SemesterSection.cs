using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Settings
{
    public class SemesterSection : SettingsSectionBase
    {
        private TextBox txtName;
        private int _editingId = 0;

        public SemesterSection(SettingsRepository repo) : base(repo)
        {
            BuildBaseLayout("Semester", "+ Add",
                new[] { "colName" },
                new[] { "Semester Name" },
                hasActiveCol: true);
            LoadData();
        }

        protected override Panel BuildFormPanel(string addButtonText)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent };

            panel.Controls.Add(MakeLabel("Semester Name", 0, 0));
            txtName = MakeTextBox(0, 18, 200);

            var btnSave = MakeButton("+ Add", System.Drawing.Color.FromArgb(95, 75, 180), 210, 16);
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                { MessageHelper.ShowWarning("Semester name is required."); return; }

                try
                {
                    if (_editingId == 0)
                    {
                        Repo.AddSemester(txtName.Text.Trim());
                        MessageHelper.ShowSuccess("Semester added.");
                    }
                    else
                    {
                        Repo.UpdateSemester(_editingId, txtName.Text.Trim());
                        MessageHelper.ShowSuccess("Semester updated.");
                        _editingId = 0;
                        btnSave.Text = "+ Add";
                    }
                    txtName.Clear();
                    LoadData();
                }
                catch (Exception ex) { MessageHelper.ShowError(ex.Message); }
            };

            panel.Controls.Add(txtName);
            panel.Controls.Add(btnSave);
            return panel;
        }

        protected override void LoadData()
        {
            var data = Repo.GetSemesters();
            dgv.Rows.Clear();
            foreach (System.Data.DataRow row in data.Rows)
            {
                int idx = dgv.Rows.Add(row["semester_name"]);
                bool active = Convert.ToInt32(row["is_active"]) == 1;
                dgv.Rows[idx].Cells["colActive"].Value = active ? "Yes" : "No";
                dgv.Rows[idx].Tag = row["id"];
            }
        }

        protected override void OnSetActive(int id, int rowIndex)
        {
            try
            {
                Repo.SetActiveSemester(id);
                MessageHelper.ShowSuccess("Semester set as active.");
                LoadData();
            }
            catch (Exception ex) { MessageHelper.ShowError(ex.Message); }
        }

        protected override void OnEdit(int id, int rowIndex)
        {
            _editingId = id;
            txtName.Text = dgv.Rows[rowIndex].Cells["colName"].Value?.ToString();
        }

        protected override void OnDelete(int id, int rowIndex)
        {
            if (MessageHelper.ShowConfirm("Delete this semester?") != DialogResult.Yes) return;
            try
            {
                Repo.DeleteSemester(id);
                MessageHelper.ShowSuccess("Semester deleted.");
                LoadData();
            }
            catch (Exception ex) { MessageHelper.ShowError("Cannot delete. It may be in use.\n\n" + ex.Message); }
        }
    }
}