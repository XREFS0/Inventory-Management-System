Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Drawing
Imports System.IO
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports MASA.InventoryManagementSystem.Application.Services
Imports MASA.InventoryManagementSystem.Domain.Entities
Imports MASA.InventoryManagementSystem.Infrastructure.Configuration
Imports MASA.InventoryManagementSystem.Infrastructure.Database
Imports MASA.InventoryManagementSystem.Infrastructure.Security
Imports MASA.InventoryManagementSystem.Presentation.Common
Imports MASA.InventoryManagementSystem.Presentation.Controls
Imports MASA.InventoryManagementSystem.Presentation.Forms

Namespace Presentation.Views

    Public Class ReportsView
        Inherits UserControl

        Private ReadOnly _reportService As New ReportService()
        Private ReadOnly _warehouseService As New WarehouseService()
        Private ReadOnly _categoryService As New CategoryService()

        Private cboReportType As ComboBox
        Private cboWarehouseFilter As ComboBox
        Private cboCategoryFilter As ComboBox
        Private dtpFromDate As DateTimePicker
        Private dtpToDate As DateTimePicker
        Private btnGenerate As Button
        Private btnExportCsv As Button
        Private dgvReportResult As DataGridView
        Private lblSummary As Label
        Private _currentReportData As DataTable

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Business Intelligence & Operational Reports", .Font = UITheme.FontHero, .ForeColor = UITheme.TextPrimary, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)
            Controls.Add(pnlHeader)

            Dim pnlConfig As New CardPanel() With {.Dock = DockStyle.Top, .Height = 90, .Padding = New Padding(12)}

            Dim lblType As New Label() With {.Text = "Report Template:", .Location = New Point(12, 14), .AutoSize = True, .Font = UITheme.FontBodyBold}
            pnlConfig.Controls.Add(lblType)
            cboReportType = New ComboBox() With {.Location = New Point(12, 34), .Width = 240, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            cboReportType.Items.AddRange(New Object() {
                "1. Current Inventory Valuation Report",
                "2. Low Stock & Reorder Warning Report",
                "3. Historical Stock Movement Log",
                "4. Sales & Revenue Summary",
                "5. Purchase Order Spend Summary"
            })
            cboReportType.SelectedIndex = 0
            AddHandler cboReportType.SelectedIndexChanged, AddressOf ReportTypeChanged
            pnlConfig.Controls.Add(cboReportType)

            Dim lblWh As New Label() With {.Text = "Facility / Warehouse:", .Location = New Point(265, 14), .AutoSize = True, .Font = UITheme.FontBodyBold}
            pnlConfig.Controls.Add(lblWh)
            cboWarehouseFilter = New ComboBox() With {.Location = New Point(265, 34), .Width = 180, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            pnlConfig.Controls.Add(cboWarehouseFilter)

            Dim lblFrom As New Label() With {.Text = "From Date:", .Location = New Point(460, 14), .AutoSize = True, .Font = UITheme.FontBodyBold}
            pnlConfig.Controls.Add(lblFrom)
            dtpFromDate = New DateTimePicker() With {.Location = New Point(460, 34), .Width = 120, .Format = DateTimePickerFormat.Short, .Value = DateTime.Today.AddDays(-30), .Font = UITheme.FontBody}
            pnlConfig.Controls.Add(dtpFromDate)

            Dim lblTo As New Label() With {.Text = "To Date:", .Location = New Point(590, 14), .AutoSize = True, .Font = UITheme.FontBodyBold}
            pnlConfig.Controls.Add(lblTo)
            dtpToDate = New DateTimePicker() With {.Location = New Point(590, 34), .Width = 120, .Format = DateTimePickerFormat.Short, .Value = DateTime.Today, .Font = UITheme.FontBody}
            pnlConfig.Controls.Add(dtpToDate)

            btnGenerate = New Button() With {.Text = "Run Report", .Location = New Point(725, 32), .Width = 120, .Height = 30}
            UITheme.StylePrimaryButton(btnGenerate)
            AddHandler btnGenerate.Click, AddressOf BtnGenerate_Click
            pnlConfig.Controls.Add(btnGenerate)

            btnExportCsv = New Button() With {.Text = "Export CSV", .Location = New Point(855, 32), .Width = 120, .Height = 30}
            UITheme.StyleSecondaryButton(btnExportCsv)
            AddHandler btnExportCsv.Click, AddressOf BtnExportCsv_Click
            pnlConfig.Controls.Add(btnExportCsv)

            Controls.Add(pnlConfig)
            pnlConfig.BringToFront()

            Dim pnlResult As New CardPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(16)}

            lblSummary = New Label() With {
                .Text = "Ready to generate report. Select a report template and click 'Run Report'.",
                .Dock = DockStyle.Top,
                .Height = 28,
                .ForeColor = UITheme.TextSecondary,
                .Font = UITheme.FontBodyBold
            }
            pnlResult.Controls.Add(lblSummary)

            dgvReportResult = New DataGridView() With {.Dock = DockStyle.Fill, .ReadOnly = True}
            UITheme.ApplyModernDataGridStyle(dgvReportResult)
            dgvReportResult.AutoGenerateColumns = True
            pnlResult.Controls.Add(dgvReportResult)
            dgvReportResult.BringToFront()

            Controls.Add(pnlResult)
            pnlResult.BringToFront()
        End Sub

        Public Async Sub LoadDataAsync()
            Try
                Dim warehouses = Await _warehouseService.GetAllWarehousesAsync()
                cboWarehouseFilter.Items.Clear()
                cboWarehouseFilter.Items.Add(New With {.Key = 0, .Value = "All Warehouses"})
                For Each w In warehouses
                    cboWarehouseFilter.Items.Add(New With {.Key = w.WarehouseId, .Value = w.Name})
                Next
                cboWarehouseFilter.DisplayMember = "Value"
                cboWarehouseFilter.ValueMember = "Key"
                cboWarehouseFilter.SelectedIndex = 0

                Await GenerateCurrentReportAsync()
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Report filter error: {ex.Message}")
            End Try
        End Sub

        Private Sub ReportTypeChanged(sender As Object, e As EventArgs)
            Dim isDateRelevant = (cboReportType.SelectedIndex >= 2)
            dtpFromDate.Enabled = isDateRelevant
            dtpToDate.Enabled = isDateRelevant
        End Sub

        Private Async Sub BtnGenerate_Click(sender As Object, e As EventArgs)
            Await GenerateCurrentReportAsync()
        End Sub

        Private Async Function GenerateCurrentReportAsync() As Task
            Dim whItem = CType(cboWarehouseFilter.SelectedItem, Object)
            Dim whId As Integer = If(whItem IsNot Nothing, CInt(whItem.GetType().GetProperty("Key").GetValue(whItem, Nothing)), 0)
            Dim whNullable As Nullable(Of Integer) = If(whId > 0, CType(whId, Nullable(Of Integer)), Nothing)

            btnGenerate.Enabled = False
            lblSummary.Text = "Querying database..."

            Try
                Select Case cboReportType.SelectedIndex
                    Case 0
                        _currentReportData = Await _reportService.GetCurrentInventoryValuationReportAsync(whNullable, Nothing)
                    Case 1
                        _currentReportData = Await _reportService.GetLowStockReportAsync(whNullable)
                    Case 2
                        _currentReportData = Await _reportService.GetStockMovementReportAsync(dtpFromDate.Value, dtpToDate.Value, Nothing, whNullable)
                    Case 3
                        _currentReportData = Await _reportService.GetSalesReportAsync(dtpFromDate.Value, dtpToDate.Value, Nothing, whNullable)
                    Case 4
                        _currentReportData = Await _reportService.GetPurchasesReportAsync(dtpFromDate.Value, dtpToDate.Value, Nothing, whNullable)
                End Select

                dgvReportResult.DataSource = _currentReportData
                lblSummary.Text = $"Report Generated: {_currentReportData.Rows.Count:N0} records returned."
            Catch ex As Exception
                MessageBox.Show($"Report generation error: {ex.Message}", "Query Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                lblSummary.Text = "Report query encountered an error."
            Finally
                btnGenerate.Enabled = True
            End Try
        End Function

        Private Sub BtnExportCsv_Click(sender As Object, e As EventArgs)
            If _currentReportData Is Nothing OrElse _currentReportData.Rows.Count = 0 Then
                MessageBox.Show("No report data available to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Using sfd As New SaveFileDialog()
                sfd.Filter = "CSV Files (*.csv)|*.csv"
                sfd.FileName = $"InventoryReport_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                If sfd.ShowDialog() = DialogResult.OK Then
                    Dim res = ReportService.ExportDataTableToCsv(_currentReportData, sfd.FileName)
                    If res.Success Then
                        MessageBox.Show("Report exported successfully to CSV!", "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Else
                        MessageBox.Show(res.Message, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If
                End If
            End Using
        End Sub
    End Class

    Public Class UsersView
        Inherits UserControl

        Private ReadOnly _userService As New UserService()
        Private dgvUsers As DataGridView
        Private btnNewUser As Button
        Private btnEditUser As Button
        Private btnToggleActive As Button

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "User Accounts & Role Permissions", .Font = UITheme.FontHero, .ForeColor = UITheme.TextPrimary, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            btnNewUser = New Button() With {.Text = "+ Create User", .Size = New Size(140, 36), .Location = New Point(Width - 164, 2), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UITheme.StylePrimaryButton(btnNewUser)
            AddHandler btnNewUser.Click, AddressOf BtnNewUser_Click
            pnlHeader.Controls.Add(btnNewUser)
            Controls.Add(pnlHeader)

            Dim pnlContent As New CardPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(16)}

            Dim pnlActions As New Panel() With {.Dock = DockStyle.Top, .Height = 40}
            btnEditUser = New Button() With {.Text = "Edit Account", .Size = New Size(120, 32), .Location = New Point(0, 2)}
            UITheme.StyleSecondaryButton(btnEditUser)
            AddHandler btnEditUser.Click, AddressOf BtnEditUser_Click
            pnlActions.Controls.Add(btnEditUser)

            btnToggleActive = New Button() With {.Text = "Toggle Status", .Size = New Size(120, 32), .Location = New Point(130, 2)}
            UITheme.StyleSecondaryButton(btnToggleActive)
            AddHandler btnToggleActive.Click, AddressOf BtnToggleActive_Click
            pnlActions.Controls.Add(btnToggleActive)

            pnlContent.Controls.Add(pnlActions)

            dgvUsers = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyModernDataGridStyle(dgvUsers)
            dgvUsers.Columns.Add("UserId", "ID")
            dgvUsers.Columns.Add("Username", "Username")
            dgvUsers.Columns.Add("FullName", "Full Name")
            dgvUsers.Columns.Add("Role", "Role")
            dgvUsers.Columns.Add("Email", "Email")
            dgvUsers.Columns.Add("Phone", "Phone")
            dgvUsers.Columns.Add("Status", "Account Status")
            dgvUsers.Columns.Add("LastLogin", "Last Login")

            dgvUsers.Columns(0).Visible = False
            dgvUsers.Columns(1).Width = 140
            dgvUsers.Columns(2).Width = 180
            dgvUsers.Columns(3).Width = 140
            dgvUsers.Columns(4).Width = 180
            dgvUsers.Columns(5).Width = 130
            dgvUsers.Columns(6).Width = 110
            dgvUsers.Columns(7).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

            AddHandler dgvUsers.CellDoubleClick, AddressOf DgvUsers_CellDoubleClick

            pnlContent.Controls.Add(dgvUsers)
            dgvUsers.BringToFront()

            Controls.Add(pnlContent)
            pnlContent.BringToFront()
        End Sub

        Public Async Sub LoadDataAsync()
            Try
                Dim users = Await _userService.GetAllUsersAsync()
                dgvUsers.Rows.Clear()
                For Each u In users
                    dgvUsers.Rows.Add(
                        u.UserId,
                        u.Username,
                        u.FullName,
                        u.RoleName,
                        u.Email,
                        u.Phone,
                        If(u.IsActive, "Active", "Disabled"),
                        If(u.LastLoginAt.HasValue, u.LastLoginAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm"), "Never")
                    )
                Next
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Users load error: {ex.Message}")
            End Try
        End Sub

        Private Sub BtnNewUser_Click(sender As Object, e As EventArgs)
            Using dlg As New UserEditDialog()
                If dlg.ShowDialog() = DialogResult.OK Then
                    LoadDataAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnEditUser_Click(sender As Object, e As EventArgs)
            If dgvUsers.SelectedRows.Count = 0 Then Return
            Dim userId = Convert.ToInt32(dgvUsers.SelectedRows(0).Cells("UserId").Value)
            Dim users = Await _userService.GetAllUsersAsync()
            Dim user = users.Find(Function(u) u.UserId = userId)
            If user IsNot Nothing Then
                Using dlg As New UserEditDialog(user)
                    If dlg.ShowDialog() = DialogResult.OK Then
                        LoadDataAsync()
                    End If
                End Using
            End If
        End Sub

        Private Sub DgvUsers_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 Then BtnEditUser_Click(sender, EventArgs.Empty)
        End Sub

        Private Async Sub BtnToggleActive_Click(sender As Object, e As EventArgs)
            If dgvUsers.SelectedRows.Count = 0 Then Return
            Dim userId = Convert.ToInt32(dgvUsers.SelectedRows(0).Cells("UserId").Value)
            Dim users = Await _userService.GetAllUsersAsync()
            Dim user = users.Find(Function(u) u.UserId = userId)
            If user IsNot Nothing Then
                user.IsActive = Not user.IsActive
                Dim res = Await _userService.UpdateUserAsync(user)
                If res.Success Then
                    LoadDataAsync()
                Else
                    MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            End If
        End Sub
    End Class

    Public Class AuditLogsView
        Inherits UserControl

        Private ReadOnly _auditService As New AuditService()
        Private dgvLogs As DataGridView
        Private btnRefresh As Button

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Immutable System Audit Trail", .Font = UITheme.FontHero, .ForeColor = UITheme.TextPrimary, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            btnRefresh = New Button() With {.Text = "Refresh Trail", .Size = New Size(130, 36), .Location = New Point(Width - 154, 2), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UITheme.StyleSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, AddressOf BtnRefresh_Click
            pnlHeader.Controls.Add(btnRefresh)
            Controls.Add(pnlHeader)

            Dim pnlContent As New CardPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(16)}

            dgvLogs = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyModernDataGridStyle(dgvLogs)
            dgvLogs.Columns.Add("Date", "Timestamp")
            dgvLogs.Columns.Add("User", "User")
            dgvLogs.Columns.Add("Action", "Action")
            dgvLogs.Columns.Add("EntityType", "Target Entity")
            dgvLogs.Columns.Add("EntityId", "Entity ID")
            dgvLogs.Columns.Add("Details", "Payload Details / Changes")

            dgvLogs.Columns(0).Width = 140
            dgvLogs.Columns(1).Width = 120
            dgvLogs.Columns(2).Width = 160
            dgvLogs.Columns(3).Width = 130
            dgvLogs.Columns(4).Width = 110
            dgvLogs.Columns(5).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

            pnlContent.Controls.Add(dgvLogs)
            Controls.Add(pnlContent)
            pnlContent.BringToFront()
        End Sub

        Public Async Sub LoadDataAsync()
            Try
                Dim logs = Await _auditService.GetRecentLogsAsync(200)
                dgvLogs.Rows.Clear()
                For Each l In logs
                    Dim details = If(Not String.IsNullOrWhiteSpace(l.NewValues), l.NewValues, l.OldValues)
                    dgvLogs.Rows.Add(
                        l.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                        l.Username,
                        l.Action,
                        l.EntityType,
                        l.EntityId,
                        details
                    )
                Next
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Audit logs load error: {ex.Message}")
            End Try
        End Sub

        Private Sub BtnRefresh_Click(sender As Object, e As EventArgs)
            LoadDataAsync()
        End Sub
    End Class

    Public Class SettingsView
        Inherits UserControl

        Private txtConnString As TextBox
        Private btnTestConn As Button
        Private btnSaveConn As Button
        Private btnInitSchema As Button
        Private lblStatus As Label
        Private txtCompanyName As TextBox
        Private txtCurrencySymbol As TextBox

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            Padding = New Padding(24)
            AutoScroll = True

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "System Configuration & Database Connection", .Font = UITheme.FontHero, .ForeColor = UITheme.TextPrimary, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)
            Controls.Add(pnlHeader)

            Dim pnlDb As New CardPanel() With {.Dock = DockStyle.Top, .Height = 240, .Padding = New Padding(16)}

            Dim lblDbTitle As New Label() With {.Text = "Supabase PostgreSQL Database Connection", .Font = UITheme.FontSubtitle, .ForeColor = UITheme.PrimaryDark, .Dock = DockStyle.Top, .Height = 30}
            pnlDb.Controls.Add(lblDbTitle)

            Dim lblConn As New Label() With {.Text = "Npgsql Connection String / Supabase URI:", .Location = New Point(16, 45), .AutoSize = True, .Font = UITheme.FontBodyBold}
            pnlDb.Controls.Add(lblConn)

            txtConnString = New TextBox() With {
                .Location = New Point(16, 68),
                .Width = 650,
                .Text = AppConfiguration.Instance.ConnectionString,
                .Font = UITheme.FontBody
            }
            pnlDb.Controls.Add(txtConnString)

            btnTestConn = New Button() With {.Text = "Test Connection", .Location = New Point(16, 105), .Width = 140, .Height = 34}
            UITheme.StyleSecondaryButton(btnTestConn)
            AddHandler btnTestConn.Click, AddressOf BtnTestConn_Click
            pnlDb.Controls.Add(btnTestConn)

            btnSaveConn = New Button() With {.Text = "Save Connection", .Location = New Point(165, 105), .Width = 140, .Height = 34}
            UITheme.StylePrimaryButton(btnSaveConn)
            AddHandler btnSaveConn.Click, AddressOf BtnSaveConn_Click
            pnlDb.Controls.Add(btnSaveConn)

            btnInitSchema = New Button() With {.Text = "Provision / Reset Schema", .Location = New Point(315, 105), .Width = 190, .Height = 34}
            UITheme.StyleSecondaryButton(btnInitSchema)
            AddHandler btnInitSchema.Click, AddressOf BtnInitSchema_Click
            pnlDb.Controls.Add(btnInitSchema)

            lblStatus = New Label() With {
                .Text = "Connection Status: Ready",
                .Location = New Point(16, 160),
                .AutoSize = True,
                .ForeColor = UITheme.TextSecondary,
                .Font = UITheme.FontBodyBold
            }
            pnlDb.Controls.Add(lblStatus)

            Controls.Add(pnlDb)
            pnlDb.BringToFront()

            Dim pnlApp As New CardPanel() With {.Dock = DockStyle.Top, .Height = 200, .Padding = New Padding(16), .Margin = New Padding(0, 20, 0, 0)}

            Dim lblAppTitle As New Label() With {.Text = "Enterprise Application Preferences", .Font = UITheme.FontSubtitle, .ForeColor = UITheme.PrimaryDark, .Dock = DockStyle.Top, .Height = 30}
            pnlApp.Controls.Add(lblAppTitle)

            Dim lblCompany As New Label() With {.Text = "Registered Company Entity:", .Location = New Point(16, 45), .AutoSize = True, .Font = UITheme.FontBodyBold}
            pnlApp.Controls.Add(lblCompany)
            txtCompanyName = New TextBox() With {.Location = New Point(16, 68), .Width = 300, .Text = AppConfiguration.Instance.CompanyName, .Font = UITheme.FontBody}
            pnlApp.Controls.Add(txtCompanyName)

            Dim lblCurr As New Label() With {.Text = "Display Currency Symbol:", .Location = New Point(340, 45), .AutoSize = True, .Font = UITheme.FontBodyBold}
            pnlApp.Controls.Add(lblCurr)
            txtCurrencySymbol = New TextBox() With {.Location = New Point(340, 68), .Width = 100, .Text = AppConfiguration.Instance.CurrencySymbol, .Font = UITheme.FontBody}
            pnlApp.Controls.Add(txtCurrencySymbol)

            Controls.Add(pnlApp)
            pnlApp.BringToFront()
        End Sub

        Public Sub LoadDataAsync()
            txtConnString.Text = AppConfiguration.Instance.ConnectionString
            txtCompanyName.Text = AppConfiguration.Instance.CompanyName
            txtCurrencySymbol.Text = AppConfiguration.Instance.CurrencySymbol
        End Sub

        Private Async Sub BtnTestConn_Click(sender As Object, e As EventArgs)
            btnTestConn.Enabled = False
            lblStatus.Text = "Testing connection to PostgreSQL/Supabase database..."
            lblStatus.ForeColor = UITheme.Primary

            Dim res = Await DbConnectionFactory.TestConnectionAsync(txtConnString.Text.Trim())
            If res.Success Then
                lblStatus.Text = res.Message
                lblStatus.ForeColor = UITheme.Success
            Else
                lblStatus.Text = res.Message
                lblStatus.ForeColor = UITheme.Danger
            End If
            btnTestConn.Enabled = True
        End Sub

        Private Sub BtnSaveConn_Click(sender As Object, e As EventArgs)
            AppConfiguration.Instance.SaveConfiguration(txtConnString.Text.Trim(), txtCompanyName.Text.Trim(), txtCurrencySymbol.Text.Trim())
            MessageBox.Show("Configuration updated successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Sub

        Private Async Sub BtnInitSchema_Click(sender As Object, e As EventArgs)
            If MessageBox.Show("Are you sure you want to initialize/seed the database schema? This will ensure all tables, constraints, and default seed records are provisioned.", "Initialize Schema", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                btnInitSchema.Enabled = False
                lblStatus.Text = "Executing database DDL schema and seed migrations..."
                Dim res = Await DatabaseInitializer.ResetAndSeedDatabaseAsync()
                If res.Success Then
                    lblStatus.Text = res.Message
                    lblStatus.ForeColor = UITheme.Success
                    MessageBox.Show(res.Message, "Database Schema", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    lblStatus.Text = res.Message
                    lblStatus.ForeColor = UITheme.Danger
                    MessageBox.Show(res.Message, "Initialization Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
                btnInitSchema.Enabled = True
            End If
        End Sub
    End Class

End Namespace
