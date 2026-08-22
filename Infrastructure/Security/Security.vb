Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports MASA.InventoryManagementSystem.Domain.Entities

Namespace Infrastructure.Security

    Public Module PasswordHasher

        Public Function HashPassword(plainPassword As String) As String
            If String.IsNullOrEmpty(plainPassword) Then
                Throw New ArgumentException("Password cannot be empty.", NameOf(plainPassword))
            End If
            Return BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor:=11)
        End Function

        Public Function VerifyPassword(plainPassword As String, passwordHash As String) As Boolean
            If String.IsNullOrEmpty(plainPassword) OrElse String.IsNullOrEmpty(passwordHash) Then
                Return False
            End If
            Try
                Return BCrypt.Net.BCrypt.Verify(plainPassword, passwordHash)
            Catch ex As Exception
                Return False
            End Try
        End Function

    End Module

    Public Class SessionContext

        Private Shared _currentUser As User
        Private Shared ReadOnly _permissions As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

        Public Shared ReadOnly Property CurrentUser As User
            Get
                Return _currentUser
            End Get
        End Property

        Public Shared ReadOnly Property IsAuthenticated As Boolean
            Get
                Return _currentUser IsNot Nothing AndAlso _currentUser.IsActive
            End Get
        End Property

        Public Shared Sub SetSession(user As User, permissions As IEnumerable(Of String))
            _currentUser = user
            _permissions.Clear()
            If permissions IsNot Nothing Then
                For Each perm In permissions
                    _permissions.Add(perm)
                Next
            End If
        End Sub

        Public Shared Sub ClearSession()
            _currentUser = Nothing
            _permissions.Clear()
        End Sub

        Public Shared Function HasPermission(permissionCode As String) As Boolean
            If _currentUser Is Nothing Then Return False
            If _currentUser.RoleId = 1 OrElse String.Equals(_currentUser.RoleName, "Administrator", StringComparison.OrdinalIgnoreCase) Then
                Return True
            End If
            Return _permissions.Contains(permissionCode)
        End Function

        Public Shared ReadOnly Property Username As String
            Get
                Return If(_currentUser?.Username, "System")
            End Get
        End Property

        Public Shared ReadOnly Property RoleName As String
            Get
                Return If(_currentUser?.RoleName, "Guest")
            End Get
        End Property

    End Class

End Namespace
