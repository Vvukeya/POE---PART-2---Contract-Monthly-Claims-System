using System.Data.SqlClient;
using System.Diagnostics;

namespace POE___PART_2___Contract_Monthly_Claims_System.Data
{
    public class DatabaseConnection
    {
        private string instanceName = "claim_system";
        private string databaseName = "ContractClaimsDB";
        private string masterConnectionString => $@"Server=(localdb)\{instanceName};Database=master;Integrated Security=true;";
        public string ConnectionString => $@"Server=(localdb)\{instanceName};Database={databaseName};Integrated Security=true;";

        public DatabaseConnection()
        {
            InitializeSystem();
            UpdateUsersTableForStringUserID();
            UpdateClaimsTableSchema();
        }

        public void InitializeSystem()
        {
            try
            {
                Console.WriteLine("Starting database initialization...");

                // Ensure instance is running
                StartInstance();

                // Create database if it doesn't exist
                CreateDatabaseIfNotExists();

                // Create tables
                CreateTables();

                Console.WriteLine("Database system initialized successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing system: {ex.Message}");
            }
        }

        private void StartInstance()
        {
            try
            {
                Console.WriteLine($"Starting LocalDB instance '{instanceName}'...");

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c sqllocaldb start \"{instanceName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = new Process { StartInfo = processStartInfo })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                        Console.WriteLine($"LocalDB instance '{instanceName}' started successfully!");
                    else if (error.Contains("is already running"))
                        Console.WriteLine($"LocalDB instance '{instanceName}' is already running.");
                    else
                        Console.WriteLine($"Note: Could not start instance: {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Note during instance start: {ex.Message}");
            }
        }

