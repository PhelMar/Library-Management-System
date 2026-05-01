using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Settings
{
    public class YearLevelSection : SettingsSectionBase
    {
        private TextBox txtName;
        private int _editingId = 0;

        public YearLevelSection(SettingsRepository repo) : base(repo)
        {
            BuildBaseLayout("Year Levels", "+ Add",
                new[] { "colName" }, new[] { "Level Name" });
            LoadData();
        }

        protected override Panel BuildFormPanel(string addButtonText)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent };
            panel.Controls.Add(MakeLabel("Level Name", 0, 0));
            txtName = MakeTextBox(0, 18, 200);

            var btnSave = MakeButton("+ Add", System.Drawing.Color.FromArgb(95, 75, 180), 210, 16);
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                { MessageHelper.ShowWarning("Level name is required."); return; }

                try
                {
                    if (_editingId == 0)
                    {
                        Repo.AddYearLevel(txtName.Text.Trim());
                        MessageHelper.ShowSuccess("Year level added.");
                    }
                    else
                    {
                        Repo.UpdateYearLevel(_editingId, txtName.Text.Trim());
                        MessageHelper.ShowSuccess("Year level updated.");
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
            var data = Repo.GetYearLevels();
            dgv.Rows.Clear();
            foreach (System.Data.DataRow row in data.Rows)
            {
                dgv.Rows.Add(row["level_name"]);
                dgv.Rows[dgv.Rows.Count - 1].Tag = row["id"];
            }
        }

        protected override void OnEdit(int id, int rowIndex)
        {
            _editingId = id;
            txtName.Text = dgv.Rows[rowIndex].Cells["colName"].Value?.ToString();
        }

        protected override void OnDelete(int id, int rowIndex)
        {
            if (MessageHelper.ShowConfirm("Delete this year level?") != DialogResult.Yes) return;
            try
            {
                Repo.DeleteYearLevel(id);
                MessageHelper.ShowSuccess("Year level deleted.");
                LoadData();
            }
            catch (Exception ex) { MessageHelper.ShowError("Cannot delete. It may be in use.\n\n" + ex.Message); }
        }
    }
}