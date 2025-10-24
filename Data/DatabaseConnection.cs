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
                                UserID INT PRIMARY KEY IDENTITY(1,1),
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
                                UserID INT NOT NULL,
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