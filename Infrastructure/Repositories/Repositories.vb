Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.Common
Imports System.Threading.Tasks
Imports Npgsql
Imports MASA.InventoryManagementSystem.Domain.Entities
Imports MASA.InventoryManagementSystem.Domain.Enums
Imports MASA.InventoryManagementSystem.Infrastructure.Configuration
Imports MASA.InventoryManagementSystem.Infrastructure.Database

Namespace Infrastructure.Repositories

    Public Class UserRepository

        Public Async Function GetByUsernameAsync(username As String) As Task(Of User)
            Const query = "SELECT u.user_id, u.username, u.password_hash, u.full_name, u.email, u.phone, u.role_id, r.role_name, u.is_active, u.last_login_at, u.created_at, u.updated_at " &
                          "FROM users u INNER JOIN roles r ON u.role_id = r.role_id WHERE LOWER(u.username) = LOWER(@Username) LIMIT 1;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Username", username.Trim())
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        If Await reader.ReadAsync().ConfigureAwait(False) Then
                            Return MapUser(reader)
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Async Function GetByIdAsync(userId As Integer) As Task(Of User)
            Const query = "SELECT u.user_id, u.username, u.password_hash, u.full_name, u.email, u.phone, u.role_id, r.role_name, u.is_active, u.last_login_at, u.created_at, u.updated_at " &
                          "FROM users u INNER JOIN roles r ON u.role_id = r.role_id WHERE u.user_id = @UserId;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@UserId", userId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        If Await reader.ReadAsync().ConfigureAwait(False) Then
                            Return MapUser(reader)
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Async Function GetAllAsync() As Task(Of List(Of User))
            Dim list As New List(Of User)()
            Const query = "SELECT u.user_id, u.username, u.password_hash, u.full_name, u.email, u.phone, u.role_id, r.role_name, u.is_active, u.last_login_at, u.created_at, u.updated_at " &
                          "FROM users u INNER JOIN roles r ON u.role_id = r.role_id ORDER BY u.user_id ASC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(MapUser(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetUserPermissionsAsync(roleId As Integer) As Task(Of List(Of String))
            Dim permissions As New List(Of String)()
            Const query = "SELECT p.permission_code FROM permissions p " &
                          "INNER JOIN role_permissions rp ON p.permission_id = rp.permission_id " &
                          "WHERE rp.role_id = @RoleId;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@RoleId", roleId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            permissions.Add(reader.GetString(0))
                        End While
                    End Using
                End Using
            End Using
            Return permissions
        End Function

        Public Async Function GetAllRolesAsync() As Task(Of List(Of Role))
            Dim list As New List(Of Role)()
            Const query = "SELECT role_id, role_name, description, is_system, created_at FROM roles ORDER BY role_id ASC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(New Role() With {
                                .RoleId = reader.GetInt32(0),
                                .RoleName = reader.GetString(1),
                                .Description = If(reader.IsDBNull(2), String.Empty, reader.GetString(2)),
                                .IsSystem = reader.GetBoolean(3),
                                .CreatedAt = reader.GetDateTime(4)
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function InsertAsync(user As User) As Task(Of Integer)
            Const query = "INSERT INTO users (username, password_hash, full_name, email, phone, role_id, is_active, created_at, updated_at) " &
                          "VALUES (@Username, @PasswordHash, @FullName, @Email, @Phone, @RoleId, @IsActive, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) " &
                          "RETURNING user_id;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Username", user.Username)
                    cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash)
                    cmd.Parameters.AddWithValue("@FullName", user.FullName)
                    cmd.Parameters.AddWithValue("@Email", If(String.IsNullOrWhiteSpace(user.Email), DBNull.Value, CObj(user.Email)))
                    cmd.Parameters.AddWithValue("@Phone", If(String.IsNullOrWhiteSpace(user.Phone), DBNull.Value, CObj(user.Phone)))
                    cmd.Parameters.AddWithValue("@RoleId", user.RoleId)
                    cmd.Parameters.AddWithValue("@IsActive", user.IsActive)

                    Dim newId = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                    Return Convert.ToInt32(newId)
                End Using
            End Using
        End Function

        Public Async Function UpdateAsync(user As User) As Task(Of Boolean)
            Const query = "UPDATE users SET full_name = @FullName, email = @Email, phone = @Phone, role_id = @RoleId, is_active = @IsActive, updated_at = CURRENT_TIMESTAMP " &
                          "WHERE user_id = @UserId;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@FullName", user.FullName)
                    cmd.Parameters.AddWithValue("@Email", If(String.IsNullOrWhiteSpace(user.Email), DBNull.Value, CObj(user.Email)))
                    cmd.Parameters.AddWithValue("@Phone", If(String.IsNullOrWhiteSpace(user.Phone), DBNull.Value, CObj(user.Phone)))
                    cmd.Parameters.AddWithValue("@RoleId", user.RoleId)
                    cmd.Parameters.AddWithValue("@IsActive", user.IsActive)
                    cmd.Parameters.AddWithValue("@UserId", user.UserId)

                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Public Async Function UpdatePasswordAsync(userId As Integer, newPasswordHash As String) As Task(Of Boolean)
            Const query = "UPDATE users SET password_hash = @PasswordHash, updated_at = CURRENT_TIMESTAMP WHERE user_id = @UserId;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@PasswordHash", newPasswordHash)
                    cmd.Parameters.AddWithValue("@UserId", userId)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Public Async Function UpdateLastLoginAsync(userId As Integer) As Task
            Const query = "UPDATE users SET last_login_at = CURRENT_TIMESTAMP WHERE user_id = @UserId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@UserId", userId)
                    Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                End Using
            End Using
        End Function

        Private Shared Function MapUser(reader As DbDataReader) As User
            Return New User() With {
                .UserId = reader.GetInt32(0),
                .Username = reader.GetString(1),
                .PasswordHash = reader.GetString(2),
                .FullName = reader.GetString(3),
                .Email = If(reader.IsDBNull(4), String.Empty, reader.GetString(4)),
                .Phone = If(reader.IsDBNull(5), String.Empty, reader.GetString(5)),
                .RoleId = reader.GetInt32(6),
                .RoleName = reader.GetString(7),
                .IsActive = reader.GetBoolean(8),
                .LastLoginAt = If(reader.IsDBNull(9), CType(Nothing, Nullable(Of DateTime)), reader.GetDateTime(9)),
                .CreatedAt = reader.GetDateTime(10),
                .UpdatedAt = reader.GetDateTime(11)
            }
        End Function

    End Class

    Public Class CategoryRepository

        Public Async Function GetAllAsync(Optional onlyActive As Boolean = False) As Task(Of List(Of Category))
            Dim list As New List(Of Category)()
            Dim query = "SELECT c.category_id, c.name, c.code, c.description, c.is_active, c.created_at, c.updated_at, " &
                        "(SELECT COUNT(*) FROM products p WHERE p.category_id = c.category_id AND p.is_active = TRUE) AS product_count " &
                        "FROM categories c " &
                        If(onlyActive, "WHERE c.is_active = TRUE ", "") &
                        "ORDER BY c.name ASC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(New Category() With {
                                .CategoryId = reader.GetInt32(0),
                                .Name = reader.GetString(1),
                                .Code = If(reader.IsDBNull(2), String.Empty, reader.GetString(2)),
                                .Description = If(reader.IsDBNull(3), String.Empty, reader.GetString(3)),
                                .IsActive = reader.GetBoolean(4),
                                .CreatedAt = reader.GetDateTime(5),
                                .UpdatedAt = reader.GetDateTime(6),
                                .ProductCount = Convert.ToInt32(reader.GetInt64(7))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetByIdAsync(categoryId As Integer) As Task(Of Category)
            Const query = "SELECT category_id, name, code, description, is_active, created_at, updated_at FROM categories WHERE category_id = @Id;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Id", categoryId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        If Await reader.ReadAsync().ConfigureAwait(False) Then
                            Return New Category() With {
                                .CategoryId = reader.GetInt32(0),
                                .Name = reader.GetString(1),
                                .Code = If(reader.IsDBNull(2), String.Empty, reader.GetString(2)),
                                .Description = If(reader.IsDBNull(3), String.Empty, reader.GetString(3)),
                                .IsActive = reader.GetBoolean(4),
                                .CreatedAt = reader.GetDateTime(5),
                                .UpdatedAt = reader.GetDateTime(6)
                            }
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Async Function ExistsByNameAsync(name As String, Optional excludeId As Integer = 0) As Task(Of Boolean)
            Const query = "SELECT COUNT(*) FROM categories WHERE LOWER(name) = LOWER(@Name) AND category_id <> @ExcludeId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Name", name.Trim())
                    cmd.Parameters.AddWithValue("@ExcludeId", excludeId)
                    Dim count = Convert.ToInt32(Await cmd.ExecuteScalarAsync().ConfigureAwait(False))
                    Return count > 0
                End Using
            End Using
        End Function

        Public Async Function InsertAsync(category As Category) As Task(Of Integer)
            Const query = "INSERT INTO categories (name, code, description, is_active, created_at, updated_at) " &
                          "VALUES (@Name, @Code, @Description, @IsActive, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) RETURNING category_id;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Name", category.Name.Trim())
                    cmd.Parameters.AddWithValue("@Code", If(String.IsNullOrWhiteSpace(category.Code), DBNull.Value, CObj(category.Code.Trim())))
                    cmd.Parameters.AddWithValue("@Description", If(String.IsNullOrWhiteSpace(category.Description), DBNull.Value, CObj(category.Description)))
                    cmd.Parameters.AddWithValue("@IsActive", category.IsActive)
                    Dim newId = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                    Return Convert.ToInt32(newId)
                End Using
            End Using
        End Function

        Public Async Function UpdateAsync(category As Category) As Task(Of Boolean)
            Const query = "UPDATE categories SET name = @Name, code = @Code, description = @Description, is_active = @IsActive, updated_at = CURRENT_TIMESTAMP " &
                          "WHERE category_id = @CategoryId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Name", category.Name.Trim())
                    cmd.Parameters.AddWithValue("@Code", If(String.IsNullOrWhiteSpace(category.Code), DBNull.Value, CObj(category.Code.Trim())))
                    cmd.Parameters.AddWithValue("@Description", If(String.IsNullOrWhiteSpace(category.Description), DBNull.Value, CObj(category.Description)))
                    cmd.Parameters.AddWithValue("@IsActive", category.IsActive)
                    cmd.Parameters.AddWithValue("@CategoryId", category.CategoryId)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Public Async Function SoftDeleteAsync(categoryId As Integer) As Task(Of Boolean)
            Const query = "UPDATE categories SET is_active = FALSE, updated_at = CURRENT_TIMESTAMP WHERE category_id = @CategoryId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@CategoryId", categoryId)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

    End Class

    Public Class SupplierRepository

        Public Async Function GetAllAsync(Optional onlyActive As Boolean = False) As Task(Of List(Of Supplier))
            Dim list As New List(Of Supplier)()
            Dim query = "SELECT supplier_id, name, company_name, contact_person, email, phone, address, tax_number, notes, is_active, created_at, updated_at " &
                        "FROM suppliers " &
                        If(onlyActive, "WHERE is_active = TRUE ", "") &
                        "ORDER BY name ASC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(MapSupplier(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetByIdAsync(supplierId As Integer) As Task(Of Supplier)
            Const query = "SELECT supplier_id, name, company_name, contact_person, email, phone, address, tax_number, notes, is_active, created_at, updated_at " &
                          "FROM suppliers WHERE supplier_id = @Id;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Id", supplierId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        If Await reader.ReadAsync().ConfigureAwait(False) Then
                            Return MapSupplier(reader)
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Async Function InsertAsync(s As Supplier) As Task(Of Integer)
            Const query = "INSERT INTO suppliers (name, company_name, contact_person, email, phone, address, tax_number, notes, is_active, created_at, updated_at) " &
                          "VALUES (@Name, @CompanyName, @ContactPerson, @Email, @Phone, @Address, @TaxNumber, @Notes, @IsActive, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) RETURNING supplier_id;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Name", s.Name.Trim())
                    cmd.Parameters.AddWithValue("@CompanyName", If(String.IsNullOrWhiteSpace(s.CompanyName), DBNull.Value, CObj(s.CompanyName.Trim())))
                    cmd.Parameters.AddWithValue("@ContactPerson", If(String.IsNullOrWhiteSpace(s.ContactPerson), DBNull.Value, CObj(s.ContactPerson.Trim())))
                    cmd.Parameters.AddWithValue("@Email", If(String.IsNullOrWhiteSpace(s.Email), DBNull.Value, CObj(s.Email.Trim())))
                    cmd.Parameters.AddWithValue("@Phone", If(String.IsNullOrWhiteSpace(s.Phone), DBNull.Value, CObj(s.Phone.Trim())))
                    cmd.Parameters.AddWithValue("@Address", If(String.IsNullOrWhiteSpace(s.Address), DBNull.Value, CObj(s.Address.Trim())))
                    cmd.Parameters.AddWithValue("@TaxNumber", If(String.IsNullOrWhiteSpace(s.TaxNumber), DBNull.Value, CObj(s.TaxNumber.Trim())))
                    cmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(s.Notes), DBNull.Value, CObj(s.Notes)))
                    cmd.Parameters.AddWithValue("@IsActive", s.IsActive)
                    Dim newId = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                    Return Convert.ToInt32(newId)
                End Using
            End Using
        End Function

        Public Async Function UpdateAsync(s As Supplier) As Task(Of Boolean)
            Const query = "UPDATE suppliers SET name = @Name, company_name = @CompanyName, contact_person = @ContactPerson, email = @Email, " &
                          "phone = @Phone, address = @Address, tax_number = @TaxNumber, notes = @Notes, is_active = @IsActive, updated_at = CURRENT_TIMESTAMP " &
                          "WHERE supplier_id = @SupplierId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Name", s.Name.Trim())
                    cmd.Parameters.AddWithValue("@CompanyName", If(String.IsNullOrWhiteSpace(s.CompanyName), DBNull.Value, CObj(s.CompanyName.Trim())))
                    cmd.Parameters.AddWithValue("@ContactPerson", If(String.IsNullOrWhiteSpace(s.ContactPerson), DBNull.Value, CObj(s.ContactPerson.Trim())))
                    cmd.Parameters.AddWithValue("@Email", If(String.IsNullOrWhiteSpace(s.Email), DBNull.Value, CObj(s.Email.Trim())))
                    cmd.Parameters.AddWithValue("@Phone", If(String.IsNullOrWhiteSpace(s.Phone), DBNull.Value, CObj(s.Phone.Trim())))
                    cmd.Parameters.AddWithValue("@Address", If(String.IsNullOrWhiteSpace(s.Address), DBNull.Value, CObj(s.Address.Trim())))
                    cmd.Parameters.AddWithValue("@TaxNumber", If(String.IsNullOrWhiteSpace(s.TaxNumber), DBNull.Value, CObj(s.TaxNumber.Trim())))
                    cmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(s.Notes), DBNull.Value, CObj(s.Notes)))
                    cmd.Parameters.AddWithValue("@IsActive", s.IsActive)
                    cmd.Parameters.AddWithValue("@SupplierId", s.SupplierId)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Public Async Function SoftDeleteAsync(supplierId As Integer) As Task(Of Boolean)
            Const query = "UPDATE suppliers SET is_active = FALSE, updated_at = CURRENT_TIMESTAMP WHERE supplier_id = @SupplierId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@SupplierId", supplierId)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Private Shared Function MapSupplier(reader As DbDataReader) As Supplier
            Return New Supplier() With {
                .SupplierId = reader.GetInt32(0),
                .Name = reader.GetString(1),
                .CompanyName = If(reader.IsDBNull(2), String.Empty, reader.GetString(2)),
                .ContactPerson = If(reader.IsDBNull(3), String.Empty, reader.GetString(3)),
                .Email = If(reader.IsDBNull(4), String.Empty, reader.GetString(4)),
                .Phone = If(reader.IsDBNull(5), String.Empty, reader.GetString(5)),
                .Address = If(reader.IsDBNull(6), String.Empty, reader.GetString(6)),
                .TaxNumber = If(reader.IsDBNull(7), String.Empty, reader.GetString(7)),
                .Notes = If(reader.IsDBNull(8), String.Empty, reader.GetString(8)),
                .IsActive = reader.GetBoolean(9),
                .CreatedAt = reader.GetDateTime(10),
                .UpdatedAt = reader.GetDateTime(11)
            }
        End Function

    End Class

    Public Class CustomerRepository

        Public Async Function GetAllAsync(Optional onlyActive As Boolean = False) As Task(Of List(Of Customer))
            Dim list As New List(Of Customer)()
            Dim query = "SELECT customer_id, full_name, company_name, email, phone, address, tax_number, notes, is_active, created_at, updated_at " &
                        "FROM customers " &
                        If(onlyActive, "WHERE is_active = TRUE ", "") &
                        "ORDER BY full_name ASC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(MapCustomer(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetByIdAsync(customerId As Integer) As Task(Of Customer)
            Const query = "SELECT customer_id, full_name, company_name, email, phone, address, tax_number, notes, is_active, created_at, updated_at " &
                          "FROM customers WHERE customer_id = @Id;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Id", customerId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        If Await reader.ReadAsync().ConfigureAwait(False) Then
                            Return MapCustomer(reader)
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Async Function InsertAsync(c As Customer) As Task(Of Integer)
            Const query = "INSERT INTO customers (full_name, company_name, email, phone, address, tax_number, notes, is_active, created_at, updated_at) " &
                          "VALUES (@FullName, @CompanyName, @Email, @Phone, @Address, @TaxNumber, @Notes, @IsActive, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) RETURNING customer_id;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@FullName", c.FullName.Trim())
                    cmd.Parameters.AddWithValue("@CompanyName", If(String.IsNullOrWhiteSpace(c.CompanyName), DBNull.Value, CObj(c.CompanyName.Trim())))
                    cmd.Parameters.AddWithValue("@Email", If(String.IsNullOrWhiteSpace(c.Email), DBNull.Value, CObj(c.Email.Trim())))
                    cmd.Parameters.AddWithValue("@Phone", If(String.IsNullOrWhiteSpace(c.Phone), DBNull.Value, CObj(c.Phone.Trim())))
                    cmd.Parameters.AddWithValue("@Address", If(String.IsNullOrWhiteSpace(c.Address), DBNull.Value, CObj(c.Address.Trim())))
                    cmd.Parameters.AddWithValue("@TaxNumber", If(String.IsNullOrWhiteSpace(c.TaxNumber), DBNull.Value, CObj(c.TaxNumber.Trim())))
                    cmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(c.Notes), DBNull.Value, CObj(c.Notes)))
                    cmd.Parameters.AddWithValue("@IsActive", c.IsActive)
                    Dim newId = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                    Return Convert.ToInt32(newId)
                End Using
            End Using
        End Function

        Public Async Function UpdateAsync(c As Customer) As Task(Of Boolean)
            Const query = "UPDATE customers SET full_name = @FullName, company_name = @CompanyName, email = @Email, " &
                          "phone = @Phone, address = @Address, tax_number = @TaxNumber, notes = @Notes, is_active = @IsActive, updated_at = CURRENT_TIMESTAMP " &
                          "WHERE customer_id = @CustomerId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@FullName", c.FullName.Trim())
                    cmd.Parameters.AddWithValue("@CompanyName", If(String.IsNullOrWhiteSpace(c.CompanyName), DBNull.Value, CObj(c.CompanyName.Trim())))
                    cmd.Parameters.AddWithValue("@Email", If(String.IsNullOrWhiteSpace(c.Email), DBNull.Value, CObj(c.Email.Trim())))
                    cmd.Parameters.AddWithValue("@Phone", If(String.IsNullOrWhiteSpace(c.Phone), DBNull.Value, CObj(c.Phone.Trim())))
                    cmd.Parameters.AddWithValue("@Address", If(String.IsNullOrWhiteSpace(c.Address), DBNull.Value, CObj(c.Address.Trim())))
                    cmd.Parameters.AddWithValue("@TaxNumber", If(String.IsNullOrWhiteSpace(c.TaxNumber), DBNull.Value, CObj(c.TaxNumber.Trim())))
                    cmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(c.Notes), DBNull.Value, CObj(c.Notes)))
                    cmd.Parameters.AddWithValue("@IsActive", c.IsActive)
                    cmd.Parameters.AddWithValue("@CustomerId", c.CustomerId)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Public Async Function SoftDeleteAsync(customerId As Integer) As Task(Of Boolean)
            Const query = "UPDATE customers SET is_active = FALSE, updated_at = CURRENT_TIMESTAMP WHERE customer_id = @CustomerId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@CustomerId", customerId)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Private Shared Function MapCustomer(reader As DbDataReader) As Customer
            Return New Customer() With {
                .CustomerId = reader.GetInt32(0),
                .FullName = reader.GetString(1),
                .CompanyName = If(reader.IsDBNull(2), String.Empty, reader.GetString(2)),
                .Email = If(reader.IsDBNull(3), String.Empty, reader.GetString(3)),
                .Phone = If(reader.IsDBNull(4), String.Empty, reader.GetString(4)),
                .Address = If(reader.IsDBNull(5), String.Empty, reader.GetString(5)),
                .TaxNumber = If(reader.IsDBNull(6), String.Empty, reader.GetString(6)),
                .Notes = If(reader.IsDBNull(7), String.Empty, reader.GetString(7)),
                .IsActive = reader.GetBoolean(8),
                .CreatedAt = reader.GetDateTime(9),
                .UpdatedAt = reader.GetDateTime(10)
            }
        End Function

    End Class

    Public Class WarehouseRepository

        Public Async Function GetAllAsync(Optional onlyActive As Boolean = False) As Task(Of List(Of Warehouse))
            Dim list As New List(Of Warehouse)()
            Dim query = "SELECT w.warehouse_id, w.name, w.code, w.location, w.manager_name, w.contact_phone, w.is_active, w.created_at, w.updated_at, " &
                        "COALESCE(SUM(ws.quantity), 0) AS total_stock, " &
                        "COALESCE(SUM(ws.quantity * p.cost_price), 0.00) AS total_value " &
                        "FROM warehouses w " &
                        "LEFT JOIN warehouse_stock ws ON w.warehouse_id = ws.warehouse_id " &
                        "LEFT JOIN products p ON ws.product_id = p.product_id " &
                        If(onlyActive, "WHERE w.is_active = TRUE ", "") &
                        "GROUP BY w.warehouse_id, w.name, w.code, w.location, w.manager_name, w.contact_phone, w.is_active, w.created_at, w.updated_at " &
                        "ORDER BY w.name ASC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(New Warehouse() With {
                                .WarehouseId = reader.GetInt32(0),
                                .Name = reader.GetString(1),
                                .Code = reader.GetString(2),
                                .Location = If(reader.IsDBNull(3), String.Empty, reader.GetString(3)),
                                .ManagerName = If(reader.IsDBNull(4), String.Empty, reader.GetString(4)),
                                .ContactPhone = If(reader.IsDBNull(5), String.Empty, reader.GetString(5)),
                                .IsActive = reader.GetBoolean(6),
                                .CreatedAt = reader.GetDateTime(7),
                                .UpdatedAt = reader.GetDateTime(8),
                                .TotalStockCount = Convert.ToInt32(reader.GetInt64(9)),
                                .TotalInventoryValue = reader.GetDecimal(10)
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetByIdAsync(warehouseId As Integer) As Task(Of Warehouse)
            Const query = "SELECT warehouse_id, name, code, location, manager_name, contact_phone, is_active, created_at, updated_at FROM warehouses WHERE warehouse_id = @Id;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Id", warehouseId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        If Await reader.ReadAsync().ConfigureAwait(False) Then
                            Return New Warehouse() With {
                                .WarehouseId = reader.GetInt32(0),
                                .Name = reader.GetString(1),
                                .Code = reader.GetString(2),
                                .Location = If(reader.IsDBNull(3), String.Empty, reader.GetString(3)),
                                .ManagerName = If(reader.IsDBNull(4), String.Empty, reader.GetString(4)),
                                .ContactPhone = If(reader.IsDBNull(5), String.Empty, reader.GetString(5)),
                                .IsActive = reader.GetBoolean(6),
                                .CreatedAt = reader.GetDateTime(7),
                                .UpdatedAt = reader.GetDateTime(8)
                            }
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Async Function ExistsByCodeAsync(code As String, Optional excludeId As Integer = 0) As Task(Of Boolean)
            Const query = "SELECT COUNT(*) FROM warehouses WHERE LOWER(code) = LOWER(@Code) AND warehouse_id <> @ExcludeId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Code", code.Trim())
                    cmd.Parameters.AddWithValue("@ExcludeId", excludeId)
                    Dim count = Convert.ToInt32(Await cmd.ExecuteScalarAsync().ConfigureAwait(False))
                    Return count > 0
                End Using
            End Using
        End Function

        Public Async Function InsertAsync(w As Warehouse) As Task(Of Integer)
            Const query = "INSERT INTO warehouses (name, code, location, manager_name, contact_phone, is_active, created_at, updated_at) " &
                          "VALUES (@Name, @Code, @Location, @ManagerName, @ContactPhone, @IsActive, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) RETURNING warehouse_id;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Name", w.Name.Trim())
                    cmd.Parameters.AddWithValue("@Code", w.Code.Trim().ToUpperInvariant())
                    cmd.Parameters.AddWithValue("@Location", If(String.IsNullOrWhiteSpace(w.Location), DBNull.Value, CObj(w.Location.Trim())))
                    cmd.Parameters.AddWithValue("@ManagerName", If(String.IsNullOrWhiteSpace(w.ManagerName), DBNull.Value, CObj(w.ManagerName.Trim())))
                    cmd.Parameters.AddWithValue("@ContactPhone", If(String.IsNullOrWhiteSpace(w.ContactPhone), DBNull.Value, CObj(w.ContactPhone.Trim())))
                    cmd.Parameters.AddWithValue("@IsActive", w.IsActive)
                    Dim newId = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                    Return Convert.ToInt32(newId)
                End Using
            End Using
        End Function

        Public Async Function UpdateAsync(w As Warehouse) As Task(Of Boolean)
            Const query = "UPDATE warehouses SET name = @Name, code = @Code, location = @Location, manager_name = @ManagerName, " &
                          "contact_phone = @ContactPhone, is_active = @IsActive, updated_at = CURRENT_TIMESTAMP WHERE warehouse_id = @WarehouseId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Name", w.Name.Trim())
                    cmd.Parameters.AddWithValue("@Code", w.Code.Trim().ToUpperInvariant())
                    cmd.Parameters.AddWithValue("@Location", If(String.IsNullOrWhiteSpace(w.Location), DBNull.Value, CObj(w.Location.Trim())))
                    cmd.Parameters.AddWithValue("@ManagerName", If(String.IsNullOrWhiteSpace(w.ManagerName), DBNull.Value, CObj(w.ManagerName.Trim())))
                    cmd.Parameters.AddWithValue("@ContactPhone", If(String.IsNullOrWhiteSpace(w.ContactPhone), DBNull.Value, CObj(w.ContactPhone.Trim())))
                    cmd.Parameters.AddWithValue("@IsActive", w.IsActive)
                    cmd.Parameters.AddWithValue("@WarehouseId", w.WarehouseId)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Public Async Function SoftDeleteAsync(warehouseId As Integer) As Task(Of Boolean)
            Const query = "UPDATE warehouses SET is_active = FALSE, updated_at = CURRENT_TIMESTAMP WHERE warehouse_id = @WarehouseId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@WarehouseId", warehouseId)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

    End Class

    Public Class ProductRepository

        Public Async Function SearchAsync(searchTerm As String, categoryId As Nullable(Of Integer), supplierId As Nullable(Of Integer), onlyActive As Boolean) As Task(Of List(Of Product))
            Dim list As New List(Of Product)()
            Dim query = "SELECT p.product_id, p.sku, p.barcode, p.name, p.description, p.category_id, c.name AS category_name, " &
                        "p.supplier_id, s.name AS supplier_name, p.cost_price, p.selling_price, p.min_stock_level, p.unit_of_measure, " &
                        "p.is_active, p.created_at, p.updated_at, " &
                        "COALESCE((SELECT SUM(ws.quantity) FROM warehouse_stock ws WHERE ws.product_id = p.product_id), 0) AS total_stock " &
                        "FROM products p " &
                        "LEFT JOIN categories c ON p.category_id = c.category_id " &
                        "LEFT JOIN suppliers s ON p.supplier_id = s.supplier_id " &
                        "WHERE 1=1 "

            If onlyActive Then
                query &= "AND p.is_active = TRUE "
            End If
            If categoryId.HasValue AndAlso categoryId.Value > 0 Then
                query &= "AND p.category_id = @CategoryId "
            End If
            If supplierId.HasValue AndAlso supplierId.Value > 0 Then
                query &= "AND p.supplier_id = @SupplierId "
            End If
            If Not String.IsNullOrWhiteSpace(searchTerm) Then
                query &= "AND (p.sku ILIKE @Search OR p.name ILIKE @Search OR p.barcode ILIKE @Search) "
            End If

            query &= "ORDER BY p.name ASC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    If categoryId.HasValue AndAlso categoryId.Value > 0 Then
                        cmd.Parameters.AddWithValue("@CategoryId", categoryId.Value)
                    End If
                    If supplierId.HasValue AndAlso supplierId.Value > 0 Then
                        cmd.Parameters.AddWithValue("@SupplierId", supplierId.Value)
                    End If
                    If Not String.IsNullOrWhiteSpace(searchTerm) Then
                        cmd.Parameters.AddWithValue("@Search", $"%{searchTerm.Trim()}%")
                    End If

                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(MapProduct(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetByIdAsync(productId As Integer) As Task(Of Product)
            Const query = "SELECT p.product_id, p.sku, p.barcode, p.name, p.description, p.category_id, c.name AS category_name, " &
                          "p.supplier_id, s.name AS supplier_name, p.cost_price, p.selling_price, p.min_stock_level, p.unit_of_measure, " &
                          "p.is_active, p.created_at, p.updated_at, " &
                          "COALESCE((SELECT SUM(ws.quantity) FROM warehouse_stock ws WHERE ws.product_id = p.product_id), 0) AS total_stock " &
                          "FROM products p " &
                          "LEFT JOIN categories c ON p.category_id = c.category_id " &
                          "LEFT JOIN suppliers s ON p.supplier_id = s.supplier_id " &
                          "WHERE p.product_id = @ProductId;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@ProductId", productId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        If Await reader.ReadAsync().ConfigureAwait(False) Then
                            Return MapProduct(reader)
                        End If
                    End Using
                End Using
            End Using
            Return Nothing
        End Function

        Public Async Function ExistsBySkuAsync(sku As String, Optional excludeId As Integer = 0) As Task(Of Boolean)
            Const query = "SELECT COUNT(*) FROM products WHERE LOWER(sku) = LOWER(@Sku) AND product_id <> @ExcludeId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Sku", sku.Trim())
                    cmd.Parameters.AddWithValue("@ExcludeId", excludeId)
                    Dim count = Convert.ToInt32(Await cmd.ExecuteScalarAsync().ConfigureAwait(False))
                    Return count > 0
                End Using
            End Using
        End Function

        Public Async Function InsertAsync(p As Product) As Task(Of Integer)
            Const query = "INSERT INTO products (sku, barcode, name, description, category_id, supplier_id, cost_price, selling_price, min_stock_level, unit_of_measure, is_active, created_at, updated_at) " &
                          "VALUES (@Sku, @Barcode, @Name, @Description, @CategoryId, @SupplierId, @CostPrice, @SellingPrice, @MinStockLevel, @UnitOfMeasure, @IsActive, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) RETURNING product_id;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Sku", p.Sku.Trim().ToUpperInvariant())
                    cmd.Parameters.AddWithValue("@Barcode", If(String.IsNullOrWhiteSpace(p.Barcode), DBNull.Value, CObj(p.Barcode.Trim())))
                    cmd.Parameters.AddWithValue("@Name", p.Name.Trim())
                    cmd.Parameters.AddWithValue("@Description", If(String.IsNullOrWhiteSpace(p.Description), DBNull.Value, CObj(p.Description)))
                    cmd.Parameters.AddWithValue("@CategoryId", If(p.CategoryId.HasValue AndAlso p.CategoryId.Value > 0, CObj(p.CategoryId.Value), DBNull.Value))
                    cmd.Parameters.AddWithValue("@SupplierId", If(p.SupplierId.HasValue AndAlso p.SupplierId.Value > 0, CObj(p.SupplierId.Value), DBNull.Value))
                    cmd.Parameters.AddWithValue("@CostPrice", p.CostPrice)
                    cmd.Parameters.AddWithValue("@SellingPrice", p.SellingPrice)
                    cmd.Parameters.AddWithValue("@MinStockLevel", p.MinStockLevel)
                    cmd.Parameters.AddWithValue("@UnitOfMeasure", If(String.IsNullOrWhiteSpace(p.UnitOfMeasure), "Units", p.UnitOfMeasure.Trim()))
                    cmd.Parameters.AddWithValue("@IsActive", p.IsActive)

                    Dim newId = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                    Return Convert.ToInt32(newId)
                End Using
            End Using
        End Function

        Public Async Function UpdateAsync(p As Product) As Task(Of Boolean)
            Const query = "UPDATE products SET sku = @Sku, barcode = @Barcode, name = @Name, description = @Description, " &
                          "category_id = @CategoryId, supplier_id = @SupplierId, cost_price = @CostPrice, selling_price = @SellingPrice, " &
                          "min_stock_level = @MinStockLevel, unit_of_measure = @UnitOfMeasure, is_active = @IsActive, updated_at = CURRENT_TIMESTAMP " &
                          "WHERE product_id = @ProductId;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Sku", p.Sku.Trim().ToUpperInvariant())
                    cmd.Parameters.AddWithValue("@Barcode", If(String.IsNullOrWhiteSpace(p.Barcode), DBNull.Value, CObj(p.Barcode.Trim())))
                    cmd.Parameters.AddWithValue("@Name", p.Name.Trim())
                    cmd.Parameters.AddWithValue("@Description", If(String.IsNullOrWhiteSpace(p.Description), DBNull.Value, CObj(p.Description)))
                    cmd.Parameters.AddWithValue("@CategoryId", If(p.CategoryId.HasValue AndAlso p.CategoryId.Value > 0, CObj(p.CategoryId.Value), DBNull.Value))
                    cmd.Parameters.AddWithValue("@SupplierId", If(p.SupplierId.HasValue AndAlso p.SupplierId.Value > 0, CObj(p.SupplierId.Value), DBNull.Value))
                    cmd.Parameters.AddWithValue("@CostPrice", p.CostPrice)
                    cmd.Parameters.AddWithValue("@SellingPrice", p.SellingPrice)
                    cmd.Parameters.AddWithValue("@MinStockLevel", p.MinStockLevel)
                    cmd.Parameters.AddWithValue("@UnitOfMeasure", If(String.IsNullOrWhiteSpace(p.UnitOfMeasure), "Units", p.UnitOfMeasure.Trim()))
                    cmd.Parameters.AddWithValue("@IsActive", p.IsActive)
                    cmd.Parameters.AddWithValue("@ProductId", p.ProductId)

                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Public Async Function SoftDeleteAsync(productId As Integer) As Task(Of Boolean)
            Const query = "UPDATE products SET is_active = FALSE, updated_at = CURRENT_TIMESTAMP WHERE product_id = @ProductId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@ProductId", productId)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Private Shared Function MapProduct(reader As DbDataReader) As Product
            Return New Product() With {
                .ProductId = reader.GetInt32(0),
                .Sku = reader.GetString(1),
                .Barcode = If(reader.IsDBNull(2), String.Empty, reader.GetString(2)),
                .Name = reader.GetString(3),
                .Description = If(reader.IsDBNull(4), String.Empty, reader.GetString(4)),
                .CategoryId = If(reader.IsDBNull(5), CType(Nothing, Nullable(Of Integer)), reader.GetInt32(5)),
                .CategoryName = If(reader.IsDBNull(6), "Uncategorized", reader.GetString(6)),
                .SupplierId = If(reader.IsDBNull(7), CType(Nothing, Nullable(Of Integer)), reader.GetInt32(7)),
                .SupplierName = If(reader.IsDBNull(8), "No Supplier", reader.GetString(8)),
                .CostPrice = reader.GetDecimal(9),
                .SellingPrice = reader.GetDecimal(10),
                .MinStockLevel = reader.GetInt32(11),
                .UnitOfMeasure = If(reader.IsDBNull(12), "Units", reader.GetString(12)),
                .IsActive = reader.GetBoolean(13),
                .CreatedAt = reader.GetDateTime(14),
                .UpdatedAt = reader.GetDateTime(15),
                .TotalStock = Convert.ToInt32(reader.GetInt64(16))
            }
        End Function

    End Class

    Public Class InventoryRepository

        Public Async Function GetStockByWarehouseAsync(warehouseId As Integer) As Task(Of List(Of WarehouseStock))
            Dim list As New List(Of WarehouseStock)()
            Const query = "SELECT ws.stock_id, ws.warehouse_id, w.name AS warehouse_name, ws.product_id, p.name AS product_name, " &
                          "p.sku AS product_sku, ws.quantity, p.min_stock_level, p.cost_price, p.selling_price, ws.last_updated_at " &
                          "FROM warehouse_stock ws " &
                          "INNER JOIN warehouses w ON ws.warehouse_id = w.warehouse_id " &
                          "INNER JOIN products p ON ws.product_id = p.product_id " &
                          "WHERE ws.warehouse_id = @WarehouseId " &
                          "ORDER BY p.name ASC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@WarehouseId", warehouseId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(MapWarehouseStock(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetAllWarehouseStockBreakdownAsync(productId As Integer) As Task(Of List(Of WarehouseStock))
            Dim list As New List(Of WarehouseStock)()
            Const query = "SELECT ws.stock_id, ws.warehouse_id, w.name AS warehouse_name, ws.product_id, p.name AS product_name, " &
                          "p.sku AS product_sku, ws.quantity, p.min_stock_level, p.cost_price, p.selling_price, ws.last_updated_at " &
                          "FROM warehouses w " &
                          "CROSS JOIN products p " &
                          "LEFT JOIN warehouse_stock ws ON w.warehouse_id = ws.warehouse_id AND p.product_id = ws.product_id " &
                          "WHERE p.product_id = @ProductId AND w.is_active = TRUE " &
                          "ORDER BY w.name ASC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@ProductId", productId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(New WarehouseStock() With {
                                .StockId = If(reader.IsDBNull(0), 0, reader.GetInt32(0)),
                                .WarehouseId = reader.GetInt32(1),
                                .WarehouseName = reader.GetString(2),
                                .ProductId = reader.GetInt32(3),
                                .ProductName = reader.GetString(4),
                                .ProductSku = reader.GetString(5),
                                .Quantity = If(reader.IsDBNull(6), 0, reader.GetInt32(6)),
                                .MinStockLevel = reader.GetInt32(7),
                                .CostPrice = reader.GetDecimal(8),
                                .SellingPrice = reader.GetDecimal(9),
                                .LastUpdatedAt = If(reader.IsDBNull(10), DateTime.UtcNow, reader.GetDateTime(10))
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetQuantityAsync(warehouseId As Integer, productId As Integer) As Task(Of Integer)
            Const query = "SELECT quantity FROM warehouse_stock WHERE warehouse_id = @WarehouseId AND product_id = @ProductId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@WarehouseId", warehouseId)
                    cmd.Parameters.AddWithValue("@ProductId", productId)
                    Dim obj = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                    If obj IsNot Nothing AndAlso Not Convert.IsDBNull(obj) Then
                        Return Convert.ToInt32(obj)
                    End If
                    Return 0
                End Using
            End Using
        End Function

        Public Async Function AdjustStockAsync(productId As Integer, warehouseId As Integer, transactionType As TransactionType, deltaQuantity As Integer, referenceType As String, referenceId As String, notes As String, username As String) As Task(Of Boolean)
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using tx = Await conn.BeginTransactionAsync().ConfigureAwait(False)
                    Try
                        Dim prevQty As Integer = 0
                        Using lockCmd = conn.CreateCommand()
                            lockCmd.Transaction = tx
                            lockCmd.CommandText = "SELECT quantity FROM warehouse_stock WHERE warehouse_id = @WarehouseId AND product_id = @ProductId FOR UPDATE;"
                            lockCmd.Parameters.AddWithValue("@WarehouseId", warehouseId)
                            lockCmd.Parameters.AddWithValue("@ProductId", productId)
                            Dim currentObj = Await lockCmd.ExecuteScalarAsync().ConfigureAwait(False)
                            If currentObj IsNot Nothing AndAlso Not Convert.IsDBNull(currentObj) Then
                                prevQty = Convert.ToInt32(currentObj)
                            Else
                                Using initCmd = conn.CreateCommand()
                                    initCmd.Transaction = tx
                                    initCmd.CommandText = "INSERT INTO warehouse_stock (warehouse_id, product_id, quantity, last_updated_at) VALUES (@WarehouseId, @ProductId, 0, CURRENT_TIMESTAMP) ON CONFLICT DO NOTHING;"
                                    initCmd.Parameters.AddWithValue("@WarehouseId", warehouseId)
                                    initCmd.Parameters.AddWithValue("@ProductId", productId)
                                    Await initCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                                End Using
                                prevQty = 0
                            End If
                        End Using

                        Dim newQty = prevQty + deltaQuantity
                        If newQty < 0 Then
                            Throw New InvalidOperationException($"Insufficient inventory: Available quantity is {prevQty}, requested adjustment is {deltaQuantity}.")
                        End If

                        Using updateCmd = conn.CreateCommand()
                            updateCmd.Transaction = tx
                            updateCmd.CommandText = "INSERT INTO warehouse_stock (warehouse_id, product_id, quantity, last_updated_at) " &
                                                   "VALUES (@WarehouseId, @ProductId, @NewQty, CURRENT_TIMESTAMP) " &
                                                   "ON CONFLICT (warehouse_id, product_id) DO UPDATE SET quantity = @NewQty, last_updated_at = CURRENT_TIMESTAMP;"
                            updateCmd.Parameters.AddWithValue("@WarehouseId", warehouseId)
                            updateCmd.Parameters.AddWithValue("@ProductId", productId)
                            updateCmd.Parameters.AddWithValue("@NewQty", newQty)
                            Await updateCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                        End Using

                        Dim typeStr = MapTransactionTypeToString(transactionType)
                        Using txCmd = conn.CreateCommand()
                            txCmd.Transaction = tx
                            txCmd.CommandText = "INSERT INTO stock_transactions (product_id, warehouse_id, transaction_type, quantity, previous_qty, new_qty, reference_type, reference_id, notes, created_by, created_at) " &
                                               "VALUES (@ProductId, @WarehouseId, @Type, @Quantity, @PrevQty, @NewQty, @RefType, @RefId, @Notes, @CreatedBy, CURRENT_TIMESTAMP);"
                            txCmd.Parameters.AddWithValue("@ProductId", productId)
                            txCmd.Parameters.AddWithValue("@WarehouseId", warehouseId)
                            txCmd.Parameters.AddWithValue("@Type", typeStr)
                            txCmd.Parameters.AddWithValue("@Quantity", Math.Abs(deltaQuantity))
                            txCmd.Parameters.AddWithValue("@PrevQty", prevQty)
                            txCmd.Parameters.AddWithValue("@NewQty", newQty)
                            txCmd.Parameters.AddWithValue("@RefType", If(String.IsNullOrWhiteSpace(referenceType), DBNull.Value, CObj(referenceType)))
                            txCmd.Parameters.AddWithValue("@RefId", If(String.IsNullOrWhiteSpace(referenceId), DBNull.Value, CObj(referenceId)))
                            txCmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(notes), DBNull.Value, CObj(notes)))
                            txCmd.Parameters.AddWithValue("@CreatedBy", username)
                            Await txCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                        End Using

                        Await tx.CommitAsync().ConfigureAwait(False)
                        Return True
                    Catch ex As Exception
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Async Function TransferStockAsync(sourceWarehouseId As Integer, destWarehouseId As Integer, items As List(Of (ProductId As Integer, Quantity As Integer)), notes As String, username As String) As Task(Of (Success As Boolean, TransferNumber As String))
            If sourceWarehouseId = destWarehouseId Then
                Throw New ArgumentException("Source and destination warehouses must be different.")
            End If
            If items Is Nothing OrElse items.Count = 0 Then
                Throw New ArgumentException("At least one product item must be selected for transfer.")
            End If

            Dim transferNumber = "TRF-" & DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using tx = Await conn.BeginTransactionAsync().ConfigureAwait(False)
                    Try
                        Dim transferId As Integer
                        Using headCmd = conn.CreateCommand()
                            headCmd.Transaction = tx
                            headCmd.CommandText = "INSERT INTO stock_transfers (transfer_number, source_warehouse_id, destination_warehouse_id, status, notes, transfer_date, created_by, created_at, updated_at) " &
                                                  "VALUES (@Number, @SourceId, @DestId, 'Completed', @Notes, CURRENT_TIMESTAMP, @CreatedBy, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) RETURNING transfer_id;"
                            headCmd.Parameters.AddWithValue("@Number", transferNumber)
                            headCmd.Parameters.AddWithValue("@SourceId", sourceWarehouseId)
                            headCmd.Parameters.AddWithValue("@DestId", destWarehouseId)
                            headCmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(notes), DBNull.Value, CObj(notes)))
                            headCmd.Parameters.AddWithValue("@CreatedBy", username)
                            transferId = Convert.ToInt32(Await headCmd.ExecuteScalarAsync().ConfigureAwait(False))
                        End Using

                        For Each item In items
                            If item.Quantity <= 0 Then Continue For

                            Using itemCmd = conn.CreateCommand()
                                itemCmd.Transaction = tx
                                itemCmd.CommandText = "INSERT INTO stock_transfer_items (transfer_id, product_id, quantity, notes) VALUES (@TransferId, @ProductId, @Quantity, @Notes);"
                                itemCmd.Parameters.AddWithValue("@TransferId", transferId)
                                itemCmd.Parameters.AddWithValue("@ProductId", item.ProductId)
                                itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity)
                                itemCmd.Parameters.AddWithValue("@Notes", $"Transfer item {transferNumber}")
                                Await itemCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                            End Using

                            Dim sourcePrevQty As Integer = 0
                            Using srcLock = conn.CreateCommand()
                                srcLock.Transaction = tx
                                srcLock.CommandText = "SELECT quantity FROM warehouse_stock WHERE warehouse_id = @WarehouseId AND product_id = @ProductId FOR UPDATE;"
                                srcLock.Parameters.AddWithValue("@WarehouseId", sourceWarehouseId)
                                srcLock.Parameters.AddWithValue("@ProductId", item.ProductId)
                                Dim srcObj = Await srcLock.ExecuteScalarAsync().ConfigureAwait(False)
                                If srcObj IsNot Nothing AndAlso Not Convert.IsDBNull(srcObj) Then
                                    sourcePrevQty = Convert.ToInt32(srcObj)
                                Else
                                    sourcePrevQty = 0
                                End If
                            End Using

                            If sourcePrevQty < item.Quantity Then
                                Throw New InvalidOperationException($"Insufficient stock for Product ID {item.ProductId} in source warehouse. Available: {sourcePrevQty}, Required: {item.Quantity}.")
                            End If

                            Dim sourceNewQty = sourcePrevQty - item.Quantity
                            Using deductCmd = conn.CreateCommand()
                                deductCmd.Transaction = tx
                                deductCmd.CommandText = "UPDATE warehouse_stock SET quantity = @NewQty, last_updated_at = CURRENT_TIMESTAMP WHERE warehouse_id = @WarehouseId AND product_id = @ProductId;"
                                deductCmd.Parameters.AddWithValue("@NewQty", sourceNewQty)
                                deductCmd.Parameters.AddWithValue("@WarehouseId", sourceWarehouseId)
                                deductCmd.Parameters.AddWithValue("@ProductId", item.ProductId)
                                Await deductCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                            End Using

                            Using txSrcCmd = conn.CreateCommand()
                                txSrcCmd.Transaction = tx
                                txSrcCmd.CommandText = "INSERT INTO stock_transactions (product_id, warehouse_id, transaction_type, quantity, previous_qty, new_qty, reference_type, reference_id, notes, created_by, created_at) " &
                                                       "VALUES (@ProductId, @WarehouseId, 'Transfer Out', @Quantity, @PrevQty, @NewQty, 'TRANSFER', @RefId, @Notes, @CreatedBy, CURRENT_TIMESTAMP);"
                                txSrcCmd.Parameters.AddWithValue("@ProductId", item.ProductId)
                                txSrcCmd.Parameters.AddWithValue("@WarehouseId", sourceWarehouseId)
                                txSrcCmd.Parameters.AddWithValue("@Quantity", item.Quantity)
                                txSrcCmd.Parameters.AddWithValue("@PrevQty", sourcePrevQty)
                                txSrcCmd.Parameters.AddWithValue("@NewQty", sourceNewQty)
                                txSrcCmd.Parameters.AddWithValue("@RefId", transferNumber)
                                txSrcCmd.Parameters.AddWithValue("@Notes", $"Transferred to Warehouse ID {destWarehouseId}")
                                txSrcCmd.Parameters.AddWithValue("@CreatedBy", username)
                                Await txSrcCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                            End Using

                            Dim destPrevQty As Integer = 0
                            Using destLock = conn.CreateCommand()
                                destLock.Transaction = tx
                                destLock.CommandText = "SELECT quantity FROM warehouse_stock WHERE warehouse_id = @WarehouseId AND product_id = @ProductId FOR UPDATE;"
                                destLock.Parameters.AddWithValue("@WarehouseId", destWarehouseId)
                                destLock.Parameters.AddWithValue("@ProductId", item.ProductId)
                                Dim dstObj = Await destLock.ExecuteScalarAsync().ConfigureAwait(False)
                                If dstObj IsNot Nothing AndAlso Not Convert.IsDBNull(dstObj) Then
                                    destPrevQty = Convert.ToInt32(dstObj)
                                Else
                                    destPrevQty = 0
                                End If
                            End Using

                            Dim destNewQty = destPrevQty + item.Quantity
                            Using addCmd = conn.CreateCommand()
                                addCmd.Transaction = tx
                                addCmd.CommandText = "INSERT INTO warehouse_stock (warehouse_id, product_id, quantity, last_updated_at) VALUES (@WarehouseId, @ProductId, @NewQty, CURRENT_TIMESTAMP) " &
                                                    "ON CONFLICT (warehouse_id, product_id) DO UPDATE SET quantity = @NewQty, last_updated_at = CURRENT_TIMESTAMP;"
                                addCmd.Parameters.AddWithValue("@WarehouseId", destWarehouseId)
                                addCmd.Parameters.AddWithValue("@ProductId", item.ProductId)
                                addCmd.Parameters.AddWithValue("@NewQty", destNewQty)
                                Await addCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                            End Using

                            Using txDstCmd = conn.CreateCommand()
                                txDstCmd.Transaction = tx
                                txDstCmd.CommandText = "INSERT INTO stock_transactions (product_id, warehouse_id, transaction_type, quantity, previous_qty, new_qty, reference_type, reference_id, notes, created_by, created_at) " &
                                                       "VALUES (@ProductId, @WarehouseId, 'Transfer In', @Quantity, @PrevQty, @NewQty, 'TRANSFER', @RefId, @Notes, @CreatedBy, CURRENT_TIMESTAMP);"
                                txDstCmd.Parameters.AddWithValue("@ProductId", item.ProductId)
                                txDstCmd.Parameters.AddWithValue("@WarehouseId", destWarehouseId)
                                txDstCmd.Parameters.AddWithValue("@Quantity", item.Quantity)
                                txDstCmd.Parameters.AddWithValue("@PrevQty", destPrevQty)
                                txDstCmd.Parameters.AddWithValue("@NewQty", destNewQty)
                                txDstCmd.Parameters.AddWithValue("@RefId", transferNumber)
                                txDstCmd.Parameters.AddWithValue("@Notes", $"Transferred from Warehouse ID {sourceWarehouseId}")
                                txDstCmd.Parameters.AddWithValue("@CreatedBy", username)
                                Await txDstCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                            End Using
                        Next

                        Await tx.CommitAsync().ConfigureAwait(False)
                        Return (True, transferNumber)
                    Catch ex As Exception
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Async Function GetRecentTransactionsAsync(limitCount As Integer) As Task(Of List(Of StockTransaction))
            Dim list As New List(Of StockTransaction)()
            Dim query = "SELECT st.transaction_id, st.product_id, p.name AS product_name, p.sku AS product_sku, " &
                        "st.warehouse_id, w.name AS warehouse_name, st.transaction_type, st.quantity, st.previous_qty, st.new_qty, " &
                        "st.reference_type, st.reference_id, st.notes, st.created_by, st.created_at " &
                        "FROM stock_transactions st " &
                        "INNER JOIN products p ON st.product_id = p.product_id " &
                        "INNER JOIN warehouses w ON st.warehouse_id = w.warehouse_id " &
                        "ORDER BY st.created_at DESC " &
                        $"LIMIT {limitCount};"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(MapTransaction(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetStockTransfersAsync() As Task(Of List(Of StockTransfer))
            Dim list As New List(Of StockTransfer)()
            Const query = "SELECT t.transfer_id, t.transfer_number, t.source_warehouse_id, sw.name AS source_warehouse, " &
                          "t.destination_warehouse_id, dw.name AS destination_warehouse, t.status, t.notes, t.transfer_date, t.created_by, t.created_at, t.updated_at " &
                          "FROM stock_transfers t " &
                          "INNER JOIN warehouses sw ON t.source_warehouse_id = sw.warehouse_id " &
                          "INNER JOIN warehouses dw ON t.destination_warehouse_id = dw.warehouse_id " &
                          "ORDER BY t.created_at DESC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(New StockTransfer() With {
                                .TransferId = reader.GetInt32(0),
                                .TransferNumber = reader.GetString(1),
                                .SourceWarehouseId = reader.GetInt32(2),
                                .SourceWarehouseName = reader.GetString(3),
                                .DestinationWarehouseId = reader.GetInt32(4),
                                .DestinationWarehouseName = reader.GetString(5),
                                .Status = reader.GetString(6),
                                .Notes = If(reader.IsDBNull(7), String.Empty, reader.GetString(7)),
                                .TransferDate = reader.GetDateTime(8),
                                .CreatedBy = reader.GetString(9),
                                .CreatedAt = reader.GetDateTime(10),
                                .UpdatedAt = reader.GetDateTime(11)
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Private Shared Function MapWarehouseStock(reader As DbDataReader) As WarehouseStock
            Return New WarehouseStock() With {
                .StockId = reader.GetInt32(0),
                .WarehouseId = reader.GetInt32(1),
                .WarehouseName = reader.GetString(2),
                .ProductId = reader.GetInt32(3),
                .ProductName = reader.GetString(4),
                .ProductSku = reader.GetString(5),
                .Quantity = reader.GetInt32(6),
                .MinStockLevel = reader.GetInt32(7),
                .CostPrice = reader.GetDecimal(8),
                .SellingPrice = reader.GetDecimal(9),
                .LastUpdatedAt = reader.GetDateTime(10)
            }
        End Function

        Private Shared Function MapTransaction(reader As DbDataReader) As StockTransaction
            Return New StockTransaction() With {
                .TransactionId = reader.GetInt32(0),
                .ProductId = reader.GetInt32(1),
                .ProductName = reader.GetString(2),
                .ProductSku = reader.GetString(3),
                .WarehouseId = reader.GetInt32(4),
                .WarehouseName = reader.GetString(5),
                .TransactionType = reader.GetString(6),
                .Quantity = reader.GetInt32(7),
                .PreviousQty = reader.GetInt32(8),
                .NewQty = reader.GetInt32(9),
                .ReferenceType = If(reader.IsDBNull(10), String.Empty, reader.GetString(10)),
                .ReferenceId = If(reader.IsDBNull(11), String.Empty, reader.GetString(11)),
                .Notes = If(reader.IsDBNull(12), String.Empty, reader.GetString(12)),
                .CreatedBy = reader.GetString(13),
                .CreatedAt = reader.GetDateTime(14)
            }
        End Function

        Private Shared Function MapTransactionTypeToString(t As TransactionType) As String
            Select Case t
                Case TransactionType.StockIn : Return "Stock In"
                Case TransactionType.StockOut : Return "Stock Out"
                Case TransactionType.Purchase : Return "Purchase"
                Case TransactionType.Sale : Return "Sale"
                Case TransactionType.AdjustmentIncrease : Return "Adjustment Increase"
                Case TransactionType.AdjustmentDecrease : Return "Adjustment Decrease"
                Case TransactionType.TransferIn : Return "Transfer In"
                Case TransactionType.TransferOut : Return "Transfer Out"
                Case TransactionType.Return : Return "Return"
                Case Else : Return "Adjustment"
            End Select
        End Function

    End Class

    Public Class PurchaseRepository

        Public Async Function GetAllAsync(Optional statusFilter As String = "") As Task(Of List(Of PurchaseOrder))
            Dim list As New List(Of PurchaseOrder)()
            Dim query = "SELECT po.po_id, po.po_number, po.supplier_id, s.name AS supplier_name, " &
                        "po.warehouse_id, w.name AS warehouse_name, po.status, po.order_date, po.expected_date, po.received_date, " &
                        "po.subtotal, po.tax_amount, po.discount_amount, po.total_amount, po.notes, po.created_by, po.created_at, po.updated_at " &
                        "FROM purchase_orders po " &
                        "INNER JOIN suppliers s ON po.supplier_id = s.supplier_id " &
                        "INNER JOIN warehouses w ON po.warehouse_id = w.warehouse_id " &
                        If(Not String.IsNullOrWhiteSpace(statusFilter), "WHERE po.status = @Status ", "") &
                        "ORDER BY po.order_date DESC, po.po_id DESC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    If Not String.IsNullOrWhiteSpace(statusFilter) Then
                        cmd.Parameters.AddWithValue("@Status", statusFilter)
                    End If
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(MapPurchaseOrder(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetByIdAsync(poId As Integer) As Task(Of PurchaseOrder)
            Const query = "SELECT po.po_id, po.po_number, po.supplier_id, s.name AS supplier_name, " &
                          "po.warehouse_id, w.name AS warehouse_name, po.status, po.order_date, po.expected_date, po.received_date, " &
                          "po.subtotal, po.tax_amount, po.discount_amount, po.total_amount, po.notes, po.created_by, po.created_at, po.updated_at " &
                          "FROM purchase_orders po " &
                          "INNER JOIN suppliers s ON po.supplier_id = s.supplier_id " &
                          "INNER JOIN warehouses w ON po.warehouse_id = w.warehouse_id " &
                          "WHERE po.po_id = @PoId;"

            Dim po As PurchaseOrder = Nothing
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@PoId", poId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        If Await reader.ReadAsync().ConfigureAwait(False) Then
                            po = MapPurchaseOrder(reader)
                        End If
                    End Using
                End Using

                If po IsNot Nothing Then
                    po.Items = Await GetItemsByPoIdAsync(conn, poId).ConfigureAwait(False)
                End If
            End Using
            Return po
        End Function

        Public Async Function CreatePurchaseOrderAsync(po As PurchaseOrder) As Task(Of Integer)
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using tx = Await conn.BeginTransactionAsync().ConfigureAwait(False)
                    Try
                        Const insertPo = "INSERT INTO purchase_orders (po_number, supplier_id, warehouse_id, status, order_date, expected_date, subtotal, tax_amount, discount_amount, total_amount, notes, created_by, created_at, updated_at) " &
                                         "VALUES (@PoNumber, @SupplierId, @WarehouseId, @Status, @OrderDate, @ExpectedDate, @Subtotal, @TaxAmount, @DiscountAmount, @TotalAmount, @Notes, @CreatedBy, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) RETURNING po_id;"

                        Dim newPoId As Integer
                        Using cmd = conn.CreateCommand()
                            cmd.Transaction = tx
                            cmd.CommandText = insertPo
                            cmd.Parameters.AddWithValue("@PoNumber", po.PoNumber)
                            cmd.Parameters.AddWithValue("@SupplierId", po.SupplierId)
                            cmd.Parameters.AddWithValue("@WarehouseId", po.WarehouseId)
                            cmd.Parameters.AddWithValue("@Status", po.Status)
                            cmd.Parameters.AddWithValue("@OrderDate", po.OrderDate)
                            cmd.Parameters.AddWithValue("@ExpectedDate", If(po.ExpectedDate.HasValue, CObj(po.ExpectedDate.Value), DBNull.Value))
                            cmd.Parameters.AddWithValue("@Subtotal", po.Subtotal)
                            cmd.Parameters.AddWithValue("@TaxAmount", po.TaxAmount)
                            cmd.Parameters.AddWithValue("@DiscountAmount", po.DiscountAmount)
                            cmd.Parameters.AddWithValue("@TotalAmount", po.TotalAmount)
                            cmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(po.Notes), DBNull.Value, CObj(po.Notes)))
                            cmd.Parameters.AddWithValue("@CreatedBy", po.CreatedBy)

                            newPoId = Convert.ToInt32(Await cmd.ExecuteScalarAsync().ConfigureAwait(False))
                        End Using

                        For Each item In po.Items
                            Using itemCmd = conn.CreateCommand()
                                itemCmd.Transaction = tx
                                itemCmd.CommandText = "INSERT INTO purchase_order_items (po_id, product_id, quantity, unit_cost, discount_percent, line_total, notes) " &
                                                      "VALUES (@PoId, @ProductId, @Quantity, @UnitCost, @DiscountPercent, @LineTotal, @Notes);"
                                itemCmd.Parameters.AddWithValue("@PoId", newPoId)
                                itemCmd.Parameters.AddWithValue("@ProductId", item.ProductId)
                                itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity)
                                itemCmd.Parameters.AddWithValue("@UnitCost", item.UnitCost)
                                itemCmd.Parameters.AddWithValue("@DiscountPercent", item.DiscountPercent)
                                itemCmd.Parameters.AddWithValue("@LineTotal", item.LineTotal)
                                itemCmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(item.Notes), DBNull.Value, CObj(item.Notes)))
                                Await itemCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                            End Using
                        Next

                        Await tx.CommitAsync().ConfigureAwait(False)
                        Return newPoId
                    Catch ex As Exception
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Async Function ReceivePurchaseOrderAsync(poId As Integer, username As String) As Task(Of Boolean)
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using tx = Await conn.BeginTransactionAsync().ConfigureAwait(False)
                    Try
                        Dim currentStatus As String = ""
                        Dim poNumber As String = ""
                        Dim warehouseId As Integer = 0
                        Using checkCmd = conn.CreateCommand()
                            checkCmd.Transaction = tx
                            checkCmd.CommandText = "SELECT status, po_number, warehouse_id FROM purchase_orders WHERE po_id = @PoId FOR UPDATE;"
                            checkCmd.Parameters.AddWithValue("@PoId", poId)
                            Using r = Await checkCmd.ExecuteReaderAsync().ConfigureAwait(False)
                                If Await r.ReadAsync().ConfigureAwait(False) Then
                                    currentStatus = r.GetString(0)
                                    poNumber = r.GetString(1)
                                    warehouseId = r.GetInt32(2)
                                Else
                                    Throw New InvalidOperationException("Purchase order not found.")
                                End If
                            End Using
                        End Using

                        If String.Equals(currentStatus, "Received", StringComparison.OrdinalIgnoreCase) Then
                            Throw New InvalidOperationException("This purchase order has already been received and stock was already updated.")
                        End If

                        Dim items As New List(Of (ProductId As Integer, Quantity As Integer))()
                        Using itemsCmd = conn.CreateCommand()
                            itemsCmd.Transaction = tx
                            itemsCmd.CommandText = "SELECT product_id, quantity FROM purchase_order_items WHERE po_id = @PoId;"
                            itemsCmd.Parameters.AddWithValue("@PoId", poId)
                            Using r = Await itemsCmd.ExecuteReaderAsync().ConfigureAwait(False)
                                While Await r.ReadAsync().ConfigureAwait(False)
                                    items.Add((r.GetInt32(0), r.GetInt32(1)))
                                End While
                            End Using
                        End Using

                        For Each itm In items
                            Dim prevQty As Integer = 0
                            Using lockCmd = conn.CreateCommand()
                                lockCmd.Transaction = tx
                                lockCmd.CommandText = "SELECT quantity FROM warehouse_stock WHERE warehouse_id = @WarehouseId AND product_id = @ProductId FOR UPDATE;"
                                lockCmd.Parameters.AddWithValue("@WarehouseId", warehouseId)
                                lockCmd.Parameters.AddWithValue("@ProductId", itm.ProductId)
                                Dim currentObj = Await lockCmd.ExecuteScalarAsync().ConfigureAwait(False)
                                If currentObj IsNot Nothing AndAlso Not Convert.IsDBNull(currentObj) Then
                                    prevQty = Convert.ToInt32(currentObj)
                                Else
                                    prevQty = 0
                                End If
                            End Using

                            Dim newQty = prevQty + itm.Quantity
                            Using updCmd = conn.CreateCommand()
                                updCmd.Transaction = tx
                                updCmd.CommandText = "INSERT INTO warehouse_stock (warehouse_id, product_id, quantity, last_updated_at) VALUES (@WarehouseId, @ProductId, @NewQty, CURRENT_TIMESTAMP) " &
                                                    "ON CONFLICT (warehouse_id, product_id) DO UPDATE SET quantity = @NewQty, last_updated_at = CURRENT_TIMESTAMP;"
                                updCmd.Parameters.AddWithValue("@WarehouseId", warehouseId)
                                updCmd.Parameters.AddWithValue("@ProductId", itm.ProductId)
                                updCmd.Parameters.AddWithValue("@NewQty", newQty)
                                Await updCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                            End Using

                            Using txCmd = conn.CreateCommand()
                                txCmd.Transaction = tx
                                txCmd.CommandText = "INSERT INTO stock_transactions (product_id, warehouse_id, transaction_type, quantity, previous_qty, new_qty, reference_type, reference_id, notes, created_by, created_at) " &
                                                   "VALUES (@ProductId, @WarehouseId, 'Purchase', @Quantity, @PrevQty, @NewQty, 'PURCHASE_ORDER', @RefId, @Notes, @CreatedBy, CURRENT_TIMESTAMP);"
                                txCmd.Parameters.AddWithValue("@ProductId", itm.ProductId)
                                txCmd.Parameters.AddWithValue("@WarehouseId", warehouseId)
                                txCmd.Parameters.AddWithValue("@Quantity", itm.Quantity)
                                txCmd.Parameters.AddWithValue("@PrevQty", prevQty)
                                txCmd.Parameters.AddWithValue("@NewQty", newQty)
                                txCmd.Parameters.AddWithValue("@RefId", poNumber)
                                txCmd.Parameters.AddWithValue("@Notes", $"Goods receipt for Purchase Order {poNumber}")
                                txCmd.Parameters.AddWithValue("@CreatedBy", username)
                                Await txCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                            End Using
                        Next

                        Using updatePoCmd = conn.CreateCommand()
                            updatePoCmd.Transaction = tx
                            updatePoCmd.CommandText = "UPDATE purchase_orders SET status = 'Received', received_date = CURRENT_DATE, updated_at = CURRENT_TIMESTAMP WHERE po_id = @PoId;"
                            updatePoCmd.Parameters.AddWithValue("@PoId", poId)
                            Await updatePoCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                        End Using

                        Await tx.CommitAsync().ConfigureAwait(False)
                        Return True
                    Catch ex As Exception
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Async Function UpdateStatusAsync(poId As Integer, newStatus As String) As Task(Of Boolean)
            Const query = "UPDATE purchase_orders SET status = @Status, updated_at = CURRENT_TIMESTAMP WHERE po_id = @PoId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Status", newStatus)
                    cmd.Parameters.AddWithValue("@PoId", poId)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Private Shared Async Function GetItemsByPoIdAsync(conn As NpgsqlConnection, poId As Integer) As Task(Of List(Of PurchaseOrderItem))
            Dim list As New List(Of PurchaseOrderItem)()
            Const query = "SELECT poi.item_id, poi.po_id, poi.product_id, p.name AS product_name, p.sku AS product_sku, " &
                          "poi.quantity, poi.unit_cost, poi.discount_percent, poi.line_total, poi.notes " &
                          "FROM purchase_order_items poi " &
                          "INNER JOIN products p ON poi.product_id = p.product_id " &
                          "WHERE poi.po_id = @PoId ORDER BY poi.item_id ASC;"

            Using cmd = conn.CreateCommand()
                cmd.CommandText = query
                cmd.Parameters.AddWithValue("@PoId", poId)
                Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                    While Await reader.ReadAsync().ConfigureAwait(False)
                        list.Add(New PurchaseOrderItem() With {
                            .ItemId = reader.GetInt32(0),
                            .PoId = reader.GetInt32(1),
                            .ProductId = reader.GetInt32(2),
                            .ProductName = reader.GetString(3),
                            .ProductSku = reader.GetString(4),
                            .Quantity = reader.GetInt32(5),
                            .UnitCost = reader.GetDecimal(6),
                            .DiscountPercent = reader.GetDecimal(7),
                            .LineTotal = reader.GetDecimal(8),
                            .Notes = If(reader.IsDBNull(9), String.Empty, reader.GetString(9))
                        })
                    End While
                End Using
            End Using
            Return list
        End Function

        Private Shared Function MapPurchaseOrder(reader As DbDataReader) As PurchaseOrder
            Return New PurchaseOrder() With {
                .PoId = reader.GetInt32(0),
                .PoNumber = reader.GetString(1),
                .SupplierId = reader.GetInt32(2),
                .SupplierName = reader.GetString(3),
                .WarehouseId = reader.GetInt32(4),
                .WarehouseName = reader.GetString(5),
                .Status = reader.GetString(6),
                .OrderDate = reader.GetDateTime(7),
                .ExpectedDate = If(reader.IsDBNull(8), CType(Nothing, Nullable(Of DateTime)), reader.GetDateTime(8)),
                .ReceivedDate = If(reader.IsDBNull(9), CType(Nothing, Nullable(Of DateTime)), reader.GetDateTime(9)),
                .Subtotal = reader.GetDecimal(10),
                .TaxAmount = reader.GetDecimal(11),
                .DiscountAmount = reader.GetDecimal(12),
                .TotalAmount = reader.GetDecimal(13),
                .Notes = If(reader.IsDBNull(14), String.Empty, reader.GetString(14)),
                .CreatedBy = reader.GetString(15),
                .CreatedAt = reader.GetDateTime(16),
                .UpdatedAt = reader.GetDateTime(17)
            }
        End Function

    End Class

    Public Class SalesRepository

        Public Async Function GetAllAsync(Optional statusFilter As String = "") As Task(Of List(Of SalesOrder))
            Dim list As New List(Of SalesOrder)()
            Dim query = "SELECT so.so_id, so.so_number, so.customer_id, c.full_name AS customer_name, " &
                        "so.warehouse_id, w.name AS warehouse_name, so.status, so.order_date, so.delivery_date, " &
                        "so.subtotal, so.tax_amount, so.discount_amount, so.total_amount, so.payment_status, so.notes, so.created_by, so.created_at, so.updated_at " &
                        "FROM sales_orders so " &
                        "INNER JOIN customers c ON so.customer_id = c.customer_id " &
                        "INNER JOIN warehouses w ON so.warehouse_id = w.warehouse_id " &
                        If(Not String.IsNullOrWhiteSpace(statusFilter), "WHERE so.status = @Status ", "") &
                        "ORDER BY so.order_date DESC, so.so_id DESC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    If Not String.IsNullOrWhiteSpace(statusFilter) Then
                        cmd.Parameters.AddWithValue("@Status", statusFilter)
                    End If
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(MapSalesOrder(reader))
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetByIdAsync(soId As Integer) As Task(Of SalesOrder)
            Const query = "SELECT so.so_id, so.so_number, so.customer_id, c.full_name AS customer_name, " &
                          "so.warehouse_id, w.name AS warehouse_name, so.status, so.order_date, so.delivery_date, " &
                          "so.subtotal, so.tax_amount, so.discount_amount, so.total_amount, so.payment_status, so.notes, so.created_by, so.created_at, so.updated_at " &
                          "FROM sales_orders so " &
                          "INNER JOIN customers c ON so.customer_id = c.customer_id " &
                          "INNER JOIN warehouses w ON so.warehouse_id = w.warehouse_id " &
                          "WHERE so.so_id = @SoId;"

            Dim so As SalesOrder = Nothing
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@SoId", soId)
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        If Await reader.ReadAsync().ConfigureAwait(False) Then
                            so = MapSalesOrder(reader)
                        End If
                    End Using
                End Using

                If so IsNot Nothing Then
                    so.Items = Await GetItemsBySoIdAsync(conn, soId).ConfigureAwait(False)
                End If
            End Using
            Return so
        End Function

        Public Async Function CreateSalesOrderAsync(so As SalesOrder) As Task(Of Integer)
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using tx = Await conn.BeginTransactionAsync().ConfigureAwait(False)
                    Try
                        Const insertSo = "INSERT INTO sales_orders (so_number, customer_id, warehouse_id, status, order_date, delivery_date, subtotal, tax_amount, discount_amount, total_amount, payment_status, notes, created_by, created_at, updated_at) " &
                                         "VALUES (@SoNumber, @CustomerId, @WarehouseId, @Status, @OrderDate, @DeliveryDate, @Subtotal, @TaxAmount, @DiscountAmount, @TotalAmount, @PaymentStatus, @Notes, @CreatedBy, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP) RETURNING so_id;"

                        Dim newSoId As Integer
                        Using cmd = conn.CreateCommand()
                            cmd.Transaction = tx
                            cmd.CommandText = insertSo
                            cmd.Parameters.AddWithValue("@SoNumber", so.SoNumber)
                            cmd.Parameters.AddWithValue("@CustomerId", so.CustomerId)
                            cmd.Parameters.AddWithValue("@WarehouseId", so.WarehouseId)
                            cmd.Parameters.AddWithValue("@Status", so.Status)
                            cmd.Parameters.AddWithValue("@OrderDate", so.OrderDate)
                            cmd.Parameters.AddWithValue("@DeliveryDate", If(so.DeliveryDate.HasValue, CObj(so.DeliveryDate.Value), DBNull.Value))
                            cmd.Parameters.AddWithValue("@Subtotal", so.Subtotal)
                            cmd.Parameters.AddWithValue("@TaxAmount", so.TaxAmount)
                            cmd.Parameters.AddWithValue("@DiscountAmount", so.DiscountAmount)
                            cmd.Parameters.AddWithValue("@TotalAmount", so.TotalAmount)
                            cmd.Parameters.AddWithValue("@PaymentStatus", so.PaymentStatus)
                            cmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(so.Notes), DBNull.Value, CObj(so.Notes)))
                            cmd.Parameters.AddWithValue("@CreatedBy", so.CreatedBy)

                            newSoId = Convert.ToInt32(Await cmd.ExecuteScalarAsync().ConfigureAwait(False))
                        End Using

                        For Each item In so.Items
                            Using itemCmd = conn.CreateCommand()
                                itemCmd.Transaction = tx
                                itemCmd.CommandText = "INSERT INTO sales_order_items (so_id, product_id, quantity, unit_price, discount_percent, line_total, notes) " &
                                                      "VALUES (@SoId, @ProductId, @Quantity, @UnitPrice, @DiscountPercent, @LineTotal, @Notes);"
                                itemCmd.Parameters.AddWithValue("@SoId", newSoId)
                                itemCmd.Parameters.AddWithValue("@ProductId", item.ProductId)
                                itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity)
                                itemCmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice)
                                itemCmd.Parameters.AddWithValue("@DiscountPercent", item.DiscountPercent)
                                itemCmd.Parameters.AddWithValue("@LineTotal", item.LineTotal)
                                itemCmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(item.Notes), DBNull.Value, CObj(item.Notes)))
                                Await itemCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                            End Using
                        Next

                        Await tx.CommitAsync().ConfigureAwait(False)
                        Return newSoId
                    Catch ex As Exception
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Async Function CompleteSalesOrderAsync(soId As Integer, username As String) As Task(Of Boolean)
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using tx = Await conn.BeginTransactionAsync().ConfigureAwait(False)
                    Try
                        Dim currentStatus As String = ""
                        Dim soNumber As String = ""
                        Dim warehouseId As Integer = 0
                        Using checkCmd = conn.CreateCommand()
                            checkCmd.Transaction = tx
                            checkCmd.CommandText = "SELECT status, so_number, warehouse_id FROM sales_orders WHERE so_id = @SoId FOR UPDATE;"
                            checkCmd.Parameters.AddWithValue("@SoId", soId)
                            Using r = Await checkCmd.ExecuteReaderAsync().ConfigureAwait(False)
                                If Await r.ReadAsync().ConfigureAwait(False) Then
                                    currentStatus = r.GetString(0)
                                    soNumber = r.GetString(1)
                                    warehouseId = r.GetInt32(2)
                                Else
                                    Throw New InvalidOperationException("Sales order not found.")
                                End If
                            End Using
                        End Using

                        If String.Equals(currentStatus, "Completed", StringComparison.OrdinalIgnoreCase) Then
                            Throw New InvalidOperationException("This sales order is already completed and stock has already been deducted.")
                        End If

                        Dim items As New List(Of (ProductId As Integer, Quantity As Integer, ProductName As String))()
                        Using itemsCmd = conn.CreateCommand()
                            itemsCmd.Transaction = tx
                            itemsCmd.CommandText = "SELECT soi.product_id, soi.quantity, p.name FROM sales_order_items soi INNER JOIN products p ON soi.product_id = p.product_id WHERE soi.so_id = @SoId;"
                            itemsCmd.Parameters.AddWithValue("@SoId", soId)
                            Using r = Await itemsCmd.ExecuteReaderAsync().ConfigureAwait(False)
                                While Await r.ReadAsync().ConfigureAwait(False)
                                    items.Add((r.GetInt32(0), r.GetInt32(1), r.GetString(2)))
                                End While
                            End Using
                        End Using

                        For Each itm In items
                            Dim prevQty As Integer = 0
                            Using lockCmd = conn.CreateCommand()
                                lockCmd.Transaction = tx
                                lockCmd.CommandText = "SELECT quantity FROM warehouse_stock WHERE warehouse_id = @WarehouseId AND product_id = @ProductId FOR UPDATE;"
                                lockCmd.Parameters.AddWithValue("@WarehouseId", warehouseId)
                                lockCmd.Parameters.AddWithValue("@ProductId", itm.ProductId)
                                Dim currentObj = Await lockCmd.ExecuteScalarAsync().ConfigureAwait(False)
                                If currentObj IsNot Nothing AndAlso Not Convert.IsDBNull(currentObj) Then
                                    prevQty = Convert.ToInt32(currentObj)
                                Else
                                    prevQty = 0
                                End If
                            End Using

                            If prevQty < itm.Quantity Then
                                Throw New InvalidOperationException($"Cannot fulfill sales order: Insufficient stock for '{itm.ProductName}'. Available in warehouse: {prevQty}, Requested: {itm.Quantity}.")
                            End If

                            Dim newQty = prevQty - itm.Quantity
                            Using updCmd = conn.CreateCommand()
                                updCmd.Transaction = tx
                                updCmd.CommandText = "UPDATE warehouse_stock SET quantity = @NewQty, last_updated_at = CURRENT_TIMESTAMP WHERE warehouse_id = @WarehouseId AND product_id = @ProductId;"
                                updCmd.Parameters.AddWithValue("@NewQty", newQty)
                                updCmd.Parameters.AddWithValue("@WarehouseId", warehouseId)
                                updCmd.Parameters.AddWithValue("@ProductId", itm.ProductId)
                                Await updCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                            End Using

                            Using txCmd = conn.CreateCommand()
                                txCmd.Transaction = tx
                                txCmd.CommandText = "INSERT INTO stock_transactions (product_id, warehouse_id, transaction_type, quantity, previous_qty, new_qty, reference_type, reference_id, notes, created_by, created_at) " &
                                                   "VALUES (@ProductId, @WarehouseId, 'Sale', @Quantity, @PrevQty, @NewQty, 'SALES_ORDER', @RefId, @Notes, @CreatedBy, CURRENT_TIMESTAMP);"
                                txCmd.Parameters.AddWithValue("@ProductId", itm.ProductId)
                                txCmd.Parameters.AddWithValue("@WarehouseId", warehouseId)
                                txCmd.Parameters.AddWithValue("@Quantity", itm.Quantity)
                                txCmd.Parameters.AddWithValue("@PrevQty", prevQty)
                                txCmd.Parameters.AddWithValue("@NewQty", newQty)
                                txCmd.Parameters.AddWithValue("@RefId", soNumber)
                                txCmd.Parameters.AddWithValue("@Notes", $"Fulfillment for Sales Order {soNumber}")
                                txCmd.Parameters.AddWithValue("@CreatedBy", username)
                                Await txCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                            End Using
                        Next

                        Using updateSoCmd = conn.CreateCommand()
                            updateSoCmd.Transaction = tx
                            updateSoCmd.CommandText = "UPDATE sales_orders SET status = 'Completed', payment_status = 'Paid', delivery_date = CURRENT_DATE, updated_at = CURRENT_TIMESTAMP WHERE so_id = @SoId;"
                            updateSoCmd.Parameters.AddWithValue("@SoId", soId)
                            Await updateSoCmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                        End Using

                        Await tx.CommitAsync().ConfigureAwait(False)
                        Return True
                    Catch ex As Exception
                        tx.Rollback()
                        Throw
                    End Try
                End Using
            End Using
        End Function

        Public Async Function UpdateStatusAsync(soId As Integer, newStatus As String) As Task(Of Boolean)
            Const query = "UPDATE sales_orders SET status = @Status, updated_at = CURRENT_TIMESTAMP WHERE so_id = @SoId;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Status", newStatus)
                    cmd.Parameters.AddWithValue("@SoId", soId)
                    Dim rows = Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    Return rows > 0
                End Using
            End Using
        End Function

        Private Shared Async Function GetItemsBySoIdAsync(conn As NpgsqlConnection, soId As Integer) As Task(Of List(Of SalesOrderItem))
            Dim list As New List(Of SalesOrderItem)()
            Const query = "SELECT soi.item_id, soi.so_id, soi.product_id, p.name AS product_name, p.sku AS product_sku, " &
                          "soi.quantity, soi.unit_price, soi.discount_percent, soi.line_total, soi.notes " &
                          "FROM sales_order_items soi " &
                          "INNER JOIN products p ON soi.product_id = p.product_id " &
                          "WHERE soi.so_id = @SoId ORDER BY soi.item_id ASC;"

            Using cmd = conn.CreateCommand()
                cmd.CommandText = query
                cmd.Parameters.AddWithValue("@SoId", soId)
                Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                    While Await reader.ReadAsync().ConfigureAwait(False)
                        list.Add(New SalesOrderItem() With {
                            .ItemId = reader.GetInt32(0),
                            .SoId = reader.GetInt32(1),
                            .ProductId = reader.GetInt32(2),
                            .ProductName = reader.GetString(3),
                            .ProductSku = reader.GetString(4),
                            .Quantity = reader.GetInt32(5),
                            .UnitPrice = reader.GetDecimal(6),
                            .DiscountPercent = reader.GetDecimal(7),
                            .LineTotal = reader.GetDecimal(8),
                            .Notes = If(reader.IsDBNull(9), String.Empty, reader.GetString(9))
                        })
                    End While
                End Using
            End Using
            Return list
        End Function

        Private Shared Function MapSalesOrder(reader As DbDataReader) As SalesOrder
            Return New SalesOrder() With {
                .SoId = reader.GetInt32(0),
                .SoNumber = reader.GetString(1),
                .CustomerId = reader.GetInt32(2),
                .CustomerName = reader.GetString(3),
                .WarehouseId = reader.GetInt32(4),
                .WarehouseName = reader.GetString(5),
                .Status = reader.GetString(6),
                .OrderDate = reader.GetDateTime(7),
                .DeliveryDate = If(reader.IsDBNull(8), CType(Nothing, Nullable(Of DateTime)), reader.GetDateTime(8)),
                .Subtotal = reader.GetDecimal(9),
                .TaxAmount = reader.GetDecimal(10),
                .DiscountAmount = reader.GetDecimal(11),
                .TotalAmount = reader.GetDecimal(12),
                .PaymentStatus = reader.GetString(13),
                .Notes = If(reader.IsDBNull(14), String.Empty, reader.GetString(14)),
                .CreatedBy = reader.GetString(15),
                .CreatedAt = reader.GetDateTime(16),
                .UpdatedAt = reader.GetDateTime(17)
            }
        End Function

    End Class

    Public Class ReportRepository

        Public Class DashboardSummary
            Public Property TotalProductsCount As Integer
            Public Property TotalStockUnits As Integer
            Public Property TotalInventoryValue As Decimal
            Public Property LowStockCount As Integer
            Public Property OutOfStockCount As Integer
            Public Property TotalSalesCount As Integer
            Public Property TotalSalesAmount As Decimal
            Public Property TotalPurchasesCount As Integer
            Public Property TotalPurchasesAmount As Decimal
        End Class

        Public Async Function GetDashboardSummaryAsync() As Task(Of DashboardSummary)
            Dim summary As New DashboardSummary()

            Const query = "SELECT " &
                          "(SELECT COUNT(*) FROM products WHERE is_active = TRUE) AS total_products, " &
                          "COALESCE((SELECT SUM(quantity) FROM warehouse_stock), 0) AS total_units, " &
                          "COALESCE((SELECT SUM(ws.quantity * p.cost_price) FROM warehouse_stock ws INNER JOIN products p ON ws.product_id = p.product_id), 0.00) AS total_val, " &
                          "COALESCE((SELECT COUNT(DISTINCT p.product_id) FROM products p INNER JOIN warehouse_stock ws ON p.product_id = ws.product_id WHERE ws.quantity > 0 AND ws.quantity <= p.min_stock_level AND p.is_active = TRUE), 0) AS low_stock, " &
                          "COALESCE((SELECT COUNT(*) FROM products p WHERE (SELECT COALESCE(SUM(quantity), 0) FROM warehouse_stock ws WHERE ws.product_id = p.product_id) = 0 AND p.is_active = TRUE), 0) AS out_of_stock, " &
                          "COALESCE((SELECT COUNT(*) FROM sales_orders WHERE status = 'Completed'), 0) AS total_sales_count, " &
                          "COALESCE((SELECT SUM(total_amount) FROM sales_orders WHERE status = 'Completed'), 0.00) AS total_sales_amt, " &
                          "COALESCE((SELECT COUNT(*) FROM purchase_orders WHERE status = 'Received'), 0) AS total_po_count, " &
                          "COALESCE((SELECT SUM(total_amount) FROM purchase_orders WHERE status = 'Received'), 0.00) AS total_po_amt;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        If Await reader.ReadAsync().ConfigureAwait(False) Then
                            summary.TotalProductsCount = Convert.ToInt32(reader.GetInt64(0))
                            summary.TotalStockUnits = Convert.ToInt32(reader.GetInt64(1))
                            summary.TotalInventoryValue = reader.GetDecimal(2)
                            summary.LowStockCount = Convert.ToInt32(reader.GetInt64(3))
                            summary.OutOfStockCount = Convert.ToInt32(reader.GetInt64(4))
                            summary.TotalSalesCount = Convert.ToInt32(reader.GetInt64(5))
                            summary.TotalSalesAmount = reader.GetDecimal(6)
                            summary.TotalPurchasesCount = Convert.ToInt32(reader.GetInt64(7))
                            summary.TotalPurchasesAmount = reader.GetDecimal(8)
                        End If
                    End Using
                End Using
            End Using

            Return summary
        End Function

        Public Async Function GetCurrentInventoryValuationReportAsync(warehouseId As Nullable(Of Integer), categoryId As Nullable(Of Integer)) As Task(Of DataTable)
            Dim dt As New DataTable("InventoryValuation")
            Dim query = "SELECT p.sku AS ""SKU"", p.name AS ""Product Name"", c.name AS ""Category"", w.name AS ""Warehouse"", " &
                        "ws.quantity AS ""In Stock"", p.unit_of_measure AS ""UOM"", p.cost_price AS ""Cost Price"", " &
                        "(ws.quantity * p.cost_price) AS ""Total Cost Valuation"", " &
                        "p.selling_price AS ""Selling Price"", " &
                        "(ws.quantity * p.selling_price) AS ""Potential Sales Value"" " &
                        "FROM warehouse_stock ws " &
                        "INNER JOIN products p ON ws.product_id = p.product_id " &
                        "INNER JOIN warehouses w ON ws.warehouse_id = w.warehouse_id " &
                        "LEFT JOIN categories c ON p.category_id = c.category_id " &
                        "WHERE p.is_active = TRUE "

            If warehouseId.HasValue AndAlso warehouseId.Value > 0 Then
                query &= "AND ws.warehouse_id = @WarehouseId "
            End If
            If categoryId.HasValue AndAlso categoryId.Value > 0 Then
                query &= "AND p.category_id = @CategoryId "
            End If

            query &= "ORDER BY w.name ASC, p.name ASC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    If warehouseId.HasValue AndAlso warehouseId.Value > 0 Then
                        cmd.Parameters.AddWithValue("@WarehouseId", warehouseId.Value)
                    End If
                    If categoryId.HasValue AndAlso categoryId.Value > 0 Then
                        cmd.Parameters.AddWithValue("@CategoryId", categoryId.Value)
                    End If

                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        dt.Load(reader)
                    End Using
                End Using
            End Using
            Return dt
        End Function

        Public Async Function GetLowStockReportAsync(warehouseId As Nullable(Of Integer)) As Task(Of DataTable)
            Dim dt As New DataTable("LowStockReport")
            Dim query = "SELECT p.sku AS ""SKU"", p.name AS ""Product Name"", c.name AS ""Category"", w.name AS ""Warehouse"", " &
                        "ws.quantity AS ""Current Stock"", p.min_stock_level AS ""Min Threshold"", " &
                        "(p.min_stock_level - ws.quantity) AS ""Deficit / Reorder Qty"", " &
                        "s.name AS ""Preferred Supplier"", s.contact_person AS ""Contact"", s.phone AS ""Supplier Phone"" " &
                        "FROM warehouse_stock ws " &
                        "INNER JOIN products p ON ws.product_id = p.product_id " &
                        "INNER JOIN warehouses w ON ws.warehouse_id = w.warehouse_id " &
                        "LEFT JOIN categories c ON p.category_id = c.category_id " &
                        "LEFT JOIN suppliers s ON p.supplier_id = s.supplier_id " &
                        "WHERE ws.quantity <= p.min_stock_level AND p.is_active = TRUE "

            If warehouseId.HasValue AndAlso warehouseId.Value > 0 Then
                query &= "AND ws.warehouse_id = @WarehouseId "
            End If

            query &= "ORDER BY ws.quantity ASC, p.name ASC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    If warehouseId.HasValue AndAlso warehouseId.Value > 0 Then
                        cmd.Parameters.AddWithValue("@WarehouseId", warehouseId.Value)
                    End If

                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        dt.Load(reader)
                    End Using
                End Using
            End Using
            Return dt
        End Function

        Public Async Function GetStockMovementReportAsync(fromDate As DateTime, toDate As DateTime, productId As Nullable(Of Integer), warehouseId As Nullable(Of Integer)) As Task(Of DataTable)
            Dim dt As New DataTable("StockMovement")
            Dim query = "SELECT st.transaction_id AS ""Tx ID"", st.created_at AS ""Timestamp"", p.sku AS ""SKU"", p.name AS ""Product"", " &
                        "w.name AS ""Warehouse"", st.transaction_type AS ""Action"", st.quantity AS ""Quantity"", " &
                        "st.previous_qty AS ""Previous"", st.new_qty AS ""New Balance"", " &
                        "st.reference_type AS ""Ref Type"", st.reference_id AS ""Ref #"", st.notes AS ""Notes"", st.created_by AS ""User"" " &
                        "FROM stock_transactions st " &
                        "INNER JOIN products p ON st.product_id = p.product_id " &
                        "INNER JOIN warehouses w ON st.warehouse_id = w.warehouse_id " &
                        "WHERE st.created_at >= @FromDate AND st.created_at <= @ToDate "

            If productId.HasValue AndAlso productId.Value > 0 Then
                query &= "AND st.product_id = @ProductId "
            End If
            If warehouseId.HasValue AndAlso warehouseId.Value > 0 Then
                query &= "AND st.warehouse_id = @WarehouseId "
            End If

            query &= "ORDER BY st.created_at DESC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@FromDate", fromDate.Date)
                    cmd.Parameters.AddWithValue("@ToDate", toDate.Date.AddDays(1).AddTicks(-1))
                    If productId.HasValue AndAlso productId.Value > 0 Then
                        cmd.Parameters.AddWithValue("@ProductId", productId.Value)
                    End If
                    If warehouseId.HasValue AndAlso warehouseId.Value > 0 Then
                        cmd.Parameters.AddWithValue("@WarehouseId", warehouseId.Value)
                    End If

                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        dt.Load(reader)
                    End Using
                End Using
            End Using
            Return dt
        End Function

        Public Async Function GetSalesReportAsync(fromDate As DateTime, toDate As DateTime, customerId As Nullable(Of Integer), warehouseId As Nullable(Of Integer)) As Task(Of DataTable)
            Dim dt As New DataTable("SalesReport")
            Dim query = "SELECT so.so_number AS ""Order #"", so.order_date AS ""Date"", c.full_name AS ""Customer"", " &
                        "w.name AS ""Warehouse"", so.status AS ""Status"", so.payment_status AS ""Payment"", " &
                        "so.subtotal AS ""Subtotal"", so.tax_amount AS ""Tax"", so.discount_amount AS ""Discount"", " &
                        "so.total_amount AS ""Total Amount"", so.created_by AS ""Sales Rep"" " &
                        "FROM sales_orders so " &
                        "INNER JOIN customers c ON so.customer_id = c.customer_id " &
                        "INNER JOIN warehouses w ON so.warehouse_id = w.warehouse_id " &
                        "WHERE so.order_date >= @FromDate AND so.order_date <= @ToDate "

            If customerId.HasValue AndAlso customerId.Value > 0 Then
                query &= "AND so.customer_id = @CustomerId "
            End If
            If warehouseId.HasValue AndAlso warehouseId.Value > 0 Then
                query &= "AND so.warehouse_id = @WarehouseId "
            End If

            query &= "ORDER BY so.order_date DESC, so.so_id DESC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@FromDate", fromDate.Date)
                    cmd.Parameters.AddWithValue("@ToDate", toDate.Date)
                    If customerId.HasValue AndAlso customerId.Value > 0 Then
                        cmd.Parameters.AddWithValue("@CustomerId", customerId.Value)
                    End If
                    If warehouseId.HasValue AndAlso warehouseId.Value > 0 Then
                        cmd.Parameters.AddWithValue("@WarehouseId", warehouseId.Value)
                    End If

                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        dt.Load(reader)
                    End Using
                End Using
            End Using
            Return dt
        End Function

        Public Async Function GetPurchasesReportAsync(fromDate As DateTime, toDate As DateTime, supplierId As Nullable(Of Integer), warehouseId As Nullable(Of Integer)) As Task(Of DataTable)
            Dim dt As New DataTable("PurchasesReport")
            Dim query = "SELECT po.po_number AS ""PO #"", po.order_date AS ""Date"", s.name AS ""Supplier"", " &
                        "w.name AS ""Warehouse"", po.status AS ""Status"", po.received_date AS ""Received Date"", " &
                        "po.subtotal AS ""Subtotal"", po.tax_amount AS ""Tax"", po.discount_amount AS ""Discount"", " &
                        "po.total_amount AS ""Total Amount"", po.created_by AS ""Created By"" " &
                        "FROM purchase_orders po " &
                        "INNER JOIN suppliers s ON po.supplier_id = s.supplier_id " &
                        "INNER JOIN warehouses w ON po.warehouse_id = w.warehouse_id " &
                        "WHERE po.order_date >= @FromDate AND po.order_date <= @ToDate "

            If supplierId.HasValue AndAlso supplierId.Value > 0 Then
                query &= "AND po.supplier_id = @SupplierId "
            End If
            If warehouseId.HasValue AndAlso warehouseId.Value > 0 Then
                query &= "AND po.warehouse_id = @WarehouseId "
            End If

            query &= "ORDER BY po.order_date DESC, po.po_id DESC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@FromDate", fromDate.Date)
                    cmd.Parameters.AddWithValue("@ToDate", toDate.Date)
                    If supplierId.HasValue AndAlso supplierId.Value > 0 Then
                        cmd.Parameters.AddWithValue("@SupplierId", supplierId.Value)
                    End If
                    If warehouseId.HasValue AndAlso warehouseId.Value > 0 Then
                        cmd.Parameters.AddWithValue("@WarehouseId", warehouseId.Value)
                    End If

                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        dt.Load(reader)
                    End Using
                End Using
            End Using
            Return dt
        End Function

    End Class

    Public Class AuditRepository

        Public Async Function LogAsync(userId As Nullable(Of Integer), username As String, action As String, entityType As String, entityId As String, oldValues As String, newValues As String) As Task
            If Not AppConfiguration.Instance.EnableAuditLogging Then Return
            Const query = "INSERT INTO audit_logs (user_id, username, action, entity_type, entity_id, old_values, new_values, created_at) " &
                          "VALUES (@UserId, @Username, @Action, @EntityType, @EntityId, @OldVal::jsonb, @NewVal::jsonb, CURRENT_TIMESTAMP);"

            Try
                Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                    Using cmd = conn.CreateCommand()
                        cmd.CommandText = query
                        cmd.Parameters.AddWithValue("@UserId", If(userId.HasValue, CObj(userId.Value), DBNull.Value))
                        cmd.Parameters.AddWithValue("@Username", If(String.IsNullOrWhiteSpace(username), "System", username))
                        cmd.Parameters.AddWithValue("@Action", action)
                        cmd.Parameters.AddWithValue("@EntityType", entityType)
                        cmd.Parameters.AddWithValue("@EntityId", If(String.IsNullOrWhiteSpace(entityId), DBNull.Value, CObj(entityId)))
                        cmd.Parameters.AddWithValue("@OldVal", If(String.IsNullOrWhiteSpace(oldValues), "null", oldValues))
                        cmd.Parameters.AddWithValue("@NewVal", If(String.IsNullOrWhiteSpace(newValues), "null", newValues))
                        Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                    End Using
                End Using
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Failed to write audit log: {ex.Message}")
            End Try
        End Function

        Public Async Function GetRecentLogsAsync(limitCount As Integer) As Task(Of List(Of AuditLog))
            Dim list As New List(Of AuditLog)()
            Dim query = "SELECT log_id, user_id, username, action, entity_type, entity_id, " &
                        "COALESCE(old_values::text, '') AS old_vals, COALESCE(new_values::text, '') AS new_vals, " &
                        "COALESCE(ip_address, '') AS ip, created_at " &
                        "FROM audit_logs " &
                        "ORDER BY created_at DESC " &
                        $"LIMIT {limitCount};"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(New AuditLog() With {
                                .LogId = reader.GetInt32(0),
                                .UserId = If(reader.IsDBNull(1), CType(Nothing, Nullable(Of Integer)), reader.GetInt32(1)),
                                .Username = reader.GetString(2),
                                .Action = reader.GetString(3),
                                .EntityType = reader.GetString(4),
                                .EntityId = If(reader.IsDBNull(5), String.Empty, reader.GetString(5)),
                                .OldValues = reader.GetString(6),
                                .NewValues = reader.GetString(7),
                                .IpAddress = reader.GetString(8),
                                .CreatedAt = reader.GetDateTime(9)
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

    End Class

    Public Class SettingRepository

        Public Async Function GetAllAsync() As Task(Of List(Of ApplicationSetting))
            Dim list As New List(Of ApplicationSetting)()
            Const query = "SELECT setting_key, setting_value, description, updated_at FROM application_settings ORDER BY setting_key ASC;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    Using reader = Await cmd.ExecuteReaderAsync().ConfigureAwait(False)
                        While Await reader.ReadAsync().ConfigureAwait(False)
                            list.Add(New ApplicationSetting() With {
                                .SettingKey = reader.GetString(0),
                                .SettingValue = reader.GetString(1),
                                .Description = If(reader.IsDBNull(2), String.Empty, reader.GetString(2)),
                                .UpdatedAt = reader.GetDateTime(3)
                            })
                        End While
                    End Using
                End Using
            End Using
            Return list
        End Function

        Public Async Function GetValueAsync(key As String, Optional defaultValue As String = "") As Task(Of String)
            Const query = "SELECT setting_value FROM application_settings WHERE setting_key = @Key;"
            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Key", key)
                    Dim result = Await cmd.ExecuteScalarAsync().ConfigureAwait(False)
                    If result IsNot Nothing AndAlso Not Convert.IsDBNull(result) Then
                        Return result.ToString()
                    End If
                    Return defaultValue
                End Using
            End Using
        End Function

        Public Async Function SaveValueAsync(key As String, value As String, Optional description As String = "") As Task
            Const query = "INSERT INTO application_settings (setting_key, setting_value, description, updated_at) " &
                          "VALUES (@Key, @Value, @Description, CURRENT_TIMESTAMP) " &
                          "ON CONFLICT (setting_key) DO UPDATE SET setting_value = @Value, description = COALESCE(NULLIF(@Description, ''), application_settings.description), updated_at = CURRENT_TIMESTAMP;"

            Using conn = Await DbConnectionFactory.OpenConnectionAsync().ConfigureAwait(False)
                Using cmd = conn.CreateCommand()
                    cmd.CommandText = query
                    cmd.Parameters.AddWithValue("@Key", key)
                    cmd.Parameters.AddWithValue("@Value", value)
                    cmd.Parameters.AddWithValue("@Description", description)
                    Await cmd.ExecuteNonQueryAsync().ConfigureAwait(False)
                End Using
            End Using
        End Function

    End Class

End Namespace
