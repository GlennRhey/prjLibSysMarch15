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
                LoadPopularBooks();
            }
        }

        private void LoadDashboardStatistics()
        {
            try
            {
                lblTotalBooks.Text = DatabaseHelper.ExecuteQuery(
                    "SELECT COUNT(*) FROM tblBooks", new SqlParameter[0]).Rows[0][0].ToString();

                lblTotalMembers.Text = DatabaseHelper.ExecuteQuery(@"
                    SELECT COUNT(*) FROM tblMembers m
                    INNER JOIN tblUsers u ON m.UserID = u.UserID
                    WHERE u.IsActive = 1", new SqlParameter[0]).Rows[0][0].ToString();

                lblActiveLoans.Text = DatabaseHelper.ExecuteQuery(
                    "SELECT COUNT(*) FROM tblTransactions WHERE Status = 'Borrowed'",
                    new SqlParameter[0]).Rows[0][0].ToString();

                lblOverdueBooks.Text = DatabaseHelper.ExecuteQuery(
                    "SELECT COUNT(*) FROM tblTransactions WHERE Status = 'Borrowed' AND DueDate < GETDATE()",
                    new SqlParameter[0]).Rows[0][0].ToString();
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
                    new SqlParameter[0]);

                gvRecentLoans.DataSource = dt;
                gvRecentLoans.DataBind();
            }
            catch { gvRecentLoans.DataSource = null; gvRecentLoans.DataBind(); }
        }

        private void LoadPopularBooks()
        {
            try
            {
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
                    new SqlParameter[0]);

                gvPopularBooks.DataSource = dt;
                gvPopularBooks.DataBind();
            }
            catch { gvPopularBooks.DataSource = null; gvPopularBooks.DataBind(); }
        }
    }
}
