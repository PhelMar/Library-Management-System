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
        private ComboBox cboCourse, cboYearLevel, cboSchoolYear, cboSemester;
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
            this.Size = new Size(460, 680);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            int left = 25;
            int width = 390;

            var period = _studentRepo.GetActivePeriod();
            lblActivePeriod = new Label
            {
                Text = period.schoolYearId > 0
                    ? $"Resolved Active: {period.schoolYearLabel} — {period.semesterName}"
                    : "⚠ No active school year found!",
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

            AddLabel("Student ID", left, 45);
            txtStudentId = AddTextBox(left, 65, width);

            if (!_isEdit)
                txtStudentId.Leave += TxtStudentId_Leave;
            else
            {
                txtStudentId.ReadOnly = true;
                txtStudentId.BackColor = Color.FromArgb(240, 240, 240);
            }

            AddLabel("Full Name", left, 105);
            txtStudentName = AddTextBox(left, 125, width);

            AddLabel("Course", left, 165);
            cboCourse = AddComboBox(left, 185, width);

            AddLabel("Year Level", left, 225);
            cboYearLevel = AddComboBox(left, 245, width);

            AddLabel("School Year", left, 285);
            cboSchoolYear = AddComboBox(left, 305, width);

            AddLabel("Semester", left, 345);
            cboSemester = AddComboBox(left, 365, width);

            AddLabel("Contact No", left, 405);
            txtContactNo = AddTextBox(left, 425, width);

            AddLabel("Email (optional)", left, 465);
            txtEmail = AddTextBox(left, 485, width);

            btnSave = new Button
            {
                Text = _isEdit ? "Update" : "Save & Enroll",
                Location = new Point(195, 580),
                Size = new Size(125, 38),
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
                Location = new Point(330, 580),
                Size = new Size(90, 38),
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

        private void TxtStudentId_Leave(object sender, EventArgs e)
        {
            string code = txtStudentId.Text.Trim();
            if (string.IsNullOrWhiteSpace(code)) return;

            var existing = _studentRepo.GetStudentByCode(code);
            if (existing == null) return;

            txtStudentName.Text = existing.StudentName;
            txtContactNo.Text = existing.ContactNo;
            txtEmail.Text = existing.Email;

            txtStudentName.ReadOnly = true;
            txtStudentName.BackColor = Color.FromArgb(240, 240, 240);
            txtContactNo.ReadOnly = true;
            txtContactNo.BackColor = Color.FromArgb(240, 240, 240);
            txtEmail.ReadOnly = true;
            txtEmail.BackColor = Color.FromArgb(240, 240, 240);

            MessageHelper.ShowWarning(
                $"Student \"{existing.StudentName}\" already exists.\n\nPersonal info pre-filled. You can still change Course, Year Level, School Year and Semester.");
        }

        private void LoadDropdowns()
        {
            var courses = _studentRepo.GetCourses();
            cboCourse.DataSource = courses;
            cboCourse.DisplayMember = "display_name";
            cboCourse.ValueMember = "id";

            var yearLevels = _studentRepo.GetYearLevels();
            cboYearLevel.DataSource = yearLevels;
            cboYearLevel.DisplayMember = "display_name";
            cboYearLevel.ValueMember = "id";

            var schoolYears = _studentRepo.GetSchoolYears();
            cboSchoolYear.DataSource = schoolYears;
            cboSchoolYear.DisplayMember = "display_name";
            cboSchoolYear.ValueMember = "id";

            var semesters = _studentRepo.GetSemesters();
            cboSemester.DataSource = semesters;
            cboSemester.DisplayMember = "display_name";
            cboSemester.ValueMember = "id";

            // Auto-select resolved active period
            var period = _studentRepo.GetActivePeriod();
            if (period.schoolYearId > 0)
                cboSchoolYear.SelectedValue = period.schoolYearId;
            if (period.semesterId > 0)
                cboSemester.SelectedValue = period.semesterId;
        }

        private void LoadStudentData()
        {
            var student = _studentRepo.GetStudentRawById(_studentDbId);
            if (student == null) return;

            txtStudentId.Text = student["student_id"].ToString();
            txtStudentName.Text = student["student_name"].ToString();
            txtContactNo.Text = student["contact_no"] == DBNull.Value ? "" : student["contact_no"].ToString();
            txtEmail.Text = student["email"] == DBNull.Value ? "" : student["email"].ToString();

            var enrollment = _studentRepo.GetEnrollmentById(_enrollmentId);
            if (enrollment != null)
            {
                cboCourse.SelectedValue = Convert.ToInt32(enrollment["course_id"]);
                cboYearLevel.SelectedValue = Convert.ToInt32(enrollment["year_level_id"]);
                cboSchoolYear.SelectedValue = Convert.ToInt32(enrollment["school_year_id"]);
                cboSemester.SelectedValue = Convert.ToInt32(enrollment["semester_id"]);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtStudentId.Text))
            { MessageHelper.ShowWarning("Student ID is required."); return; }

            if (string.IsNullOrWhiteSpace(txtStudentName.Text))
            { MessageHelper.ShowWarning("Full Name is required."); return; }

            if (cboCourse.SelectedValue == null)
            { MessageHelper.ShowWarning("Please select a course."); return; }

            if (cboYearLevel.SelectedValue == null)
            { MessageHelper.ShowWarning("Please select a year level."); return; }

            if (cboSchoolYear.SelectedValue == null)
            { MessageHelper.ShowWarning("Please select a school year."); return; }

            if (cboSemester.SelectedValue == null)
            { MessageHelper.ShowWarning("Please select a semester."); return; }

            if (string.IsNullOrWhiteSpace(txtContactNo.Text))
            { MessageHelper.ShowWarning("Contact number is required."); return; }

            int courseId = Convert.ToInt32(cboCourse.SelectedValue);
            int yearLevelId = Convert.ToInt32(cboYearLevel.SelectedValue);
            int schoolYearId = Convert.ToInt32(cboSchoolYear.SelectedValue);
            int semesterId = Convert.ToInt32(cboSemester.SelectedValue);

            try
            {
                if (_isEdit)
                {
                    var student = new Student
                    {
                        Id = _studentDbId,
                        StudentId = txtStudentId.Text.Trim(),
                        StudentName = txtStudentName.Text.Trim(),
                        ContactNo = txtContactNo.Text.Trim(),
                        Email = txtEmail.Text.Trim()
                    };

                    _studentRepo.UpdateStudent(student, courseId, yearLevelId, schoolYearId, semesterId, _enrollmentId);
                    MessageHelper.ShowSuccess("Student updated successfully.");
                }
                else
                {
                    string studentCode = txtStudentId.Text.Trim();
                    var existingStudent = _studentRepo.GetStudentByCode(studentCode);

                    if (existingStudent != null)
                    {
                        bool alreadyEnrolled = _studentRepo.IsAlreadyEnrolled(existingStudent.Id, schoolYearId, semesterId);

                        if (alreadyEnrolled)
                        {
                            MessageHelper.ShowWarning(
                                $"Student \"{existingStudent.StudentName}\" is already enrolled in the selected school year and semester.");
                            return;
                        }

                        var confirm = MessageHelper.ShowConfirm(
                            $"Enroll \"{existingStudent.StudentName}\" in the selected school year and semester?");

                        if (confirm != DialogResult.Yes) return;

                        _studentRepo.EnrollExistingStudent(existingStudent.Id, courseId, yearLevelId, schoolYearId, semesterId);
                        MessageHelper.ShowSuccess("Student enrolled successfully.");
                    }
                    else
                    {
                        var student = new Student
                        {
                            StudentId = studentCode,
                            StudentName = txtStudentName.Text.Trim(),
                            ContactNo = txtContactNo.Text.Trim(),
                            Email = txtEmail.Text.Trim()
                        };

                        _studentRepo.AddStudentAndEnroll(student, courseId, yearLevelId, schoolYearId, semesterId);
                        MessageHelper.ShowSuccess("Student added and enrolled successfully.");
                    }
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageHelper.ShowError("Unable to save. Please try again.\n\nDetails: " + ex.Message);
            }
        }

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