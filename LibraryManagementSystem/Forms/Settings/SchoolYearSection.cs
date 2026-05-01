using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Settings
{
    public class SchoolYearSection : SettingsSectionBase
    {
        private TextBox txtYear;
        private int _editingId = 0;

        public SchoolYearSection(SettingsRepository repo) : base(repo)
        {
            BuildBaseLayout("School Year", "+ Add",
                new[] { "colYear" },
                new[] { "Year Label" },
                hasActiveCol: true);
            LoadData();
        }

        protected override Panel BuildFormPanel(string addButtonText)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

            panel.Controls.Add(MakeLabel("Year Label (e.g. 2026-2027)", 0, 0));
            txtYear = MakeTextBox(0, 18, 180);

            var btnSave = MakeButton("+ Add", Color.FromArgb(95, 75, 180), 190, 16);
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtYear.Text))
                { MessageHelper.ShowWarning("Year label is required."); return; }

                try
                {
                    if (_editingId == 0)
                    {
                        Repo.AddSchoolYear(txtYear.Text.Trim());
                        MessageHelper.ShowSuccess("School year added successfully.");
                    }
                    else
                    {
                        Repo.UpdateSchoolYear(_editingId, txtYear.Text.Trim());
                        MessageHelper.ShowSuccess("School year updated successfully.");
                        _editingId = 0;
                        btnSave.Text = "+ Add";
                    }
                    txtYear.Clear();
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageHelper.ShowError("Unable to save. " + ex.Message);
                }
            };

            panel.Controls.Add(txtYear);
            panel.Controls.Add(btnSave);
            return panel;
        }

        protected override void LoadData()
        {
            var data = Repo.GetSchoolYears();
            dgv.Rows.Clear();
            foreach (System.Data.DataRow row in data.Rows)
            {
                int idx = dgv.Rows.Add(row["year_label"]);
                bool active = Convert.ToInt32(row["is_active"]) == 1;
                dgv.Rows[idx].Cells["colActive"].Value = active ? "Yes" : "No";
                dgv.Rows[idx].Tag = row["id"];
            }
        }

        protected override void OnSetActive(int id, int rowIndex)
        {
            try
            {
                Repo.SetActiveSchoolYear(id);
                MessageHelper.ShowSuccess("School year set as active.");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Unable to set active. " + ex.Message);
            }
        }

        protected override void OnEdit(int id, int rowIndex)
        {
            _editingId = id;
            txtYear.Text = dgv.Rows[rowIndex].Cells["colYear"].Value?.ToString();
        }

        protected override void OnDelete(int id, int rowIndex)
        {
            if (MessageHelper.ShowConfirm("Delete this school year?") != System.Windows.Forms.DialogResult.Yes) return;
            try
            {
                Repo.DeleteSchoolYear(id);
                MessageHelper.ShowSuccess("School year deleted.");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Cannot delete. It may be in use.\n\n" + ex.Message);
            }
        }
    }
}