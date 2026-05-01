using LibrarySystem.Core.Helpers;
using LibrarySystem.Repositories;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Settings
{
    public class LibrarianUserSection : SettingsSectionBase
    {
        private TextBox txtFullName, txtContact, txtEmail, txtUsername, txtPassword;
        private ComboBox cboShift, cboRole;
        private int _editingLibrarianId = 0;
        private int _editingUserId = 0;

        public LibrarianUserSection(SettingsRepository repo) : base(repo)
        {
            BuildBaseLayout("Librarians & Users", "+ Add",
                new[] { "colName", "colShift", "colContact", "colUsername", "colRole" },
                new[] { "Full Name", "Shift", "Contact No", "Username", "Role" });
            LoadData();
        }

        protected override Panel BuildFormPanel(string addButtonText)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Height = 60
            };

            panel.Controls.Add(MakeLabel("Full Name", 0, 0));
            txtFullName = MakeTextBox(0, 18, 150);

            panel.Controls.Add(MakeLabel("Shift", 160, 0));
            cboShift = MakeComboBox(160, 18, 110);
            cboShift.Items.AddRange(new object[] { "Morning", "Afternoon", "Evening" });
            cboShift.SelectedIndex = 0;

            panel.Controls.Add(MakeLabel("Contact No", 280, 0));
            txtContact = MakeTextBox(280, 18, 120);

            panel.Controls.Add(MakeLabel("Email", 410, 0));
            txtEmail = MakeTextBox(410, 18, 150);

            panel.Controls.Add(MakeLabel("Username", 570, 0));
            txtUsername = MakeTextBox(570, 18, 120);

            panel.Controls.Add(MakeLabel(_editingLibrarianId == 0 ? "Password" : "Password (blank = no change)", 700, 0));
            txtPassword = MakeTextBox(700, 18, 120);
            txtPassword.PasswordChar = '●';

            panel.Controls.Add(MakeLabel("Role", 830, 0));
            cboRole = MakeComboBox(830, 18, 100);
            cboRole.Items.AddRange(new object[] { "admin", "staff" });
            cboRole.SelectedIndex = 1;

            var btnSave = MakeButton("+ Add", Color.FromArgb(95, 75, 180), 940, 16, 100);
            btnSave.Click += (s, e) => HandleSave(btnSave);

            panel.Controls.Add(txtFullName);
            panel.Controls.Add(cboShift);
            panel.Controls.Add(txtContact);
            panel.Controls.Add(txtEmail);
            panel.Controls.Add(txtUsername);
            panel.Controls.Add(txtPassword);
            panel.Controls.Add(cboRole);
            panel.Controls.Add(btnSave);

            return panel;
        }

        private void HandleSave(Button btnSave)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            { MessageHelper.ShowWarning("Full name is required."); return; }
            if (string.IsNullOrWhiteSpace(txtContact.Text))
            { MessageHelper.ShowWarning("Contact number is required."); return; }
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            { MessageHelper.ShowWarning("Username is required."); return; }
            if (_editingLibrarianId == 0 && string.IsNullOrWhiteSpace(txtPassword.Text))
            { MessageHelper.ShowWarning("Password is required for new users."); return; }

            try
            {
                string shift = cboShift.SelectedItem?.ToString() ?? "Morning";
                string role = cboRole.SelectedItem?.ToString() ?? "staff";

                if (_editingLibrarianId == 0)
                {
                    Repo.AddLibrarianAndUser(
                        txtFullName.Text.Trim(), shift,
                        txtContact.Text.Trim(), txtEmail.Text.Trim(),
                        txtUsername.Text.Trim(), txtPassword.Text.Trim(), role);
                    MessageHelper.ShowSuccess("Librarian and user account created successfully.");
                }
                else
                {
                    Repo.UpdateLibrarianAndUser(
                        _editingLibrarianId, _editingUserId,
                        txtFullName.Text.Trim(), shift,
                        txtContact.Text.Trim(), txtEmail.Text.Trim(),
                        txtUsername.Text.Trim(), txtPassword.Text.Trim(), role);
                    MessageHelper.ShowSuccess("Librarian and user updated successfully.");
                    _editingLibrarianId = 0;
                    _editingUserId = 0;
                    btnSave.Text = "+ Add";
                }

                ClearForm();
                LoadData();
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Unable to save. " + ex.Message);
            }
        }

        private void ClearForm()
        {
            txtFullName.Clear();
            txtContact.Clear();
            txtEmail.Clear();
            txtUsername.Clear();
            txtPassword.Clear();
            cboShift.SelectedIndex = 0;
            cboRole.SelectedIndex = 1;
        }

        protected override void LoadData()
        {
            var data = Repo.GetLibrarians();
            dgv.Rows.Clear();
            foreach (System.Data.DataRow row in data.Rows)
            {
                int idx = dgv.Rows.Add(
                    row["full_name"],
                    row["shift"],
                    row["contact_no"],
                    row["username"] == DBNull.Value ? "--" : row["username"],
                    row["role"] == DBNull.Value ? "--" : row["role"]
                );
                dgv.Rows[idx].Tag = new int[]
                {
                    Convert.ToInt32(row["id"]),
                    row["user_id"] == DBNull.Value ? 0 : Convert.ToInt32(row["user_id"])
                };
            }
        }

        protected override void OnEdit(int id, int rowIndex)
        {
            var ids = (int[])dgv.Rows[rowIndex].Tag;
            _editingLibrarianId = ids[0];
            _editingUserId = ids[1];

            txtFullName.Text = dgv.Rows[rowIndex].Cells["colName"].Value?.ToString();
            txtContact.Text = dgv.Rows[rowIndex].Cells["colContact"].Value?.ToString();
            txtUsername.Text = dgv.Rows[rowIndex].Cells["colUsername"].Value?.ToString();
            txtPassword.Clear();

            string shift = dgv.Rows[rowIndex].Cells["colShift"].Value?.ToString();
            cboShift.SelectedItem = shift;

            string role = dgv.Rows[rowIndex].Cells["colRole"].Value?.ToString();
            cboRole.SelectedItem = role;
        }

        protected override void OnDelete(int id, int rowIndex)
        {
            if (MessageHelper.ShowConfirm(
                "Delete this librarian and their user account?\nThis cannot be undone.") != DialogResult.Yes)
                return;

            try
            {
                Repo.DeleteLibrarian(id);
                MessageHelper.ShowSuccess("Librarian and user account deleted.");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Cannot delete. " + ex.Message);
            }
        }
    }
}