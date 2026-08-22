Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

Namespace Presentation.Common

    Public Module UITheme

        Public ReadOnly Primary As Color = Color.FromArgb(37, 99, 235)
        Public ReadOnly PrimaryDark As Color = Color.FromArgb(29, 78, 216)
        Public ReadOnly PrimaryLight As Color = Color.FromArgb(239, 246, 255)
        Public ReadOnly Secondary As Color = Color.FromArgb(71, 85, 105)
        Public ReadOnly SidebarBg As Color = Color.FromArgb(15, 23, 42)
        Public ReadOnly SidebarHover As Color = Color.FromArgb(30, 41, 59)
        Public ReadOnly SidebarActive As Color = Color.FromArgb(37, 99, 235)
        Public ReadOnly TopBarBg As Color = Color.FromArgb(255, 255, 255)
        Public ReadOnly AppBackground As Color = Color.FromArgb(248, 250, 252)
        Public ReadOnly CardBackground As Color = Color.FromArgb(255, 255, 255)
        Public ReadOnly BorderColor As Color = Color.FromArgb(226, 232, 240)
        Public ReadOnly TextPrimary As Color = Color.FromArgb(15, 23, 42)
        Public ReadOnly TextSecondary As Color = Color.FromArgb(100, 116, 139)
        Public ReadOnly TextMuted As Color = Color.FromArgb(148, 163, 184)

        Public ReadOnly Success As Color = Color.FromArgb(16, 185, 129)
        Public ReadOnly SuccessBg As Color = Color.FromArgb(236, 253, 245)
        Public ReadOnly Warning As Color = Color.FromArgb(245, 158, 11)
        Public ReadOnly WarningBg As Color = Color.FromArgb(254, 252, 232)
        Public ReadOnly Danger As Color = Color.FromArgb(239, 68, 68)
        Public ReadOnly DangerBg As Color = Color.FromArgb(254, 242, 242)
        Public ReadOnly Info As Color = Color.FromArgb(6, 182, 212)

        Public ReadOnly FontHero As New Font("Segoe UI", 18.0F, FontStyle.Bold)
        Public ReadOnly FontTitle As New Font("Segoe UI", 14.0F, FontStyle.Bold)
        Public ReadOnly FontSubtitle As New Font("Segoe UI", 11.0F, FontStyle.Bold)
        Public ReadOnly FontBody As New Font("Segoe UI", 9.5F, FontStyle.Regular)
        Public ReadOnly FontBodyBold As New Font("Segoe UI", 9.5F, FontStyle.Bold)
        Public ReadOnly FontSmall As New Font("Segoe UI", 8.5F, FontStyle.Regular)
        Public ReadOnly FontSidebar As New Font("Segoe UI Semibold", 10.0F, FontStyle.Regular)

        Public Sub ApplyModernDataGridStyle(grid As DataGridView)
            grid.EnableHeadersVisualStyles = False
            grid.BackgroundColor = Color.White
            grid.BorderStyle = BorderStyle.None
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            grid.GridColor = BorderColor
            grid.RowHeadersVisible = False
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            grid.MultiSelect = False
            grid.AutoGenerateColumns = False
            grid.AllowUserToAddRows = False
            grid.AllowUserToDeleteRows = False
            grid.AllowUserToResizeRows = False
            grid.RowTemplate.Height = 38

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249)
            grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary
            grid.ColumnHeadersDefaultCellStyle.Font = New Font("Segoe UI", 9.5F, FontStyle.Bold)
            grid.ColumnHeadersDefaultCellStyle.Padding = New Padding(10, 8, 10, 8)
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
            grid.ColumnHeadersHeight = 42

            grid.DefaultCellStyle.BackColor = Color.White
            grid.DefaultCellStyle.ForeColor = TextPrimary
            grid.DefaultCellStyle.Font = FontBody
            grid.DefaultCellStyle.Padding = New Padding(10, 4, 10, 4)
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 242, 255)
            grid.DefaultCellStyle.SelectionForeColor = PrimaryDark

            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252)
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(238, 242, 255)
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = PrimaryDark
        End Sub

        Public Sub StylePrimaryButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.BackColor = Primary
            btn.ForeColor = Color.White
            btn.Font = FontBodyBold
            btn.Cursor = Cursors.Hand
            btn.Height = 36
            btn.Padding = New Padding(14, 0, 14, 0)
        End Sub

        Public Sub StyleSecondaryButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderColor = BorderColor
            btn.FlatAppearance.BorderSize = 1
            btn.BackColor = Color.White
            btn.ForeColor = TextPrimary
            btn.Font = FontBodyBold
            btn.Cursor = Cursors.Hand
            btn.Height = 36
            btn.Padding = New Padding(14, 0, 14, 0)
        End Sub

        Public Sub StyleSuccessButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.BackColor = Success
            btn.ForeColor = Color.White
            btn.Font = FontBodyBold
            btn.Cursor = Cursors.Hand
            btn.Height = 36
            btn.Padding = New Padding(14, 0, 14, 0)
        End Sub

        Public Sub StyleDangerButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.BackColor = Danger
            btn.ForeColor = Color.White
            btn.Font = FontBodyBold
            btn.Cursor = Cursors.Hand
            btn.Height = 36
            btn.Padding = New Padding(14, 0, 14, 0)
        End Sub

    End Module

End Namespace
