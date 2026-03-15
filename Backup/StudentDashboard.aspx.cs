using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using prjLibrarySystem.Models;

namespace prjLibrarySystem
{
    public partial class StudentDashboard : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Check if user is logged in
            if (Session["Username"] == null || Session["UserRole"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }

            // Check if user is student
            if (Session["UserRole"].ToString() != "Student")
            {
                Response.Redirect("Login.aspx");
                return;
            }

            lblStudentName.Text = Session["Username"].ToString();

            if (!IsPostBack)
            {
                LoadStudentStatistics();
            }
        }

        private void LoadStudentStatistics()
        {
            // Demo data - in real app, this would query database
            lblAvailableBooks.Text = "8";
            lblBorrowedBooks.Text = "2";
            lblOverdueBooks.Text = "0";
            lblTotalBorrowed.Text = "5";
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            LoadStudentStatistics();
        }

        protected void btnChangePassword_Click(object sender, EventArgs e)
        {
            string currentPassword = txtCurrentPassword.Text.Trim();
            string newPassword = txtNewPassword.Text.Trim();
            string confirmPassword = txtConfirmPassword.Text.Trim();

            // Reset error and success messages
            HidePasswordMessages();

            // Validation
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ShowPasswordError("All fields are required.");
                KeepModalOpen();
                return;
            }

            if (newPassword.Length < 6)
            {
                ShowPasswordError("New password must be at least 6 characters long.");
                KeepModalOpen();
                return;
            }

            if (newPassword != confirmPassword)
            {
                ShowPasswordError("New password and confirmation do not match.");
                KeepModalOpen();
                return;
            }

            if (currentPassword == newPassword)
            {
                ShowPasswordError("New password must be different from current password.");
                KeepModalOpen();
                return;
            }

            try
            {
                string userId = Session["UserID"].ToString();
                bool success = DatabaseHelper.ChangePassword(userId, currentPassword, newPassword);

                if (success)
                {
                    ShowPasswordSuccess("Password changed successfully!");
                    // Clear form fields
                    txtCurrentPassword.Text = "";
                    txtNewPassword.Text = "";
                    txtConfirmPassword.Text = "";
                    KeepModalOpen();
                }
                else
                {
                    ShowPasswordError("Current password is incorrect.");
                    KeepModalOpen();
                }
            }
            catch (Exception ex)
            {
                ShowPasswordError("An error occurred while changing password: " + ex.Message);
                KeepModalOpen();
            }
        }

        private void KeepModalOpen()
        {
            ScriptManager.RegisterStartupScript(this, GetType(), "keepModalOpen", 
                "setTimeout(function() { var modal = new bootstrap.Modal(document.getElementById('changePasswordModal')); modal.show(); }, 100);", true);
        }

        private void ShowPasswordError(string message)
        {
            lblPasswordError.Text = message;
            passwordError.Style["display"] = "block";
            passwordSuccess.Style["display"] = "none";
        }

        private void ShowPasswordSuccess(string message)
        {
            lblPasswordSuccess.Text = message;
            passwordSuccess.Style["display"] = "block";
            passwordError.Style["display"] = "none";
        }

        private void HidePasswordMessages()
        {
            passwordError.Style["display"] = "none";
            passwordSuccess.Style["display"] = "none";
        }
    }
}
