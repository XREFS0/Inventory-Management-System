Option Strict On
Option Explicit On

Imports System
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports MASA.InventoryManagementSystem.Presentation.Common

Namespace Presentation.Controls

    Public Class CardPanel
        Inherits Panel

        Public Property BorderThickness As Integer = 1
        Public Property BorderDrawColor As Color = Color.FromArgb(226, 232, 240)
        Public Property CornerRadius As Integer = 8

        Public Sub New()
            DoubleBuffered = True
            BackColor = Color.White
            Padding = New Padding(16)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

            Dim rect = New Rectangle(0, 0, Width - 1, Height - 1)
            Using pen As New Pen(BorderDrawColor, BorderThickness)
                e.Graphics.DrawRectangle(pen, rect)
            End Using
        End Sub
    End Class

    Public Class StatCard
        Inherits UserControl

        Private _title As String = "Metric Title"
        Private _value As String = "0"
        Private _subtitle As String = "Real-time summary"
        Private _accentColor As Color = Color.FromArgb(37, 99, 235)

        Public Property CardTitle As String
            Get
                Return _title
            End Get
            Set(value As String)
                _title = value
                Invalidate()
            End Set
        End Property

        Public Property CardValue As String
            Get
                Return _value
            End Get
            Set(value As String)
                _value = value
                Invalidate()
            End Set
        End Property

        Public Property Subtitle As String
            Get
                Return _subtitle
            End Get
            Set(value As String)
                _subtitle = value
                Invalidate()
            End Set
        End Property

        Public Property AccentColor As Color
            Get
                Return _accentColor
            End Get
            Set(value As Color)
                _accentColor = value
                Invalidate()
            End Set
        End Property

        Public Sub New()
            DoubleBuffered = True
            BackColor = Color.White
            Size = New Size(240, 110)
            Padding = New Padding(16)
        End Sub

        Protected Overrides Sub OnPaint(e As PaintEventArgs)
            MyBase.OnPaint(e)
            Dim g = e.Graphics
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit

            Dim rect = New Rectangle(0, 0, Width - 1, Height - 1)
            Using pen As New Pen(UITheme.BorderColor, 1)
                g.DrawRectangle(pen, rect)
            End Using

            Using brush As New SolidBrush(_accentColor)
                g.FillRectangle(brush, 0, 0, 4, Height)
            End Using

            Using titleBrush As New SolidBrush(UITheme.TextSecondary)
                Using font As New Font("Segoe UI", 9.0F, FontStyle.Bold)
                    g.DrawString(_title.ToUpperInvariant(), font, titleBrush, 16, 14)
                End Using
            End Using

            Using valBrush As New SolidBrush(UITheme.TextPrimary)
                Using font As New Font("Segoe UI", 16.0F, FontStyle.Bold)
                    g.DrawString(_value, font, valBrush, 16, 36)
                End Using
            End Using

            Using subBrush As New SolidBrush(UITheme.TextMuted)
                Using font As New Font("Segoe UI", 8.5F, FontStyle.Regular)
                    g.DrawString(_subtitle, font, subBrush, 16, 75)
                End Using
            End Using
        End Sub
    End Class

End Namespace
