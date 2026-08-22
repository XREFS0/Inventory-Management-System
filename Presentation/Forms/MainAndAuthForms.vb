Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports MASA.InventoryManagementSystem.Application.Services
Imports MASA.InventoryManagementSystem.Domain.Entities
Imports MASA.InventoryManagementSystem.Infrastructure.Configuration
Imports MASA.InventoryManagementSystem.Infrastructure.Database
Imports MASA.InventoryManagementSystem.Infrastructure.Security
Imports MASA.InventoryManagementSystem.Presentation.Common
Imports MASA.InventoryManagementSystem.Presentation.Views

Namespace Presentation.Forms

    Public Class LoginForm
        Inherits Form

        Private ReadOnly _authService As New AuthService()

        Private txtUsername As TextBox
        Private txtPassword As TextBox
        Private chkShowPassword As CheckBox
        Private btnLogin As Button
        Private lblDbStatus As Label
        Private lblErrorMessage As Label

        Public Sub New()
            InitializeComponent()
            CheckDatabaseConnectionAsync()
        End Sub

        Private Sub InitializeComponent()
            Text = "MASA Inventory Management System - Authentication"
            Size = New Size(460, 580)
            StartPosition = FormStartPosition.CenterScreen
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.White
            Font = UITheme.FontBody

            Dim pnlBrand As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 120,
                .BackColor = UITheme.SidebarBg
            }

            Dim lblBrandName As New Label() With {
                .Text = "MASA INVENTORY",
                .Font = New Font("Segoe UI", 16.0F, FontStyle.Bold),
                .ForeColor = Color.White,
                .Location = New Point(32, 24),
                .AutoSize = True
            }
            pnlBrand.Controls.Add(lblBrandName)

            Dim lblBrandTagline As New Label() With {
                .Text = "Enterprise Warehouse & Stock Management",
                .Font = New Font("Segoe UI", 9.0F, FontStyle.Regular),
                .ForeColor = UITheme.TextMuted,
                .Location = New Point(32, 58),
                .AutoSize = True
            }
            pnlBrand.Controls.Add(lblBrandTagline)

            Controls.Add(pnlBrand)

            Dim top = 145
            Dim lblPrompt As New Label() With {
                .Text = "Sign in to access your inventory workspace",
                .Location = New Point(32, top),
                .AutoSize = True,
                .Font = UITheme.FontSubtitle,
                .ForeColor = UITheme.TextPrimary
            }
            Controls.Add(lblPrompt)

            top += 35
            Dim lblUser As New Label() With {.Text = "Username", .Location = New Point(32, top), .AutoSize = True, .Font = UITheme.FontBodyBold, .ForeColor = UITheme.TextSecondary}
            Controls.Add(lblUser)

            txtUsername = New TextBox() With {
                .Location = New Point(32, top + 20),
                .Width = 380,
                .Font = New Font("Segoe UI", 11.0F),
                .Text = "admin"
            }
            Controls.Add(txtUsername)

            top += 55
            Dim lblPass As New Label() With {.Text = "Password", .Location = New Point(32, top), .AutoSize = True, .Font = UITheme.FontBodyBold, .ForeColor = UITheme.TextSecondary}
            Controls.Add(lblPass)

            txtPassword = New TextBox() With {
                .Location = New Point(32, top + 20),
                .Width = 380,
                .UseSystemPasswordChar = True,
                .Font = New Font("Segoe UI", 11.0F),
                .Text = "Admin@123"
            }
            Controls.Add(txtPassword)

            top += 50
            chkShowPassword = New CheckBox() With {
                .Text = "Show Password",
                .Location = New Point(32, top),
                .AutoSize = True,
                .Font = UITheme.FontSmall,
                .ForeColor = UITheme.TextSecondary
            }
            AddHandler chkShowPassword.CheckedChanged, Sub() txtPassword.UseSystemPasswordChar = Not chkShowPassword.Checked
            Controls.Add(chkShowPassword)

            top += 25
            lblErrorMessage = New Label() With {
                .Text = "",
                .Location = New Point(32, top),
                .Size = New Size(380, 24),
                .ForeColor = UITheme.Danger,
                .Font = UITheme.FontSmall
            }
            Controls.Add(lblErrorMessage)

            top += 28
            btnLogin = New Button() With {
                .Text = "Sign In",
                .Location = New Point(32, top),
                .Width = 380,
                .Height = 40
            }
            UITheme.StylePrimaryButton(btnLogin)
            AddHandler btnLogin.Click, AddressOf BtnLogin_Click
            Controls.Add(btnLogin)

            top += 48
            Dim btnDbConfig As New Button() With {
                .Text = "Configure Database & Supabase Connection",
                .Location = New Point(32, top),
                .Width = 380,
                .Height = 32
            }
            UITheme.StyleSecondaryButton(btnDbConfig)
            AddHandler btnDbConfig.Click, AddressOf OpenDbConfigDialog
            Controls.Add(btnDbConfig)

            top += 38
            lblDbStatus = New Label() With {
                .Text = "Checking database connection...",
                .Location = New Point(32, top),
                .Size = New Size(380, 20),
                .TextAlign = ContentAlignment.MiddleCenter,
                .ForeColor = UITheme.TextMuted,
                .Font = UITheme.FontSmall,
                .Cursor = Cursors.Hand
            }
            AddHandler lblDbStatus.Click, AddressOf OpenDbConfigDialog
            Controls.Add(lblDbStatus)

            AcceptButton = btnLogin
        End Sub

        Private Sub OpenDbConfigDialog(sender As Object, e As EventArgs)
            Using dlg As New DatabaseConfigDialog()
                If dlg.ShowDialog() = DialogResult.OK Then
                    lblErrorMessage.Text = ""
                    CheckDatabaseConnectionAsync()
                End If
            End Using
        End Sub

        Public Async Sub CheckDatabaseConnectionAsync()
            Try
                Dim res = Await DbConnectionFactory.TestConnectionAsync()
                If res.Success Then
                    lblDbStatus.Text = "Database Connected (Ready)"
                    lblDbStatus.ForeColor = UITheme.Success
                Else
                    lblDbStatus.Text = "Database offline - Click to configure"
                    lblDbStatus.ForeColor = UITheme.Danger
                End If
            Catch
                lblDbStatus.Text = "Database error - Click to configure"
                lblDbStatus.ForeColor = UITheme.Danger
            End Try
        End Sub

        Private Async Sub BtnLogin_Click(sender As Object, e As EventArgs)
            lblErrorMessage.Text = ""
            Dim username = txtUsername.Text.Trim()
            Dim password = txtPassword.Text

            If String.IsNullOrWhiteSpace(username) OrElse String.IsNullOrWhiteSpace(password) Then
                lblErrorMessage.Text = "Please enter both username and password."
                Return
            End If

            btnLogin.Enabled = False
            btnLogin.Text = "Authenticating..."

            Try
                Dim res = Await _authService.LoginAsync(username, password)
                If res.Success Then
                    DialogResult = DialogResult.OK
                    Close()
                Else
                    lblErrorMessage.Text = res.Message
                End If
            Catch ex As Exception
                lblErrorMessage.Text = $"System Error: {ex.Message}"
            Finally
                btnLogin.Enabled = True
                btnLogin.Text = "Sign In"
            End Try
        End Sub
    End Class

    Public Class MainForm
        Inherits Form

        Private ReadOnly _authService As New AuthService()

        Private pnlTopBar As Panel
        Private pnlSidebar As Panel
        Private pnlMainContent As Panel
        Private lblCurrentUser As Label
        Private lblCurrentRole As Label

        Private ReadOnly _navButtons As New Dictionary(Of String, Button)()
        Private _currentViewControl As UserControl

        Private _viewDashboard As DashboardView
        Private _viewProducts As ProductsView
        Private _viewCategories As CategoriesView
        Private _viewWarehouses As WarehousesView
        Private _viewInventory As InventoryView
        Private _viewTransfers As TransfersView
        Private _viewSuppliers As SuppliersView
        Private _viewCustomers As CustomersView
        Private _viewPurchases As PurchasesView
        Private _viewSales As SalesView
        Private _viewReports As ReportsView
        Private _viewUsers As UsersView
        Private _viewAuditLogs As AuditLogsView
        Private _viewSettings As SettingsView

        Public Sub New()
            InitializeComponent()
            InitializeViews()
            NavigateTo("Dashboard")
        End Sub

        Private Sub InitializeComponent()
            Text = AppConfiguration.Instance.ApplicationName
            Size = New Size(1280, 800)
            MinimumSize = New Size(1024, 680)
            StartPosition = FormStartPosition.CenterScreen
            BackColor = UITheme.AppBackground
            Font = UITheme.FontBody

            pnlTopBar = New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 60,
                .BackColor = UITheme.TopBarBg,
                .Padding = New Padding(24, 0, 24, 0)
            }

            AddHandler pnlTopBar.Paint, Sub(s, e)
                                            Using p As New Pen(UITheme.BorderColor, 1)
                                                e.Graphics.DrawLine(p, 0, pnlTopBar.Height - 1, pnlTopBar.Width, pnlTopBar.Height - 1)
                                            End Using
                                        End Sub

            Dim lblAppTitle As New Label() With {
                .Text = "MASA Inventory Management System",
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.TextPrimary,
                .Location = New Point(24, 18),
                .AutoSize = True
            }
            pnlTopBar.Controls.Add(lblAppTitle)

            Dim btnLogout As New Button() With {
                .Text = "Log Out",
                .Size = New Size(85, 32),
                .Location = New Point(pnlTopBar.Width - 110, 14),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StyleSecondaryButton(btnLogout)
            AddHandler btnLogout.Click, AddressOf BtnLogout_Click
            pnlTopBar.Controls.Add(btnLogout)

            lblCurrentRole = New Label() With {
                .Text = SessionContext.RoleName,
                .Location = New Point(pnlTopBar.Width - 280, 32),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .Size = New Size(160, 18),
                .TextAlign = ContentAlignment.MiddleRight,
                .ForeColor = UITheme.TextSecondary,
                .Font = UITheme.FontSmall
            }
            pnlTopBar.Controls.Add(lblCurrentRole)

            lblCurrentUser = New Label() With {
                .Text = SessionContext.Username,
                .Location = New Point(pnlTopBar.Width - 280, 12),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .Size = New Size(160, 20),
                .TextAlign = ContentAlignment.MiddleRight,
                .ForeColor = UITheme.PrimaryDark,
                .Font = UITheme.FontBodyBold
            }
            pnlTopBar.Controls.Add(lblCurrentUser)

            Controls.Add(pnlTopBar)

            pnlSidebar = New Panel() With {
                .Dock = DockStyle.Left,
                .Width = 230,
                .BackColor = UITheme.SidebarBg,
                .AutoScroll = True,
                .Padding = New Padding(10, 15, 10, 15)
            }

            BuildSidebarNavigation()
            Controls.Add(pnlSidebar)

            pnlMainContent = New Panel() With {
                .Dock = DockStyle.Fill,
                .BackColor = UITheme.AppBackground
            }
            Controls.Add(pnlMainContent)
            pnlMainContent.BringToFront()
        End Sub

        Private Sub BuildSidebarNavigation()
            Dim navItems As New List(Of (Key As String, Title As String)) From {
                ("Dashboard", "Dashboard"),
                ("Products", "Products"),
                ("Categories", "Categories"),
                ("Warehouses", "Warehouses"),
                ("Inventory", "Inventory & Stock"),
                ("Transfers", "Stock Transfers"),
                ("Suppliers", "Suppliers"),
                ("Customers", "Customers"),
                ("Purchases", "Purchases (PO)"),
                ("Sales", "Sales Orders (SO)"),
                ("Reports", "Reports"),
                ("Users", "Users & Roles"),
                ("AuditLogs", "Audit Trail"),
                ("Settings", "Settings")
            }

            Dim top = 10
            For Each item In navItems
                Dim key = item.Key
                Dim btn As New Button() With {
                    .Text = $"   {item.Title}",
                    .TextAlign = ContentAlignment.MiddleLeft,
                    .Location = New Point(8, top),
                    .Size = New Size(200, 40),
                    .FlatStyle = FlatStyle.Flat,
                    .ForeColor = Color.FromArgb(203, 213, 225),
                    .BackColor = UITheme.SidebarBg,
                    .Font = UITheme.FontBodyBold,
                    .Cursor = Cursors.Hand
                }
                btn.FlatAppearance.BorderSize = 0

                AddHandler btn.Click, Sub(s, e) NavigateTo(key)
                pnlSidebar.Controls.Add(btn)
                _navButtons(key) = btn
                top += 44
            Next
        End Sub

        Private Sub InitializeViews()
            _viewDashboard = New DashboardView()
            _viewProducts = New ProductsView()
            _viewCategories = New CategoriesView()
            _viewWarehouses = New WarehousesView()
            _viewInventory = New InventoryView()
            _viewTransfers = New TransfersView()
            _viewSuppliers = New SuppliersView()
            _viewCustomers = New CustomersView()
            _viewPurchases = New PurchasesView()
            _viewSales = New SalesView()
            _viewReports = New ReportsView()
            _viewUsers = New UsersView()
            _viewAuditLogs = New AuditLogsView()
            _viewSettings = New SettingsView()
        End Sub

        Public Sub NavigateTo(viewKey As String)
            For Each kvp In _navButtons
                If kvp.Key = viewKey Then
                    kvp.Value.BackColor = UITheme.SidebarActive
                    kvp.Value.ForeColor = Color.White
                Else
                    kvp.Value.BackColor = UITheme.SidebarBg
                    kvp.Value.ForeColor = Color.FromArgb(203, 213, 225)
                End If
            Next

            pnlMainContent.SuspendLayout()
            pnlMainContent.Controls.Clear()

            Select Case viewKey
                Case "Dashboard"
                    _currentViewControl = _viewDashboard
                    _viewDashboard.LoadDataAsync()
                Case "Products"
                    _currentViewControl = _viewProducts
                    _viewProducts.LoadDataAsync()
                Case "Categories"
                    _currentViewControl = _viewCategories
                    _viewCategories.LoadDataAsync()
                Case "Warehouses"
                    _currentViewControl = _viewWarehouses
                    _viewWarehouses.LoadDataAsync()
                Case "Inventory"
                    _currentViewControl = _viewInventory
                    _viewInventory.LoadDataAsync()
                Case "Transfers"
                    _currentViewControl = _viewTransfers
                    _viewTransfers.LoadDataAsync()
                Case "Suppliers"
                    _currentViewControl = _viewSuppliers
                    _viewSuppliers.LoadDataAsync()
                Case "Customers"
                    _currentViewControl = _viewCustomers
                    _viewCustomers.LoadDataAsync()
                Case "Purchases"
                    _currentViewControl = _viewPurchases
                    _viewPurchases.LoadDataAsync()
                Case "Sales"
                    _currentViewControl = _viewSales
                    _viewSales.LoadDataAsync()
                Case "Reports"
                    _currentViewControl = _viewReports
                    _viewReports.LoadDataAsync()
                Case "Users"
                    _currentViewControl = _viewUsers
                    _viewUsers.LoadDataAsync()
                Case "AuditLogs"
                    _currentViewControl = _viewAuditLogs
                    _viewAuditLogs.LoadDataAsync()
                Case "Settings"
                    _currentViewControl = _viewSettings
                    _viewSettings.LoadDataAsync()
            End Select

            If _currentViewControl IsNot Nothing Then
                _currentViewControl.Dock = DockStyle.Fill
                pnlMainContent.Controls.Add(_currentViewControl)
            End If

            pnlMainContent.ResumeLayout()
        End Sub

        Private Sub BtnLogout_Click(sender As Object, e As EventArgs)
            If MessageBox.Show("Are you sure you want to log out?", "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                _authService.Logout()
                Hide()
                Using loginForm As New LoginForm()
                    If loginForm.ShowDialog() = DialogResult.OK Then
                        lblCurrentUser.Text = SessionContext.Username
                        lblCurrentRole.Text = SessionContext.RoleName
                        Show()
                        NavigateTo("Dashboard")
                    Else
                        Close()
                    End If
                End Using
            End If
        End Sub
    End Class

End Namespace
