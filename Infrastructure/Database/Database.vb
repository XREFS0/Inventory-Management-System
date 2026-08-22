Option Strict On
Option Explicit On

Imports System
Imports System.Data
Imports System.Data.Common
Imports System.IO
Imports System.Reflection
Imports System.Threading.Tasks
Imports Npgsql
Imports MASA.InventoryManagementSystem.Infrastructure.Configuration

Namespace Infrastructure.Database

    Public Class DbConnectionFactory

        Public Shared Function CreateConnection() As NpgsqlConnection
            Dim connStr = AppConfiguration.Instance.ConnectionString
            If String.IsNullOrWhiteSpace(connStr) Then
                Throw New InvalidOperationException("Database connection string is not configured. Please verify your settings or appsettings.json.")
            End If
            Return New NpgsqlConnection(connStr)
        End Function

        Public Shared Async Function OpenConnectionAsync() As Task(Of NpgsqlConnection)
            Dim conn = CreateConnection()
            Try
                Await conn.OpenAsync().ConfigureAwait(False)
                Return conn
            Catch ex As Exception
                conn.Dispose()
                Throw New InvalidOperationException($"Unable to connect to PostgreSQL/Supabase database: {ex.Message}", ex)
            End Try
        End Function

        Public Shared Async Function TestConnectionAsync(Optional customConnString As String = "") As Task(Of (Success As Boolean, Message As String))
            Dim connStr = If(String.IsNullOrWhiteSpace(customConnString), AppConfiguration.Instance.ConnectionString, customConnString)
            If String.IsNullOrWhiteSpace(connStr) Then
                Return (False, "Connection string is empty.")
            End If

            Try
                Using conn As New NpgsqlConnection(connStr)
                    Await conn.OpenAsync().ConfigureAwait(False)
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT version();"
                        Dim versionObj = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                        Dim versionStr = If(versionObj?.ToString(), "PostgreSQL Database")
                        Return (True, $"Connected successfully! Server: {versionStr}")
                    End Using
                End Using
            Catch ex As Exception
                Return (False, $"Connection failed: {ex.Message}")
            End Try
        End Function

    End Class

    Public Class DatabaseInitializer

        Public Shared Async Function InitializeDatabaseAsync() As Task(Of (Success As Boolean, Message As String))
            Try
                Dim tablesExist = Await AreCoreTablesPresentAsync().ConfigureAwait(False)
                If Not tablesExist Then
                    Dim schemaScript = LoadEmbeddedSql("schema.sql")
                    Dim seedScript = LoadEmbeddedSql("seed.sql")

                    Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                        Using tx = Await conn.BeginTransactionAsync().ConfigureAwait(False)
                            Dim failedMessage As String = Nothing
                            Try
                                If Not String.IsNullOrWhiteSpace(schemaScript) Then
                                    Using cmd = conn.CreateCommand()
                                        cmd.Transaction = tx
                                        cmd.CommandText = schemaScript
                                        cmd.CommandTimeout = 60
                                        Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                                    End Using
                                End If

                                If Not String.IsNullOrWhiteSpace(seedScript) Then
                                    Using cmd = conn.CreateCommand()
                                        cmd.Transaction = tx
                                        cmd.CommandText = seedScript
                                        cmd.CommandTimeout = 60
                                        Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                                    End Using
                                End If

                                Await tx.CommitAsync().ConfigureAwait(False)
                            Catch ex As Exception
                                tx.Rollback()
                                failedMessage = ex.Message
                            End Try

                            If failedMessage IsNot Nothing Then
                                Return (False, $"Failed during database initialization: {failedMessage}")
                            End If
                            Return (True, "Database schema and initial seed data provisioned successfully.")
                        End Using
                    End Using
                Else
                    Return (True, "Database schema is already up to date.")
                End If
            Catch ex As Exception
                Return (False, $"Database connection or initialization error: {ex.Message}")
            End Try
        End Function

        Public Shared Async Function ResetAndSeedDatabaseAsync() As Task(Of (Success As Boolean, Message As String))
            Try
                Dim schemaScript = LoadEmbeddedSql("schema.sql")
                Dim seedScript = LoadEmbeddedSql("seed.sql")

                Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                    Using tx = Await conn.BeginTransactionAsync().ConfigureAwait(False)
                        Dim failedMessage As String = Nothing
                        Try
                            Using cmd = conn.CreateCommand()
                                cmd.Transaction = tx
                                cmd.CommandText = schemaScript & vbCrLf & seedScript
                                cmd.CommandTimeout = 90
                                Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                            End Using

                            Await tx.CommitAsync().ConfigureAwait(False)
                        Catch ex As Exception
                            tx.Rollback()
                            failedMessage = ex.Message
                        End Try

                        If failedMessage IsNot Nothing Then
                            Return (False, $"Failed to reset/seed: {failedMessage}")
                        End If

                        Await EnsureAdminUserExistsAsync(conn).ConfigureAwait(False)

                        Return (True, "Database successfully initialized and seeded.")
                    End Using
                End Using
            Catch ex As Exception
                Return (False, $"Error: {ex.Message}")
            End Try
        End Function

        Public Shared Async Function EnsureAdminUserExistsAsync(conn As NpgsqlConnection) As Task
            Try
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = "SELECT COUNT(*) FROM users WHERE username = 'admin';"
                    Dim count = Convert.ToInt32(Await cmd.ExecuteScalarAsync().ConfigureAwait(False))
                    If count = 0 Then
                        Dim hash = Security.PasswordHasher.HashPassword("Admin@123")
                        cmd.CommandText = "INSERT INTO users (username, password_hash, full_name, email, phone, role_id, is_active) " &
                                          "VALUES ('admin', @hash, 'Mohamed El-Sayed (Admin)', 'admin@masainventory.com.eg', '+20 100 234 5678', 1, TRUE) " &
                                          "ON CONFLICT (username) DO UPDATE SET password_hash = @hash, is_active = TRUE;"
                        cmd.Parameters.AddWithValue("@hash", hash)
                        Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Else
                        Dim hash = Security.PasswordHasher.HashPassword("Admin@123")
                        cmd.CommandText = "UPDATE users SET password_hash = @hash, is_active = TRUE WHERE username = 'admin';"
                        cmd.Parameters.AddWithValue("@hash", hash)
                        Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    End If
                End Using
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"EnsureAdminUserExistsAsync warning: {ex.Message}")
            End Try
        End Function

        Public Shared Async Function AreCoreTablesPresentAsync() As Task(Of Boolean)
            Try
                Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name IN ('products', 'users', 'warehouses', 'stock_transactions');"
                        Dim countObj = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                        Dim count = Convert.ToInt32(countObj)
                        Return count >= 4
                    End Using
                End Using
            Catch
                Return False
            End Try
        End Function

        Private Shared Function LoadEmbeddedSql(fileName As String) As String
            Try
                Dim currentAsm As Assembly = Assembly.GetExecutingAssembly()
                Dim resourceName = $"MASA.InventoryManagementSystem.{fileName}"
                Dim fullResourceName = $"MASA.InventoryManagementSystem.Database.{fileName}"

                Using stream = currentAsm.GetManifestResourceStream(fullResourceName)
                    If stream IsNot Nothing Then
                        Using reader As New StreamReader(stream)
                            Return reader.ReadToEnd()
                        End Using
                    End If
                End Using

                Using stream = currentAsm.GetManifestResourceStream(resourceName)
                    If stream IsNot Nothing Then
                        Using reader As New StreamReader(stream)
                            Return reader.ReadToEnd()
                        End Using
                    End If
                End Using

                Dim localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", fileName)
                If File.Exists(localPath) Then
                    Return File.ReadAllText(localPath)
                End If

                Dim wsPath = Path.Combine(Directory.GetCurrentDirectory(), "Database", fileName)
                If File.Exists(wsPath) Then
                    Return File.ReadAllText(wsPath)
                End If
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Failed to load SQL file {fileName}: {ex.Message}")
            End Try
            Return String.Empty
        End Function

    End Class

End Namespace
