using LibrarySystem.Core.Helpers;
using LibrarySystem.Models;
using LibrarySystem.Repositories;
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace LibrarySystem.Forms.Students
{
    public class StudentAddEditForm : Form
    {
        private readonly int _studentDbId;
        private readonly int _enrollmentId;
        private readonly StudentRepository _studentRepo;
        private readonly bool _isEdit;

        private TextBox txtStudentId, txtStudentName, txtContactNo, txtEmail;
        private ComboBox cboCourse, cboYearLevel;
        private Label lblActivePeriod;
        private Button btnSave, btnCancel;

        public StudentAddEditForm(int studentDbId, int enrollmentId)
        {
            _studentDbId = studentDbId;
            _enrollmentId = enrollmentId;
            _isEdit = studentDbId > 0;
            _studentRepo = new StudentRepository();

            InitializeForm();
            LoadDropdowns();
            if (_isEdit) LoadStudentData();
        }

        private void InitializeForm()
        {
            this.Text = _isEdit ? "Edit Student" : "Add Student";
            this.Size = new Size(460, 480);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            int left = 25;
            int width = 390;

            // ── Active Period Banner ──────────────────────
            var period = _studentRepo.GetActivePeriod();
            lblActivePeriod = new Label
            {
                Text = period.schoolYearId > 0
                            ? $"Enrolling to: {period.schoolYearLabel} — {period.semesterName}"
                            : "⚠ No active school year or semester!",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = period.schoolYearId > 0
                            ? Color.FromArgb(95, 75, 180)
                            : Color.FromArgb(231, 76, 60),
                Location = new Point(0, 0),
                Size = new Size(460, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            this.Controls.Add(lblActivePeriod);

            // ── Fields ────────────────────────────────────
            AddLabel("Student ID", left, 45);
            txtStudentId = AddTextBox(left, 65, width);

            AddLabel("Full Name", left, 105);
            txtStudentName = AddTextBox(left, 125, width);

            AddLabel("Course", left, 165);
            cboCourse = AddComboBox(left, 185, width);

            AddLabel("Year Level", left, 225);
            cboYearLevel = AddComboBox(left, 245, width);

            AddLabel("Contact No", left, 285);
            txtContactNo = AddTextBox(left, 305, width);

            AddLabel("Email (optional)", left, 345);
            txtEmail = AddTextBox(left, 365, width);

            // ── Buttons ───────────────────────────────────
            btnSave = new Button
            {
                Text = _isEdit ? "Update" : "Save & Enroll",
                Location = new Point(200, 410),
                Size = new Size(110, 35),
                BackColor = Color.FromArgb(95, 75, 180),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(320, 410),
                Size = new Size(95, 35),
                BackColor = Color.FromArgb(200, 200, 200),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10f),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => this.Close();

            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);
        }

        private void LoadDropdowns()
        {
            // Course
            var courses = _studentRepo.GetCourses();
            cboCourse.DataSource = courses;
            cboCourse.DisplayMember = "display_name";
            cboCourse.ValueMember = "id";

            // Year Level
            var yearLevels = _studentRepo.GetYearLevels();
            cboYearLevel.DataSource = yearLevels;
            cboYearLevel.DisplayMember = "display_name";
            cboYearLevel.ValueMember = "id";
        }

        private void LoadStudentData()
        {
            // Load from DB via enrollment id
            var data = _studentRepo.GetStudentsPaged("", 1, 1000, out _);

            foreach (DataRow row in data.Rows)
            {
                if (Convert.ToInt32(row["id"]) == _studentDbId)
                {
                    txtStudentId.Text = row["student_code"].ToString();
                    txtStudentName.Text = row["student_name"].ToString();
                    txtContactNo.Text = row["contact_no"].ToString();
                    cboCourse.SelectedValue = Convert.ToInt32(row["course_id"]);
                    cboYearLevel.SelectedValue = Convert.ToInt32(row["year_level_id"]);
                    break;
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // ── Validate all fields ───────────────────────
            if (string.IsNullOrWhiteSpace(txtStudentId.Text))
            { MessageHelper.ShowWarning("Student ID is required."); return; }

            if (string.IsNullOrWhiteSpace(txtStudentName.Text))
            { MessageHelper.ShowWarning("Full Name is required."); return; }

            if (cboCourse.SelectedValue == null)
            { MessageHelper.ShowWarning("Please select a course."); return; }

            if (cboYearLevel.SelectedValue == null)
            { MessageHelper.ShowWarning("Please select a year level."); return; }

            if (string.IsNullOrWhiteSpace(txtContactNo.Text))
            { MessageHelper.ShowWarning("Contact number is required."); return; }

            int courseId = Convert.ToInt32(cboCourse.SelectedValue);
            int yearLevelId = Convert.ToInt32(cboYearLevel.SelectedValue);

            try
            {
                if (_isEdit)
                {
                    // ── Edit mode ─────────────────────────
                    var student = new Student
                    {
                        Id = _studentDbId,
                        StudentId = txtStudentId.Text.Trim(),
                        StudentName = txtStudentName.Text.Trim(),
                        ContactNo = txtContactNo.Text.Trim(),
                        Email = txtEmail.Text.Trim()
                    };

                    _studentRepo.UpdateStudent(student, courseId, yearLevelId, _enrollmentId);
                    MessageHelper.ShowSuccess("Student updated successfully!");
                }
                else
                {
                    // ── Add mode — validate duplicates first ──
                    string studentCode = txtStudentId.Text.Trim();
                    var existingStudent = _studentRepo.GetStudentByCode(studentCode);

                    if (existingStudent != null)
                    {
                        // Student exists — check if already enrolled this period
                        bool alreadyEnrolled = _studentRepo.IsAlreadyEnrolled(existingStudent.Id);

                        if (alreadyEnrolled)
                        {
                            MessageHelper.ShowWarning(
                                $"Student '{studentCode}' is already enrolled in the active school year and semester.");
                            return;
                        }

                        // Student exists but not yet enrolled this period — just enroll
                        var confirm = MessageHelper.ShowConfirm(
                            $"Student '{existingStudent.StudentName}' already exists.\n" +
                            $"Do you want to enroll them in the current semester?");

                        if (confirm != DialogResult.Yes) return;

                        _studentRepo.EnrollExistingStudent(existingStudent.Id, courseId, yearLevelId);
                        MessageHelper.ShowSuccess("Student enrolled successfully!");
                    }
                    else
                    {
                        // Brand new student — add and enroll
                        var student = new Student
                        {
                            StudentId = studentCode,
                            StudentName = txtStudentName.Text.Trim(),
                            ContactNo = txtContactNo.Text.Trim(),
                            Email = txtEmail.Text.Trim()
                        };

                        _studentRepo.AddStudentAndEnroll(student, courseId, yearLevelId);
                        MessageHelper.ShowSuccess("Student added and enrolled successfully!");
                    }
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Failed to save: " + ex.Message);
            }
        }

        // ── UI Helpers ────────────────────────────────────
        private void AddLabel(string text, int x, int y)
        {
            this.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.Gray
            });
        }

        private TextBox AddTextBox(int x, int y, int width)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 30),
                Font = new Font("Segoe UI", 10f),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txt);
            return txt;
        }

        private ComboBox AddComboBox(int x, int y, int width)
        {
            var cbo = new ComboBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 30),
                Font = new Font("Segoe UI", 10f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            this.Controls.Add(cbo);
            return cbo;
        }
    }
}