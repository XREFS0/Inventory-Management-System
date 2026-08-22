Option Strict On
Option Explicit On

Imports System
Imports MASA.InventoryManagementSystem.Infrastructure.Configuration
Imports MASA.InventoryManagementSystem.Infrastructure.Database
Imports MASA.InventoryManagementSystem.Infrastructure.Security
Imports MASA.InventoryManagementSystem.Presentation.Forms

Public Module Program

    <STAThread()>
    Public Sub Main(args As String())
        System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.SystemAware)
        System.Windows.Forms.Application.EnableVisualStyles()
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(False)

        Dim config = AppConfiguration.Instance

        If config.AutoInitializeSchema Then
            Try
                Task.Run(Async Function()
                             Await DatabaseInitializer.InitializeDatabaseAsync().ConfigureAwait(False)
                         End Function).Wait(TimeSpan.FromSeconds(5))
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Auto-init database skipped/failed: {ex.Message}")
            End Try
        End If

        Using loginForm As New LoginForm()
            Dim dialogResult = loginForm.ShowDialog()
            If dialogResult = System.Windows.Forms.DialogResult.OK AndAlso SessionContext.IsAuthenticated Then
                System.Windows.Forms.Application.Run(New MainForm())
            End If
        End Using
    End Sub

End Module
