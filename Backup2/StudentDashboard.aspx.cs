using System;
using System.Data;
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
                LoadRecommendations();
                LoadStudentNotifications();
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

        private void LoadRecommendations()
        {
            try
            {
                string memberId = Session["MemberID"]?.ToString();
                if (string.IsNullOrEmpty(memberId)) return;

                // Recommendation logic based on borrowing history
                string query = @"
                    SELECT TOP 5 DISTINCT b.ISBN, b.Title, b.Author, b.Category, 
                           b.AvailableCopies, COUNT(t.BorrowID) as Popularity
                    FROM tblBooks b
                    INNER JOIN tblTransactions t ON b.ISBN = t.ISBN
                    WHERE t.MemberID IN (
                        SELECT DISTINCT MemberID 
                        FROM tblTransactions 
                        WHERE ISBN IN (
                            SELECT ISBN FROM tblTransactions 
                            WHERE MemberID = @MemberID
                        )
                    )
                    AND b.ISBN NOT IN (
                        SELECT ISBN FROM tblTransactions 
                        WHERE MemberID = @MemberID
                    )
                    AND b.AvailableCopies > 0
                    GROUP BY b.ISBN, b.Title, b.Author, b.Category, b.AvailableCopies
                    ORDER BY Popularity DESC";

                DataTable dt = DatabaseHelper.ExecuteQuery(query, 
                    new System.Data.SqlClient.SqlParameter[] { new System.Data.SqlClient.SqlParameter("@MemberID", memberId) });

                if (dt.Rows.Count > 0)
                {
                    gvRecommendations.DataSource = dt;
                    gvRecommendations.DataBind();
                    gvRecommendations.Visible = true;
                    noRecommendations.Visible = false;
                }
                else
                {
                    gvRecommendations.Visible = false;
                    noRecommendations.Visible = true;
                }
            }
            catch (Exception ex)
            {
                // Handle error - hide recommendations if there's an issue
                gvRecommendations.Visible = false;
                noRecommendations.Visible = true;
            }
        }

        private void LoadStudentNotifications()
        {
            try
            {
                string memberEmail = Session["Email"]?.ToString();
                if (string.IsNullOrEmpty(memberEmail)) return;

                // Get student's notifications
                string query = @"
                    SELECT TOP 5 Subject, Message, CreatedAt, Status
                    FROM tblNotifications 
                    WHERE Recipient = @Email
                    AND CreatedAt >= DATEADD(day, -7, GETDATE())
                    ORDER BY CreatedAt DESC";

                System.Data.SqlClient.SqlParameter[] parameters = new System.Data.SqlClient.SqlParameter[]
                {
                    new System.Data.SqlClient.SqlParameter("@Email", memberEmail)
                };

                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);

                if (dt.Rows.Count > 0)
                {
                    // Update notification badge
                    studentNotificationBadge.Text = dt.Rows.Count.ToString();
                    studentNotificationList.Visible = true;
                    noStudentNotifications.Visible = false;
                    
                    // Build notification list
                    string notificationHtml = "";
                    foreach (DataRow row in dt.Rows)
                    {
                        string createdAt = Convert.ToDateTime(row["CreatedAt"]).ToString("MMM dd");
                        string subject = row["Subject"].ToString();
                        string message = row["Message"].ToString();
                        
                        notificationHtml += $@"
                            <li><a class='dropdown-item'>
                                <small class='text-muted'>{createdAt}</small><br>
                                <strong>{subject}</strong><br>
                                <small>{message}</small>
                            </a></li>";
                    }
                    studentNotificationList.InnerHtml = notificationHtml;
                }
                else
                {
                    studentNotificationBadge.Text = "0";
                    studentNotificationList.Visible = false;
                    noStudentNotifications.Visible = true;
                }
            }
            catch (Exception ex)
            {
                // Handle error - hide notifications if there's an issue
                studentNotificationBadge.Text = "0";
                studentNotificationList.Visible = false;
                noStudentNotifications.Visible = true;
            }
        }
    }
}
