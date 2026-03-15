using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;

namespace prjLibrarySystem.Models
{
    public class DatabaseHelper
    {
        private static string ConnectionString =
            ConfigurationManager.ConnectionStrings["LibraryDB"]?.ConnectionString ??
            "Data Source=MSI\\SQLEXPRESS;Initial Catalog=dbLibrarySystem;Integrated Security=True";

        // ── Password hashing ──────────────────────────────────────────────────
        // SHA256, UTF-8 encoding, lowercase hex output
        // All password comparisons and updates go through this method.

        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // ── Core helpers ──────────────────────────────────────────────────────

        public static DataTable ExecuteQuery(string query, SqlParameter[] parameters = null)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(query, connection))
            {
                if (parameters != null) command.Parameters.AddRange(parameters);
                using (var adapter = new SqlDataAdapter(command))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public static int ExecuteNonQuery(string query, SqlParameter[] parameters = null)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(query, connection))
            {
                if (parameters != null) command.Parameters.AddRange(parameters);
                connection.Open();
                return command.ExecuteNonQuery();
            }
        }

        public static object ExecuteScalar(string query, SqlParameter[] parameters = null)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(query, connection))
            {
                if (parameters != null) command.Parameters.AddRange(parameters);
                connection.Open();
                return command.ExecuteScalar();
            }
        }

        public static DataTable ExecuteStoredProcedure(string procedureName, SqlParameter[] parameters = null)
        {
            using (var connection = new SqlConnection(ConnectionString))
            using (var command = new SqlCommand(procedureName, connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                if (parameters != null) command.Parameters.AddRange(parameters);
                using (var adapter = new SqlDataAdapter(command))
                {
                    var dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public static bool TestConnection()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    return true;
                }
            }
            catch { return false; }
        }

        // ── Authentication ─────────────────────────────────────────────────────
        // Accepts plain text password — hashes internally before DB comparison.

        public static User AuthenticateUser(string userId, string plainTextPassword)
        {
            string query = @"
                SELECT UserID, Role, FullName, Email, IsActive
                FROM   tblUsers
                WHERE  UserID       = @UserID
                  AND  PasswordHash = @PasswordHash
                  AND  IsActive     = 1";

            DataTable dt = ExecuteQuery(query, new SqlParameter[]
            {
                new SqlParameter("@UserID",       userId),
                new SqlParameter("@PasswordHash", HashPassword(plainTextPassword))
            });

            if (dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            return new User
            {
                UserID = row["UserID"].ToString(),
                Role = row["Role"].ToString(),
                FullName = row["FullName"]?.ToString() ?? "",
                Email = row["Email"]?.ToString() ?? "",
                IsActive = true
            };
        }

        // Authenticate student — returns full row including MemberID
        public static DataRow AuthenticateStudent(string userId, string plainTextPassword)
        {
            string query = @"
                SELECT u.UserID, u.Role, u.FullName, u.Email, u.IsActive,
                       m.MemberID, m.Course, m.YearLevel
                FROM   tblUsers   u
                INNER JOIN tblMembers m ON m.UserID = u.UserID
                WHERE  u.UserID       = @UserID
                  AND  u.PasswordHash = @PasswordHash
                  AND  u.IsActive     = 1
                  AND  u.Role         = 'Student'";

            DataTable dt = ExecuteQuery(query, new SqlParameter[]
            {
                new SqlParameter("@UserID",       userId),
                new SqlParameter("@PasswordHash", HashPassword(plainTextPassword))
            });

            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        // ── Change Password ────────────────────────────────────────────────────
        // Accepts plain text passwords — hashes internally.

        public static bool ChangePassword(string userId, string currentPlainText, string newPlainText)
        {
            try
            {
                int count = Convert.ToInt32(ExecuteScalar(
                    @"SELECT COUNT(*) FROM tblUsers
                      WHERE UserID = @UserID AND PasswordHash = @Current AND IsActive = 1",
                    new SqlParameter[]
                    {
                        new SqlParameter("@UserID",  userId),
                        new SqlParameter("@Current", HashPassword(currentPlainText))
                    }));

                if (count == 0) return false;

                ExecuteNonQuery(
                    "UPDATE tblUsers SET PasswordHash = @New WHERE UserID = @UserID",
                    new SqlParameter[]
                    {
                        new SqlParameter("@New",    HashPassword(newPlainText)),
                        new SqlParameter("@UserID", userId)
                    });

                return true;
            }
            catch { return false; }
        }
    }
}