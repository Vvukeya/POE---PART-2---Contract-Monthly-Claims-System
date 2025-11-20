using System.Data.SqlClient;
using POE___PART_2___Contract_Monthly_Claims_System.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace POE___PART_2___Contract_Monthly_Claims_System.Data
{
    public class AllQueries
    {
        private readonly DatabaseConnection _dbConnection;

        public AllQueries()
        {
            _dbConnection = new DatabaseConnection();
        }

        public bool TestDatabaseConnection()
        {
            return _dbConnection.TestConnection();
        }

        public string GetConnectionString()
        {
            return _dbConnection.ConnectionString;
        }

        // Helper method to check if a column exists
        private async Task<bool> ColumnExistsAsync(SqlConnection connection, string tableName, string columnName)
        {
            string query = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@ColumnName", columnName);
                return (int)await command.ExecuteScalarAsync() > 0;
            }
        }

        // User Queries
        public async Task<User> GetUserByCredentialsAsync(string email, string password, string role)
        {
            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Users WHERE Email = @Email AND Password = @Password AND Role = @Role";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    command.Parameters.AddWithValue("@Password", password);
                    command.Parameters.AddWithValue("@Role", role);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                UserID = reader.GetString(reader.GetOrdinal("UserID")),
                                Role = reader.GetString(reader.GetOrdinal("Role")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                Surname = reader.GetString(reader.GetOrdinal("Surname")),
                                Address = reader.GetString(reader.GetOrdinal("Address")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Password = reader.GetString(reader.GetOrdinal("Password")),
                                PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                RegistrationDate = reader.GetDateTime(reader.GetOrdinal("RegistrationDate"))
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task<bool> AddUserAsync(User user)
        {
            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();

                // Generate custom user ID based on role
                string userID = await GenerateCustomUserIDAsync(connection, user.Role);

                string query = @"INSERT INTO Users (UserID, Role, FirstName, Surname, Address, Email, Password, PhoneNumber, RegistrationDate) 
                               VALUES (@UserID, @Role, @FirstName, @Surname, @Address, @Email, @Password, @PhoneNumber, @RegistrationDate)";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userID);
                    command.Parameters.AddWithValue("@Role", user.Role);
                    command.Parameters.AddWithValue("@FirstName", user.FirstName);
                    command.Parameters.AddWithValue("@Surname", user.Surname);
                    command.Parameters.AddWithValue("@Address", user.Address);
                    command.Parameters.AddWithValue("@Email", user.Email);
                    command.Parameters.AddWithValue("@Password", user.Password);
                    command.Parameters.AddWithValue("@PhoneNumber", user.PhoneNumber);
                    command.Parameters.AddWithValue("@RegistrationDate", user.RegistrationDate);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        // Add this method to generate custom user IDs
        private async Task<string> GenerateCustomUserIDAsync(SqlConnection connection, string role)
        {
            string prefix = role switch
            {
                "Lecturer" => "L",
                "Coordinator" => "PC",
                "Manager" => "AM",
                "HR" => "HR",
                _ => "U"
            };

            // Get the next sequence number for this role
            string query = $@"
                SELECT COUNT(*) + 1 
                FROM Users 
                WHERE Role = @Role";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Role", role);
                var nextNumber = Convert.ToInt32(await command.ExecuteScalarAsync());

                return $"{prefix}{nextNumber:D3}"; // Format as L001, PC001, etc.
            }
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT COUNT(1) FROM Users WHERE Email = @Email";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    return (int)await command.ExecuteScalarAsync() > 0;
                }
            }
        }

        public async Task<User> GetUserByIdAsync(string userId)
        {
            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Users WHERE UserID = @UserID";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                UserID = reader.GetString(reader.GetOrdinal("UserID")),
                                Role = reader.GetString(reader.GetOrdinal("Role")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                Surname = reader.GetString(reader.GetOrdinal("Surname")),
                                Address = reader.GetString(reader.GetOrdinal("Address")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Password = reader.GetString(reader.GetOrdinal("Password")),
                                PhoneNumber = reader.GetString(reader.GetOrdinal("PhoneNumber")),
                                RegistrationDate = reader.GetDateTime(reader.GetOrdinal("RegistrationDate"))
                            };
                        }
                    }
                }
            }
            return null;
        }

        // Claim Queries
        public async Task<int> AddClaimAsync(Claim claim)
        {
            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();
                string query = @"INSERT INTO Claims (UserID, Month, Year, ModuleCode, Sessions, Hours, HourlyRate, 
                               Description, Status, CorrectionNotes, SubmissionDate) 
                               VALUES (@UserID, @Month, @Year, @ModuleCode, @Sessions, @Hours, @HourlyRate, 
                               @Description, @Status, @CorrectionNotes, @SubmissionDate);
                               SELECT SCOPE_IDENTITY();";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", claim.UserID);
                    command.Parameters.AddWithValue("@Month", claim.Month);
                    command.Parameters.AddWithValue("@Year", claim.Year);
                    command.Parameters.AddWithValue("@ModuleCode", claim.ModuleCode);
                    command.Parameters.AddWithValue("@Sessions", claim.Sessions);
                    command.Parameters.AddWithValue("@Hours", claim.Hours);
                    command.Parameters.AddWithValue("@HourlyRate", claim.HourlyRate);
                    command.Parameters.AddWithValue("@Description", claim.Description);
                    command.Parameters.AddWithValue("@Status", claim.Status);
                    command.Parameters.AddWithValue("@CorrectionNotes", claim.CorrectionNotes ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SubmissionDate", claim.SubmissionDate);

                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        public async Task<List<Claim>> GetClaimsByUserIdAsync(string userId)
        {
            var claims = new List<Claim>();

            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();

                // Check if new columns exist
                bool paymentDateExists = await ColumnExistsAsync(connection, "Claims", "PaymentDate");
                bool paidByExists = await ColumnExistsAsync(connection, "Claims", "PaidBy");
                bool verifiedByExists = await ColumnExistsAsync(connection, "Claims", "VerifiedBy");
                bool approvedByExists = await ColumnExistsAsync(connection, "Claims", "ApprovedBy");

                string query = @"SELECT c.*, u.UserID as User_UserID, u.FirstName, u.Surname, u.Email, u.Role 
                               FROM Claims c 
                               INNER JOIN Users u ON c.UserID = u.UserID 
                               WHERE c.UserID = @UserID 
                               ORDER BY c.SubmissionDate DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var hoursValue = reader.GetValue(reader.GetOrdinal("Hours"));
                            var hourlyRateValue = reader.GetValue(reader.GetOrdinal("HourlyRate"));

                            double hours = 0;
                            double hourlyRate = 0;

                            if (hoursValue != DBNull.Value)
                                hours = Convert.ToDouble(hoursValue);

                            if (hourlyRateValue != DBNull.Value)
                                hourlyRate = Convert.ToDouble(hourlyRateValue);

                            var claim = new Claim
                            {
                                ClaimID = reader.GetInt32(reader.GetOrdinal("ClaimID")),
                                UserID = reader.GetString(reader.GetOrdinal("UserID")),
                                Month = reader.GetInt32(reader.GetOrdinal("Month")),
                                Year = reader.GetInt32(reader.GetOrdinal("Year")),
                                ModuleCode = reader.GetString(reader.GetOrdinal("ModuleCode")),
                                Sessions = reader.GetInt32(reader.GetOrdinal("Sessions")),
                                Hours = hours,
                                HourlyRate = hourlyRate,
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                CorrectionNotes = reader.IsDBNull(reader.GetOrdinal("CorrectionNotes")) ?
                                    null : reader.GetString(reader.GetOrdinal("CorrectionNotes")),
                                SubmissionDate = reader.GetDateTime(reader.GetOrdinal("SubmissionDate")),
                                User = new User
                                {
                                    UserID = reader.GetString(reader.GetOrdinal("User_UserID")),
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    Surname = reader.GetString(reader.GetOrdinal("Surname")),
                                    Email = reader.GetString(reader.GetOrdinal("Email")),
                                    Role = reader.GetString(reader.GetOrdinal("Role"))
                                }
                            };

                            // Safely handle new columns
                            if (paymentDateExists)
                            {
                                claim.PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ?
                                    null : reader.GetDateTime(reader.GetOrdinal("PaymentDate"));
                            }

                            if (paidByExists)
                            {
                                claim.PaidBy = reader.IsDBNull(reader.GetOrdinal("PaidBy")) ?
                                    null : reader.GetString(reader.GetOrdinal("PaidBy"));
                            }

                            if (verifiedByExists)
                            {
                                claim.VerifiedBy = reader.IsDBNull(reader.GetOrdinal("VerifiedBy")) ?
                                    null : reader.GetString(reader.GetOrdinal("VerifiedBy"));
                            }

                            if (approvedByExists)
                            {
                                claim.ApprovedBy = reader.IsDBNull(reader.GetOrdinal("ApprovedBy")) ?
                                    null : reader.GetString(reader.GetOrdinal("ApprovedBy"));
                            }

                            claims.Add(claim);
                        }
                    }
                }
            }
            return claims;
        }

        // Get pending claims with user data
        public async Task<List<Claim>> GetPendingClaimsAsync()
        {
            var claims = new List<Claim>();

            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();

                // Check if new columns exist
                bool paymentDateExists = await ColumnExistsAsync(connection, "Claims", "PaymentDate");
                bool paidByExists = await ColumnExistsAsync(connection, "Claims", "PaidBy");
                bool verifiedByExists = await ColumnExistsAsync(connection, "Claims", "VerifiedBy");
                bool approvedByExists = await ColumnExistsAsync(connection, "Claims", "ApprovedBy");

                string query = @"SELECT c.*, u.UserID as User_UserID, u.FirstName, u.Surname, u.Email, u.Role 
                               FROM Claims c 
                               INNER JOIN Users u ON c.UserID = u.UserID 
                               WHERE c.Status IN ('Pending', 'Resubmitted') 
                               ORDER BY c.SubmissionDate";

                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var hoursValue = reader.GetValue(reader.GetOrdinal("Hours"));
                        var hourlyRateValue = reader.GetValue(reader.GetOrdinal("HourlyRate"));

                        double hours = 0;
                        double hourlyRate = 0;

                        if (hoursValue != DBNull.Value)
                            hours = Convert.ToDouble(hoursValue);

                        if (hourlyRateValue != DBNull.Value)
                            hourlyRate = Convert.ToDouble(hourlyRateValue);

                        var claim = new Claim
                        {
                            ClaimID = reader.GetInt32(reader.GetOrdinal("ClaimID")),
                            UserID = reader.GetString(reader.GetOrdinal("UserID")),
                            Month = reader.GetInt32(reader.GetOrdinal("Month")),
                            Year = reader.GetInt32(reader.GetOrdinal("Year")),
                            ModuleCode = reader.GetString(reader.GetOrdinal("ModuleCode")),
                            Sessions = reader.GetInt32(reader.GetOrdinal("Sessions")),
                            Hours = hours,
                            HourlyRate = hourlyRate,
                            Description = reader.GetString(reader.GetOrdinal("Description")),
                            Status = reader.GetString(reader.GetOrdinal("Status")),
                            CorrectionNotes = reader.IsDBNull(reader.GetOrdinal("CorrectionNotes")) ?
                                null : reader.GetString(reader.GetOrdinal("CorrectionNotes")),
                            SubmissionDate = reader.GetDateTime(reader.GetOrdinal("SubmissionDate")),
                            User = new User
                            {
                                UserID = reader.GetString(reader.GetOrdinal("User_UserID")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                Surname = reader.GetString(reader.GetOrdinal("Surname")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Role = reader.GetString(reader.GetOrdinal("Role"))
                            }
                        };

                        // Safely handle new columns
                        if (paymentDateExists)
                        {
                            claim.PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ?
                                null : reader.GetDateTime(reader.GetOrdinal("PaymentDate"));
                        }

                        if (paidByExists)
                        {
                            claim.PaidBy = reader.IsDBNull(reader.GetOrdinal("PaidBy")) ?
                                null : reader.GetString(reader.GetOrdinal("PaidBy"));
                        }

                        if (verifiedByExists)
                        {
                            claim.VerifiedBy = reader.IsDBNull(reader.GetOrdinal("VerifiedBy")) ?
                                null : reader.GetString(reader.GetOrdinal("VerifiedBy"));
                        }

                        if (approvedByExists)
                        {
                            claim.ApprovedBy = reader.IsDBNull(reader.GetOrdinal("ApprovedBy")) ?
                                null : reader.GetString(reader.GetOrdinal("ApprovedBy"));
                        }

                        claims.Add(claim);
                    }
                }
            }
            return claims;
        }

        // Get verified claims with user data
        public async Task<List<Claim>> GetVerifiedClaimsAsync()
        {
            var claims = new List<Claim>();

            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();

                // Check if new columns exist
                bool paymentDateExists = await ColumnExistsAsync(connection, "Claims", "PaymentDate");
                bool paidByExists = await ColumnExistsAsync(connection, "Claims", "PaidBy");
                bool verifiedByExists = await ColumnExistsAsync(connection, "Claims", "VerifiedBy");
                bool approvedByExists = await ColumnExistsAsync(connection, "Claims", "ApprovedBy");

                string query = @"SELECT c.*, u.UserID as User_UserID, u.FirstName, u.Surname, u.Email, u.Role 
                               FROM Claims c 
                               INNER JOIN Users u ON c.UserID = u.UserID 
                               WHERE c.Status = 'Verified' 
                               ORDER BY c.SubmissionDate";

                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var hoursValue = reader.GetValue(reader.GetOrdinal("Hours"));
                        var hourlyRateValue = reader.GetValue(reader.GetOrdinal("HourlyRate"));

                        double hours = 0;
                        double hourlyRate = 0;

                        if (hoursValue != DBNull.Value)
                            hours = Convert.ToDouble(hoursValue);

                        if (hourlyRateValue != DBNull.Value)
                            hourlyRate = Convert.ToDouble(hourlyRateValue);

                        var claim = new Claim
                        {
                            ClaimID = reader.GetInt32(reader.GetOrdinal("ClaimID")),
                            UserID = reader.GetString(reader.GetOrdinal("UserID")),
                            Month = reader.GetInt32(reader.GetOrdinal("Month")),
                            Year = reader.GetInt32(reader.GetOrdinal("Year")),
                            ModuleCode = reader.GetString(reader.GetOrdinal("ModuleCode")),
                            Sessions = reader.GetInt32(reader.GetOrdinal("Sessions")),
                            Hours = hours,
                            HourlyRate = hourlyRate,
                            Description = reader.GetString(reader.GetOrdinal("Description")),
                            Status = reader.GetString(reader.GetOrdinal("Status")),
                            CorrectionNotes = reader.IsDBNull(reader.GetOrdinal("CorrectionNotes")) ?
                                null : reader.GetString(reader.GetOrdinal("CorrectionNotes")),
                            SubmissionDate = reader.GetDateTime(reader.GetOrdinal("SubmissionDate")),
                            User = new User
                            {
                                UserID = reader.GetString(reader.GetOrdinal("User_UserID")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                Surname = reader.GetString(reader.GetOrdinal("Surname")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Role = reader.GetString(reader.GetOrdinal("Role"))
                            }
                        };

                        // Safely handle new columns
                        if (paymentDateExists)
                        {
                            claim.PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ?
                                null : reader.GetDateTime(reader.GetOrdinal("PaymentDate"));
                        }

                        if (paidByExists)
                        {
                            claim.PaidBy = reader.IsDBNull(reader.GetOrdinal("PaidBy")) ?
                                null : reader.GetString(reader.GetOrdinal("PaidBy"));
                        }

                        if (verifiedByExists)
                        {
                            claim.VerifiedBy = reader.IsDBNull(reader.GetOrdinal("VerifiedBy")) ?
                                null : reader.GetString(reader.GetOrdinal("VerifiedBy"));
                        }

                        if (approvedByExists)
                        {
                            claim.ApprovedBy = reader.IsDBNull(reader.GetOrdinal("ApprovedBy")) ?
                                null : reader.GetString(reader.GetOrdinal("ApprovedBy"));
                        }

                        claims.Add(claim);
                    }
                }
            }
            return claims;
        }

        // Get all claims with user data
        public async Task<List<Claim>> GetAllClaimsAsync()
        {
            var claims = new List<Claim>();

            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();

                // Check if new columns exist
                bool paymentDateExists = await ColumnExistsAsync(connection, "Claims", "PaymentDate");
                bool paidByExists = await ColumnExistsAsync(connection, "Claims", "PaidBy");
                bool verifiedByExists = await ColumnExistsAsync(connection, "Claims", "VerifiedBy");
                bool approvedByExists = await ColumnExistsAsync(connection, "Claims", "ApprovedBy");

                string query = @"SELECT c.*, u.UserID as User_UserID, u.FirstName, u.Surname, u.Email, u.Role 
                               FROM Claims c 
                               INNER JOIN Users u ON c.UserID = u.UserID 
                               ORDER BY c.SubmissionDate DESC";

                using (var command = new SqlCommand(query, connection))
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var hoursValue = reader.GetValue(reader.GetOrdinal("Hours"));
                        var hourlyRateValue = reader.GetValue(reader.GetOrdinal("HourlyRate"));

                        double hours = 0;
                        double hourlyRate = 0;

                        if (hoursValue != DBNull.Value)
                            hours = Convert.ToDouble(hoursValue);

                        if (hourlyRateValue != DBNull.Value)
                            hourlyRate = Convert.ToDouble(hourlyRateValue);

                        var claim = new Claim
                        {
                            ClaimID = reader.GetInt32(reader.GetOrdinal("ClaimID")),
                            UserID = reader.GetString(reader.GetOrdinal("UserID")),
                            Month = reader.GetInt32(reader.GetOrdinal("Month")),
                            Year = reader.GetInt32(reader.GetOrdinal("Year")),
                            ModuleCode = reader.GetString(reader.GetOrdinal("ModuleCode")),
                            Sessions = reader.GetInt32(reader.GetOrdinal("Sessions")),
                            Hours = hours,
                            HourlyRate = hourlyRate,
                            Description = reader.GetString(reader.GetOrdinal("Description")),
                            Status = reader.GetString(reader.GetOrdinal("Status")),
                            CorrectionNotes = reader.IsDBNull(reader.GetOrdinal("CorrectionNotes")) ?
                                null : reader.GetString(reader.GetOrdinal("CorrectionNotes")),
                            SubmissionDate = reader.GetDateTime(reader.GetOrdinal("SubmissionDate")),
                            User = new User
                            {
                                UserID = reader.GetString(reader.GetOrdinal("User_UserID")),
                                FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                Surname = reader.GetString(reader.GetOrdinal("Surname")),
                                Email = reader.GetString(reader.GetOrdinal("Email")),
                                Role = reader.GetString(reader.GetOrdinal("Role"))
                            }
                        };

                        // Safely handle new columns
                        if (paymentDateExists)
                        {
                            claim.PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ?
                                null : reader.GetDateTime(reader.GetOrdinal("PaymentDate"));
                        }

                        if (paidByExists)
                        {
                            claim.PaidBy = reader.IsDBNull(reader.GetOrdinal("PaidBy")) ?
                                null : reader.GetString(reader.GetOrdinal("PaidBy"));
                        }

                        if (verifiedByExists)
                        {
                            claim.VerifiedBy = reader.IsDBNull(reader.GetOrdinal("VerifiedBy")) ?
                                null : reader.GetString(reader.GetOrdinal("VerifiedBy"));
                        }

                        if (approvedByExists)
                        {
                            claim.ApprovedBy = reader.IsDBNull(reader.GetOrdinal("ApprovedBy")) ?
                                null : reader.GetString(reader.GetOrdinal("ApprovedBy"));
                        }

                        claims.Add(claim);
                    }
                }
            }
            return claims;
        }

        // Updated UpdateClaimStatusAsync method with verifiedBy and approvedBy parameters
        public async Task<bool> UpdateClaimStatusAsync(int claimId, string status, string correctionNotes = null, string verifiedBy = null, string approvedBy = null)
        {
            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();

                // Check if new columns exist
                bool verifiedByExists = await ColumnExistsAsync(connection, "Claims", "VerifiedBy");
                bool approvedByExists = await ColumnExistsAsync(connection, "Claims", "ApprovedBy");

                string query = @"UPDATE Claims SET Status = @Status, 
                               CorrectionNotes = @CorrectionNotes";

                if (verifiedByExists && verifiedBy != null)
                {
                    query += ", VerifiedBy = @VerifiedBy";
                }

                if (approvedByExists && approvedBy != null)
                {
                    query += ", ApprovedBy = @ApprovedBy";
                }

                query += " WHERE ClaimID = @ClaimID";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@CorrectionNotes", correctionNotes ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@ClaimID", claimId);

                    if (verifiedByExists && verifiedBy != null)
                    {
                        command.Parameters.AddWithValue("@VerifiedBy", verifiedBy);
                    }

                    if (approvedByExists && approvedBy != null)
                    {
                        command.Parameters.AddWithValue("@ApprovedBy", approvedBy);
                    }

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        // Keep the original method for backward compatibility
        public async Task<bool> UpdateClaimStatusAsync(int claimId, string status, string correctionNotes = null)
        {
            return await UpdateClaimStatusAsync(claimId, status, correctionNotes, null, null);
        }

        public async Task<bool> UpdateClaimPaymentStatusAsync(int claimId, string status, string paidBy)
        {
            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();
                string query = @"UPDATE Claims SET Status = @Status, PaymentDate = @PaymentDate, PaidBy = @PaidBy 
                       WHERE ClaimID = @ClaimID";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Status", status);
                    command.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
                    command.Parameters.AddWithValue("@PaidBy", paidBy);
                    command.Parameters.AddWithValue("@ClaimID", claimId);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<bool> UpdateClaimAsync(Claim claim)
        {
            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();
                string query = @"UPDATE Claims SET 
                               Month = @Month, Year = @Year, ModuleCode = @ModuleCode, 
                               Sessions = @Sessions, Hours = @Hours, HourlyRate = @HourlyRate,
                               Description = @Description, Status = @Status, 
                               CorrectionNotes = @CorrectionNotes, SubmissionDate = @SubmissionDate
                               WHERE ClaimID = @ClaimID";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Month", claim.Month);
                    command.Parameters.AddWithValue("@Year", claim.Year);
                    command.Parameters.AddWithValue("@ModuleCode", claim.ModuleCode);
                    command.Parameters.AddWithValue("@Sessions", claim.Sessions);
                    command.Parameters.AddWithValue("@Hours", claim.Hours);
                    command.Parameters.AddWithValue("@HourlyRate", claim.HourlyRate);
                    command.Parameters.AddWithValue("@Description", claim.Description);
                    command.Parameters.AddWithValue("@Status", claim.Status);
                    command.Parameters.AddWithValue("@CorrectionNotes", claim.CorrectionNotes ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@SubmissionDate", claim.SubmissionDate);
                    command.Parameters.AddWithValue("@ClaimID", claim.ClaimID);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<Claim> GetClaimByIdAsync(int claimId, string userId = null)
        {
            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();

                // Check if new columns exist
                bool paymentDateExists = await ColumnExistsAsync(connection, "Claims", "PaymentDate");
                bool paidByExists = await ColumnExistsAsync(connection, "Claims", "PaidBy");
                bool verifiedByExists = await ColumnExistsAsync(connection, "Claims", "VerifiedBy");
                bool approvedByExists = await ColumnExistsAsync(connection, "Claims", "ApprovedBy");

                string query = @"SELECT c.*, u.UserID as User_UserID, u.FirstName, u.Surname, u.Email, u.Role 
                               FROM Claims c 
                               INNER JOIN Users u ON c.UserID = u.UserID 
                               WHERE c.ClaimID = @ClaimID" +
                               (!string.IsNullOrEmpty(userId) ? " AND c.UserID = @UserID" : "");

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClaimID", claimId);
                    if (!string.IsNullOrEmpty(userId))
                        command.Parameters.AddWithValue("@UserID", userId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var hoursValue = reader.GetValue(reader.GetOrdinal("Hours"));
                            var hourlyRateValue = reader.GetValue(reader.GetOrdinal("HourlyRate"));

                            double hours = 0;
                            double hourlyRate = 0;

                            if (hoursValue != DBNull.Value)
                                hours = Convert.ToDouble(hoursValue);

                            if (hourlyRateValue != DBNull.Value)
                                hourlyRate = Convert.ToDouble(hourlyRateValue);

                            var claim = new Claim
                            {
                                ClaimID = reader.GetInt32(reader.GetOrdinal("ClaimID")),
                                UserID = reader.GetString(reader.GetOrdinal("UserID")),
                                Month = reader.GetInt32(reader.GetOrdinal("Month")),
                                Year = reader.GetInt32(reader.GetOrdinal("Year")),
                                ModuleCode = reader.GetString(reader.GetOrdinal("ModuleCode")),
                                Sessions = reader.GetInt32(reader.GetOrdinal("Sessions")),
                                Hours = hours,
                                HourlyRate = hourlyRate,
                                Description = reader.GetString(reader.GetOrdinal("Description")),
                                Status = reader.GetString(reader.GetOrdinal("Status")),
                                CorrectionNotes = reader.IsDBNull(reader.GetOrdinal("CorrectionNotes")) ?
                                    null : reader.GetString(reader.GetOrdinal("CorrectionNotes")),
                                SubmissionDate = reader.GetDateTime(reader.GetOrdinal("SubmissionDate")),
                                User = new User
                                {
                                    UserID = reader.GetString(reader.GetOrdinal("User_UserID")),
                                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                                    Surname = reader.GetString(reader.GetOrdinal("Surname")),
                                    Email = reader.GetString(reader.GetOrdinal("Email")),
                                    Role = reader.GetString(reader.GetOrdinal("Role"))
                                }
                            };

                            // Safely handle new columns
                            if (paymentDateExists)
                            {
                                claim.PaymentDate = reader.IsDBNull(reader.GetOrdinal("PaymentDate")) ?
                                    null : reader.GetDateTime(reader.GetOrdinal("PaymentDate"));
                            }

                            if (paidByExists)
                            {
                                claim.PaidBy = reader.IsDBNull(reader.GetOrdinal("PaidBy")) ?
                                    null : reader.GetString(reader.GetOrdinal("PaidBy"));
                            }

                            if (verifiedByExists)
                            {
                                claim.VerifiedBy = reader.IsDBNull(reader.GetOrdinal("VerifiedBy")) ?
                                    null : reader.GetString(reader.GetOrdinal("VerifiedBy"));
                            }

                            if (approvedByExists)
                            {
                                claim.ApprovedBy = reader.IsDBNull(reader.GetOrdinal("ApprovedBy")) ?
                                    null : reader.GetString(reader.GetOrdinal("ApprovedBy"));
                            }

                            return claim;
                        }
                    }
                }
            }
            return null;
        }

        // Statistics Queries
        public async Task<int> GetClaimsCountAsync(string userId = null, string status = null)
        {
            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT COUNT(*) FROM Claims WHERE 1=1";

                if (!string.IsNullOrEmpty(userId))
                    query += " AND UserID = @UserID";
                if (!string.IsNullOrEmpty(status))
                    query += " AND Status = @Status";

                using (var command = new SqlCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(userId))
                        command.Parameters.AddWithValue("@UserID", userId);
                    if (!string.IsNullOrEmpty(status))
                        command.Parameters.AddWithValue("@Status", status);

                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        // Document Queries
        public async Task<int> AddDocumentAsync(Document document)
        {
            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();
                string query = @"INSERT INTO Documents (ClaimID, FileName, FileType, FilePath, FileSize, UploadDate) 
                               VALUES (@ClaimID, @FileName, @FileType, @FilePath, @FileSize, @UploadDate);
                               SELECT SCOPE_IDENTITY();";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClaimID", document.ClaimID);
                    command.Parameters.AddWithValue("@FileName", document.FileName);
                    command.Parameters.AddWithValue("@FileType", document.FileType);
                    command.Parameters.AddWithValue("@FilePath", document.FilePath);
                    command.Parameters.AddWithValue("@FileSize", document.FileSize);
                    command.Parameters.AddWithValue("@UploadDate", document.UploadDate);

                    return Convert.ToInt32(await command.ExecuteScalarAsync());
                }
            }
        }

        public async Task<Document> GetDocumentByIdAsync(int documentId)
        {
            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Documents WHERE DocumentID = @DocumentID";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentID", documentId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Document
                            {
                                DocumentID = reader.GetInt32(reader.GetOrdinal("DocumentID")),
                                ClaimID = reader.GetInt32(reader.GetOrdinal("ClaimID")),
                                FileName = reader.GetString(reader.GetOrdinal("FileName")),
                                FileType = reader.GetString(reader.GetOrdinal("FileType")),
                                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                                FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                                UploadDate = reader.GetDateTime(reader.GetOrdinal("UploadDate"))
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task<List<Document>> GetDocumentsByClaimIdAsync(int claimId)
        {
            var documents = new List<Document>();

            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Documents WHERE ClaimID = @ClaimID ORDER BY UploadDate DESC";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ClaimID", claimId);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            documents.Add(new Document
                            {
                                DocumentID = reader.GetInt32(reader.GetOrdinal("DocumentID")),
                                ClaimID = reader.GetInt32(reader.GetOrdinal("ClaimID")),
                                FileName = reader.GetString(reader.GetOrdinal("FileName")),
                                FileType = reader.GetString(reader.GetOrdinal("FileType")),
                                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                                FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                                UploadDate = reader.GetDateTime(reader.GetOrdinal("UploadDate"))
                            });
                        }
                    }
                }
            }
            return documents;
        }

        // Document methods for controller
        public async Task<DocumentInfo> GetDocumentInfoAsync(int documentId)
        {
            using (var connection = new SqlConnection(_dbConnection.ConnectionString))
            {
                await connection.OpenAsync();
                var query = "SELECT FileName, FileType, FilePath FROM Documents WHERE DocumentID = @DocumentID";

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@DocumentID", documentId);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new DocumentInfo
                            {
                                FileName = reader.GetString(0),
                                FileType = reader.GetString(1),
                                FilePath = reader.GetString(2)
                            };
                        }
                    }
                }
            }
            return null;
        }

        public class DocumentInfo
        {
            public string FileName { get; set; } = string.Empty;
            public string FileType { get; set; } = string.Empty;
            public string FilePath { get; set; } = string.Empty;
        }
    }
}