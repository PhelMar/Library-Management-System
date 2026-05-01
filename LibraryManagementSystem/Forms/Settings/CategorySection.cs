using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Settings
{
    public class CategorySection : SettingsSectionBase
    {
        private TextBox txtName;
        private int _editingId = 0;

        public CategorySection(SettingsRepository repo) : base(repo)
        {
            BuildBaseLayout("Book Categories", "+ Add",
                new[] { "colName" }, new[] { "Category Name" });
            LoadData();
        }

        protected override Panel BuildFormPanel(string addButtonText)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = System.Drawing.Color.Transparent };
            panel.Controls.Add(MakeLabel("Category Name", 0, 0));
            txtName = MakeTextBox(0, 18, 200);

            var btnSave = MakeButton("+ Add", System.Drawing.Color.FromArgb(95, 75, 180), 210, 16);
            btnSave.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                { MessageHelper.ShowWarning("Category name is required."); return; }

                try
                {
                    if (_editingId == 0)
                    {
                        Repo.AddCategory(txtName.Text.Trim());
                        MessageHelper.ShowSuccess("Category added.");
                    }
                    else
                    {
                        Repo.UpdateCategory(_editingId, txtName.Text.Trim());
                        MessageHelper.ShowSuccess("Category updated.");
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
            var data = Repo.GetCategories();
            dgv.Rows.Clear();
            foreach (System.Data.DataRow row in data.Rows)
            {
                dgv.Rows.Add(row["category_name"]);
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
            if (MessageHelper.ShowConfirm("Delete this category?") != DialogResult.Yes) return;
            try
            {
                Repo.DeleteCategory(id);
                MessageHelper.ShowSuccess("Category deleted.");
                LoadData();
            }
            catch (Exception ex) { MessageHelper.ShowError("Cannot delete. It may be in use.\n\n" + ex.Message); }
        }
    }
}