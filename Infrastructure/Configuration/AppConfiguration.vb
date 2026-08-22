Option Strict On
Option Explicit On

Imports System
Imports System.IO
Imports Microsoft.Extensions.Configuration

Namespace Infrastructure.Configuration

    Public Class AppConfiguration

        Private Shared _instance As AppConfiguration
        Private Shared ReadOnly _lock As New Object()
        Private _configRoot As IConfigurationRoot

        Public Property ConnectionString As String = String.Empty
        Public Property SupabaseUrl As String = "https://uyxoclhhhrbeskdxlsnv.supabase.co"
        Public Property SupabaseProjectRef As String = "uyxoclhhhrbeskdxlsnv"
        Public Property SupabaseAnonKey As String = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6InV5eG9jbGhoaHJiZXNrZHhsc252Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3ODc0MDc3MTAsImV4cCI6MjEwMjk4MzcxMH0.cTAjNMfMNVre_hojSPe1iVuKY4rntWpQ2LqE93OaQfs"
        Public Property PoolerHost As String = "aws-0-eu-central-1.pooler.supabase.com"
        Public Property PoolerUsername As String = "postgres.uyxoclhhhrbeskdxlsnv"

        Public Property ApplicationName As String = "MASA Inventory Management System - Egypt"
        Public Property CompanyName As String = "MASA Enterprises Egypt SAE"
        Public Property DefaultCurrency As String = "EGP"
        Public Property CurrencySymbol As String = "EGP"
        Public Property LowStockThresholdDefault As Integer = 10
        Public Property EnableAuditLogging As Boolean = True
        Public Property AutoInitializeSchema As Boolean = True

        Private Sub New()
            LoadConfiguration()
        End Sub

        Public Shared ReadOnly Property Instance As AppConfiguration
            Get
                If _instance Is Nothing Then
                    SyncLock _lock
                        If _instance Is Nothing Then
                            _instance = New AppConfiguration()
                        End If
                    End SyncLock
                End If
                Return _instance
            End Get
        End Property

        Public Sub LoadConfiguration()
            Try
                Dim builder = New ConfigurationBuilder() _
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory) _
                    .AddJsonFile("appsettings.json", optional:=True, reloadOnChange:=True) _
                    .AddEnvironmentVariables()

                _configRoot = builder.Build()

                Dim envConn = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION_STRING")
                If String.IsNullOrWhiteSpace(envConn) Then
                    envConn = Environment.GetEnvironmentVariable("DATABASE_URL")
                End If

                If Not String.IsNullOrWhiteSpace(envConn) Then
                    ConnectionString = envConn
                Else
                    Dim jsonConn = _configRoot.GetConnectionString("PostgreSql")
                    If Not String.IsNullOrWhiteSpace(jsonConn) Then
                        ConnectionString = jsonConn
                    End If
                End If

                Dim supaSection = _configRoot.GetSection("Supabase")
                If supaSection.Exists() Then
                    If Not String.IsNullOrWhiteSpace(supaSection("Url")) Then SupabaseUrl = supaSection("Url")
                    If Not String.IsNullOrWhiteSpace(supaSection("ProjectRef")) Then SupabaseProjectRef = supaSection("ProjectRef")
                    If Not String.IsNullOrWhiteSpace(supaSection("AnonKey")) Then SupabaseAnonKey = supaSection("AnonKey")
                    If Not String.IsNullOrWhiteSpace(supaSection("PoolerHost")) Then PoolerHost = supaSection("PoolerHost")
                    If Not String.IsNullOrWhiteSpace(supaSection("PoolerUsername")) Then PoolerUsername = supaSection("PoolerUsername")
                End If

                Dim appSection = _configRoot.GetSection("ApplicationSettings")
                If appSection.Exists() Then
                    If Not String.IsNullOrWhiteSpace(appSection("ApplicationName")) Then
                        ApplicationName = appSection("ApplicationName")
                    End If
                    If Not String.IsNullOrWhiteSpace(appSection("Company")) Then
                        CompanyName = appSection("Company")
                    End If
                    If Not String.IsNullOrWhiteSpace(appSection("DefaultCurrency")) Then
                        DefaultCurrency = appSection("DefaultCurrency")
                    End If
                    If Not String.IsNullOrWhiteSpace(appSection("CurrencySymbol")) Then
                        CurrencySymbol = appSection("CurrencySymbol")
                    End If

                    Dim thresholdStr = appSection("LowStockThresholdDefault")
                    Dim parsedThreshold As Integer
                    If Integer.TryParse(thresholdStr, parsedThreshold) Then
                        LowStockThresholdDefault = parsedThreshold
                    End If

                    Dim auditStr = appSection("EnableAuditLogging")
                    Dim parsedAudit As Boolean
                    If Boolean.TryParse(auditStr, parsedAudit) Then
                        EnableAuditLogging = parsedAudit
                    End If

                    Dim autoInitStr = appSection("AutoInitializeSchema")
                    Dim parsedAutoInit As Boolean
                    If Boolean.TryParse(autoInitStr, parsedAutoInit) Then
                        AutoInitializeSchema = parsedAutoInit
                    End If
                End If
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Error loading configuration: {ex.Message}")
            End Try
        End Sub

        Public Sub SaveConfiguration(newConnectionString As String, company As String, currency As String)
            ConnectionString = newConnectionString
            CompanyName = company
            CurrencySymbol = currency

            Try
                Dim filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json")
                Dim jsonContent As String = "{" & vbCrLf &
                    "  ""ConnectionStrings"": {" & vbCrLf &
                    "    ""PostgreSql"": """ & newConnectionString.Replace("\", "\\").Replace("""", "\""") & """" & vbCrLf &
                    "  }," & vbCrLf &
                    "  ""Supabase"": {" & vbCrLf &
                    "    ""Url"": """ & SupabaseUrl & """," & vbCrLf &
                    "    ""ProjectRef"": """ & SupabaseProjectRef & """," & vbCrLf &
                    "    ""AnonKey"": """ & SupabaseAnonKey & """," & vbCrLf &
                    "    ""PoolerHost"": """ & PoolerHost & """," & vbCrLf &
                    "    ""PoolerUsername"": """ & PoolerUsername & """" & vbCrLf &
                    "  }," & vbCrLf &
                    "  ""ApplicationSettings"": {" & vbCrLf &
                    "    ""ApplicationName"": """ & ApplicationName & """," & vbCrLf &
                    "    ""Company"": """ & CompanyName & """," & vbCrLf &
                    "    ""DefaultCurrency"": """ & DefaultCurrency & """," & vbCrLf &
                    "    ""CurrencySymbol"": """ & CurrencySymbol & """," & vbCrLf &
                    "    ""LowStockThresholdDefault"": " & LowStockThresholdDefault.ToString() & "," & vbCrLf &
                    "    ""EnableAuditLogging"": " & EnableAuditLogging.ToString().ToLower() & "," & vbCrLf &
                    "    ""AutoInitializeSchema"": " & AutoInitializeSchema.ToString().ToLower() & vbCrLf &
                    "  }" & vbCrLf &
                    "}"
                File.WriteAllText(filePath, jsonContent)
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Failed to write appsettings.json: {ex.Message}")
            End Try
        End Sub

    End Class

End Namespace
