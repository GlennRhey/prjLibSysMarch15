using System;
using System.Data;
using System.Data.SqlClient;
using System.Web.UI;
using prjLibrarySystem.Models;

namespace prjLibrarySystem
{
    public partial class AdminDashboard : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (Session["UserID"] == null && Session["Username"] == null)
            {
                Response.Redirect("Login.aspx");
                return;
            }

            if (Session["UserRole"]?.ToString() != "Admin")
            {
                Response.Redirect("StudentDashboard.aspx");
                return;
            }

            lblAdminName.Text = "Welcome, " + (Session["FullName"] ?? Session["UserID"] ?? Session["Username"]).ToString();

            if (!IsPostBack)
            {
                LoadDashboardStatistics();
                LoadRecentLoans();
                LoadNotifications();
            }
            LoadPopularBooks();
        }

        private void LoadDashboardStatistics()
        {
            try
            {
                System.Data.SqlClient.SqlParameter[] emptyParams = new System.Data.SqlClient.SqlParameter[0];
                
                lblTotalBooks.Text = DatabaseHelper.ExecuteQuery(
                    "SELECT COUNT(*) FROM tblBooks", emptyParams).Rows[0][0].ToString();

                lblTotalMembers.Text = DatabaseHelper.ExecuteQuery(@"
                    SELECT COUNT(*) FROM tblMembers m
                    INNER JOIN tblUsers u ON m.UserID = u.UserID
                    WHERE u.IsActive = 1", emptyParams).Rows[0][0].ToString();

                lblActiveLoans.Text = DatabaseHelper.ExecuteQuery(
                    "SELECT COUNT(*) FROM tblTransactions WHERE Status = 'Borrowed'",
                    emptyParams).Rows[0][0].ToString();

                lblOverdueBooks.Text = DatabaseHelper.ExecuteQuery(
                    "SELECT COUNT(*) FROM tblTransactions WHERE Status = 'Borrowed' AND DueDate < GETDATE()",
                    emptyParams).Rows[0][0].ToString();
            }
            catch
            {
                lblTotalBooks.Text = lblTotalMembers.Text = lblActiveLoans.Text = lblOverdueBooks.Text = "N/A";
            }
        }

        private void LoadRecentLoans()
        {
            try
            {
                System.Data.SqlClient.SqlParameter[] emptyParams = new System.Data.SqlClient.SqlParameter[0];
                
                DataTable dt = DatabaseHelper.ExecuteQuery(@"
                    SELECT TOP 10
                        b.Title    AS BookTitle,
                        m.FullName AS MemberName,
                        t.BorrowDate AS LoanDate
                    FROM tblTransactions t
                    INNER JOIN tblMembers m ON t.MemberID = m.MemberID
                    INNER JOIN tblBooks   b ON t.ISBN = b.ISBN
                    WHERE t.Status = 'Borrowed'
                    ORDER BY t.BorrowDate DESC",
                    emptyParams);

                gvRecentLoans.DataSource = dt;
                gvRecentLoans.DataBind();
            }
            catch { gvRecentLoans.DataSource = null; gvRecentLoans.DataBind(); }
        }

        private void LoadPopularBooks()
        {
            try
            {
                System.Data.SqlClient.SqlParameter[] emptyParams = new System.Data.SqlClient.SqlParameter[0];
                
                DataTable dt = DatabaseHelper.ExecuteQuery(@"
                    SELECT TOP 10
                        b.Title, b.Author,
                        COUNT(t.BorrowID) AS LoanCount
                    FROM tblBooks b
                    LEFT JOIN tblTransactions t
                        ON b.ISBN = t.ISBN
                       AND t.RequestType = 'Borrow' AND t.RequestStatus = 'Accepted'
                    GROUP BY b.ISBN, b.Title, b.Author
                    ORDER BY LoanCount DESC",
                    emptyParams);

                gvPopularBooks.DataSource = dt;
                gvPopularBooks.DataBind();
            }
            catch { gvPopularBooks.DataSource = null; gvPopularBooks.DataBind(); }
        }

        private void LoadNotifications()
        {
            try
            {
                // Get recent notifications
                string query = @"
                    SELECT TOP 10 Subject, Recipient, CreatedAt, Status
                    FROM tblNotifications 
                    ORDER BY CreatedAt DESC";

                // Create parameters array (even if empty)
                System.Data.SqlClient.SqlParameter[] parameters = new System.Data.SqlClient.SqlParameter[0];
                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);
                gvNotifications.DataSource = dt;
                gvNotifications.DataBind();
            }
            catch (Exception ex)
            {
                // Handle error - hide notifications if there's an issue
                gvNotifications.DataSource = null;
                gvNotifications.DataBind();
            }
        }

        protected void btnSendReminders_Click(object sender, EventArgs e)
        {
            try
            {
                DatabaseHelper.SendDueDateReminders();
                
                // Show success message (you can add a label for this)
                LoadNotifications(); // Refresh the notifications list
            }
            catch (Exception ex)
            {
                // Handle error
            }
        }
    }
}