        private void CreateDatabaseIfNotExists()
        {
            try
            {
                using (var connection = new SqlConnection(masterConnectionString))
                {
                    connection.Open();

                    string checkDbQuery = $"SELECT COUNT(*) FROM sys.databases WHERE name = '{databaseName}'";
                    using (var checkCmd = new SqlCommand(checkDbQuery, connection))
                    {
                        var exists = (int)checkCmd.ExecuteScalar() > 0;

                        if (!exists)
                        {
                            Console.WriteLine($"Creating database '{databaseName}'...");
                            string createDbQuery = $"CREATE DATABASE [{databaseName}]";
                            using (var createCmd = new SqlCommand(createDbQuery, connection))
                            {
                                createCmd.ExecuteNonQuery();
                                Console.WriteLine($"Database '{databaseName}' created successfully!");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Database '{databaseName}' already exists.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Note during database creation: {ex.Message}");
            }
        }

        private void CreateTables()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();
                    Console.WriteLine("Creating tables...");

                    string createUsersTable = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
                        BEGIN
                            CREATE TABLE Users (
                                UserID NVARCHAR(50) PRIMARY KEY,
                                Role VARCHAR(50) NOT NULL,
                                FirstName VARCHAR(100) NOT NULL,
                                Surname VARCHAR(100) NOT NULL,
                                Address VARCHAR(500) NOT NULL,
                                Email VARCHAR(255) NOT NULL UNIQUE,
                                Password VARCHAR(255) NOT NULL,
                                PhoneNumber VARCHAR(20) NOT NULL,
                                RegistrationDate DATETIME NOT NULL
                            );
                            PRINT 'Users table created successfully.';
                        END
                        ELSE
                        BEGIN
                            PRINT 'Users table already exists.';
                        END";

                    string createClaimsTable = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Claims' AND xtype='U')
                        BEGIN
                            CREATE TABLE Claims (
                                ClaimID INT PRIMARY KEY IDENTITY(1,1),
                                UserID NVARCHAR(50) NOT NULL,
                                Month INT NOT NULL,
                                Year INT NOT NULL,
                                ModuleCode VARCHAR(50) NOT NULL,
                                Sessions INT NOT NULL,
                                Hours DECIMAL(10,2) NOT NULL,
                                HourlyRate DECIMAL(10,2) NOT NULL,
                                Description NVARCHAR(MAX) NOT NULL,
                                Status VARCHAR(50) NOT NULL DEFAULT 'Pending',
                                CorrectionNotes NVARCHAR(MAX),
                                SubmissionDate DATETIME NOT NULL,
                                FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE
                            );
                            PRINT 'Claims table created successfully.';
                        END
                        ELSE
                        BEGIN
                            PRINT 'Claims table already exists.';
                        END";

                    string createDocumentsTable = @"
                        IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Documents' AND xtype='U')
                        BEGIN
                            CREATE TABLE Documents (
                                DocumentID INT PRIMARY KEY IDENTITY(1,1),
                                ClaimID INT NOT NULL,
                                FileName NVARCHAR(500) NOT NULL,
                                FileType VARCHAR(50) NOT NULL,
                                FilePath NVARCHAR(500) NOT NULL,
                                FileSize BIGINT NOT NULL,
                                UploadDate DATETIME NOT NULL,
                                FOREIGN KEY (ClaimID) REFERENCES Claims(ClaimID) ON DELETE CASCADE
                            );
                            PRINT 'Documents table created successfully.';
                        END
                        ELSE
                        BEGIN
                            PRINT 'Documents table already exists.';
                        END";

                    using (var cmd = new SqlCommand(createUsersTable, connection))
                        cmd.ExecuteNonQuery();

                    using (var cmd = new SqlCommand(createClaimsTable, connection))
                        cmd.ExecuteNonQuery();

                    using (var cmd = new SqlCommand(createDocumentsTable, connection))
                        cmd.ExecuteNonQuery();

                    Console.WriteLine("All tables verified successfully!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating tables: {ex.Message}");
                throw;
            }
        }

        private void UpdateUsersTableForStringUserID()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    // Check if UserID is already string type
                    string checkColumnTypeQuery = @"
                        SELECT DATA_TYPE 
                        FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'Users' AND COLUMN_NAME = 'UserID'";

                    using (var checkCmd = new SqlCommand(checkColumnTypeQuery, connection))
                    {
                        var dataType = checkCmd.ExecuteScalar()?.ToString();

                        if (dataType != "nvarchar" && dataType != "varchar")
                        {
                            Console.WriteLine("Converting database to use string UserIDs...");

                            // Drop foreign key constraints first
                            string dropFkQuery = @"
                                IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Claims_Users')
                                    ALTER TABLE Claims DROP CONSTRAINT FK_Claims_Users";

                            using (var dropCmd = new SqlCommand(dropFkQuery, connection))
                                dropCmd.ExecuteNonQuery();

                            // Create temporary tables
                            string createTempUsers = @"
                                CREATE TABLE Users_temp (
                                    UserID NVARCHAR(50) PRIMARY KEY,
                                    Role VARCHAR(50) NOT NULL,
                                    FirstName VARCHAR(100) NOT NULL,
                                    Surname VARCHAR(100) NOT NULL,
                                    Address VARCHAR(500) NOT NULL,
                                    Email VARCHAR(255) NOT NULL UNIQUE,
                                    Password VARCHAR(255) NOT NULL,
                                    PhoneNumber VARCHAR(20) NOT NULL,
                                    RegistrationDate DATETIME NOT NULL
                                );";

                            string createTempClaims = @"
                                CREATE TABLE Claims_temp (
                                    ClaimID INT PRIMARY KEY IDENTITY(1,1),
                                    UserID NVARCHAR(50) NOT NULL,
                                    Month INT NOT NULL,
                                    Year INT NOT NULL,
                                    ModuleCode VARCHAR(50) NOT NULL,
                                    Sessions INT NOT NULL,
                                    Hours DECIMAL(10,2) NOT NULL,
                                    HourlyRate DECIMAL(10,2) NOT NULL,
                                    Description NVARCHAR(MAX) NOT NULL,
                                    Status VARCHAR(50) NOT NULL DEFAULT 'Pending',
                                    CorrectionNotes NVARCHAR(MAX),
                                    SubmissionDate DATETIME NOT NULL,
                                    PaymentDate DATETIME NULL,
                                    PaidBy NVARCHAR(255) NULL
                                );";

                            using (var cmd = new SqlCommand(createTempUsers, connection))
                                cmd.ExecuteNonQuery();

                            using (var cmd = new SqlCommand(createTempClaims, connection))
                                cmd.ExecuteNonQuery();

                            // Drop old tables
                            string dropOldTables = @"
                                DROP TABLE IF EXISTS Documents;
                                DROP TABLE IF EXISTS Claims;
                                DROP TABLE IF EXISTS Users;";

                            using (var cmd = new SqlCommand(dropOldTables, connection))
                                cmd.ExecuteNonQuery();

                            // Rename temp tables
                            string renameTables = @"
                                EXEC sp_rename 'Users_temp', 'Users';
                                EXEC sp_rename 'Claims_temp', 'Claims';";

                            using (var cmd = new SqlCommand(renameTables, connection))
                                cmd.ExecuteNonQuery();

                            // Recreate Documents table
                            string createDocumentsTable = @"
                                CREATE TABLE Documents (
                                    DocumentID INT PRIMARY KEY IDENTITY(1,1),
                                    ClaimID INT NOT NULL,
                                    FileName NVARCHAR(500) NOT NULL,
                                    FileType VARCHAR(50) NOT NULL,
                                    FilePath NVARCHAR(500) NOT NULL,
                                    FileSize BIGINT NOT NULL,
                                    UploadDate DATETIME NOT NULL,
                                    FOREIGN KEY (ClaimID) REFERENCES Claims(ClaimID) ON DELETE CASCADE
                                );";

                            using (var cmd = new SqlCommand(createDocumentsTable, connection))
                                cmd.ExecuteNonQuery();

                            // Add foreign key constraint
                            string addFkQuery = @"
                                ALTER TABLE Claims 
                                ADD CONSTRAINT FK_Claims_Users 
                                FOREIGN KEY (UserID) REFERENCES Users(UserID) ON DELETE CASCADE";

                            using (var cmd = new SqlCommand(addFkQuery, connection))
                                cmd.ExecuteNonQuery();

                            Console.WriteLine("Database successfully converted to use string UserIDs.");
                        }
                        else
                        {
                            Console.WriteLine("Database already uses string UserIDs.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating Users table for string UserID: {ex.Message}");
            }
        }

        private void UpdateClaimsTableSchema()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    // Check if PaymentDate column exists
                    string checkPaymentDateQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'Claims' AND COLUMN_NAME = 'PaymentDate'";

                    using (var checkCmd = new SqlCommand(checkPaymentDateQuery, connection))
                    {
                        var paymentDateExists = (int)checkCmd.ExecuteScalar() > 0;

                        if (!paymentDateExists)
                        {
                            // Add PaymentDate column
                            string addPaymentDateQuery = @"ALTER TABLE Claims ADD PaymentDate DATETIME NULL";
                            using (var addCmd = new SqlCommand(addPaymentDateQuery, connection))
                            {
                                addCmd.ExecuteNonQuery();
                                Console.WriteLine("Added PaymentDate column to Claims table.");
                            }
                        }
                    }

                    // Check if PaidBy column exists
                    string checkPaidByQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'Claims' AND COLUMN_NAME = 'PaidBy'";

                    using (var checkCmd = new SqlCommand(checkPaidByQuery, connection))
                    {
                        var paidByExists = (int)checkCmd.ExecuteScalar() > 0;

                        if (!paidByExists)
                        {
                            // Add PaidBy column
                            string addPaidByQuery = @"ALTER TABLE Claims ADD PaidBy NVARCHAR(255) NULL";
                            using (var addCmd = new SqlCommand(addPaidByQuery, connection))
                            {
                                addCmd.ExecuteNonQuery();
                                Console.WriteLine("Added PaidBy column to Claims table.");
                            }
                        }
                    }

                    // ADD THIS NEW CODE FOR VerifiedBy AND ApprovedBy COLUMNS:

                    // Check if VerifiedBy column exists
                    string checkVerifiedByQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'Claims' AND COLUMN_NAME = 'VerifiedBy'";

                    using (var checkCmd = new SqlCommand(checkVerifiedByQuery, connection))
                    {
                        var verifiedByExists = (int)checkCmd.ExecuteScalar() > 0;

                        if (!verifiedByExists)
                        {
                            // Add VerifiedBy column
                            string addVerifiedByQuery = @"ALTER TABLE Claims ADD VerifiedBy NVARCHAR(255) NULL";
                            using (var addCmd = new SqlCommand(addVerifiedByQuery, connection))
                            {
                                addCmd.ExecuteNonQuery();
                                Console.WriteLine("Added VerifiedBy column to Claims table.");
                            }
                        }
                    }

                    // Check if ApprovedBy column exists
                    string checkApprovedByQuery = @"
                SELECT COUNT(*) 
                FROM INFORMATION_SCHEMA.COLUMNS 
                WHERE TABLE_NAME = 'Claims' AND COLUMN_NAME = 'ApprovedBy'";

                    using (var checkCmd = new SqlCommand(checkApprovedByQuery, connection))
                    {
                        var approvedByExists = (int)checkCmd.ExecuteScalar() > 0;

                        if (!approvedByExists)
                        {
                            // Add ApprovedBy column
                            string addApprovedByQuery = @"ALTER TABLE Claims ADD ApprovedBy NVARCHAR(255) NULL";
                            using (var addCmd = new SqlCommand(addApprovedByQuery, connection))
                            {
                                addCmd.ExecuteNonQuery();
                                Console.WriteLine("Added ApprovedBy column to Claims table.");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating Claims table schema: {ex.Message}");
            }
        }
        

        public bool TestConnection()
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    using (var cmd = new SqlCommand("SELECT 1", connection))
                    {
                        var result = cmd.ExecuteScalar();
                        return result != null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Connection test failed: {ex.Message}");
                return false;
            }
        }
    }
}