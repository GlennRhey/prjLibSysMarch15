using System;
using System.Data;
using System.Web.UI;
using prjLibrarySystem.Models;

namespace prjLibrarySystem
{
    public partial class Notifications : System.Web.UI.Page
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

            if (!IsPostBack)
            {
                LoadNotifications();
            }
        }

        private void LoadNotifications()
        {
            try
            {
                // Get all notifications
                string query = @"
                    SELECT NotificationID, NotificationType, Recipient, Subject, Message, Status, CreatedAt
                    FROM tblNotifications 
                    ORDER BY CreatedAt DESC";

                System.Data.SqlClient.SqlParameter[] parameters = new System.Data.SqlClient.SqlParameter[0];
                DataTable dt = DatabaseHelper.ExecuteQuery(query, parameters);
                
                gvNotifications.DataSource = dt;
                gvNotifications.DataBind();
            }
            catch (Exception ex)
            {
                // Handle error
                gvNotifications.DataSource = null;
                gvNotifications.DataBind();
            }
        }
    }
}
