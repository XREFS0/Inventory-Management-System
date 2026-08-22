Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Data
Imports System.IO
Imports System.Text
Imports System.Threading.Tasks
Imports MASA.InventoryManagementSystem.Domain.Entities
Imports MASA.InventoryManagementSystem.Domain.Enums
Imports MASA.InventoryManagementSystem.Infrastructure.Repositories
Imports MASA.InventoryManagementSystem.Infrastructure.Security

Namespace Application.Services

    Public Class AuthService

        Private ReadOnly _userRepo As New UserRepository()
        Private ReadOnly _auditRepo As New AuditRepository()

        Public Async Function LoginAsync(username As String, password As String) As Task(Of (Success As Boolean, Message As String, User As User))
            If String.IsNullOrWhiteSpace(username) OrElse String.IsNullOrWhiteSpace(password) Then
                Return (False, "Please enter both username and password.", Nothing)
            End If

            Try
                Dim user = Await _userRepo.GetByUsernameAsync(username.Trim()).ConfigureAwait(False)
                If user Is Nothing Then
                    Return (False, "Invalid username or password.", Nothing)
                End If

                If Not user.IsActive Then
                    Return (False, "Your account has been deactivated. Please contact your administrator.", Nothing)
                End If

                Dim isValidPassword = PasswordHasher.VerifyPassword(password, user.PasswordHash)
                If Not isValidPassword Then
                    Return (False, "Invalid username or password.", Nothing)
                End If

                Dim permissions = Await _userRepo.GetUserPermissionsAsync(user.RoleId).ConfigureAwait(False)

                SessionContext.SetSession(user, permissions)

                Await _userRepo.UpdateLastLoginAsync(user.UserId).ConfigureAwait(False)

                Await _auditRepo.LogAsync(user.UserId, user.Username, "User Login", "User", user.UserId.ToString(), Nothing, $"{{""username"": ""{user.Username}"", ""role"": ""{user.RoleName}""}}").ConfigureAwait(False)

                Return (True, "Login successful.", user)
            Catch ex As Exception
                Return (False, $"Login failed due to a system error: {ex.Message}", Nothing)
            End Try
        End Function

        Public Sub Logout()
            If SessionContext.IsAuthenticated Then
                Dim user = SessionContext.CurrentUser
                Task.Run(Async Function()
                             Await _auditRepo.LogAsync(user.UserId, user.Username, "User Logout", "User", user.UserId.ToString(), Nothing, Nothing).ConfigureAwait(False)
                         End Function)
            End If
            SessionContext.ClearSession()
        End Sub

        Public Async Function ChangePasswordAsync(userId As Integer, currentPassword As String, newPassword As String) As Task(Of (Success As Boolean, Message As String))
            If String.IsNullOrWhiteSpace(newPassword) OrElse newPassword.Length < 6 Then
                Return (False, "New password must be at least 6 characters long.")
            End If

            Dim user = Await _userRepo.GetByIdAsync(userId).ConfigureAwait(False)
            If user Is Nothing Then
                Return (False, "User not found.")
            End If

            If Not PasswordHasher.VerifyPassword(currentPassword, user.PasswordHash) Then
                Return (False, "Current password is incorrect.")
            End If

            Dim newHash = PasswordHasher.HashPassword(newPassword)
            Dim updated = Await _userRepo.UpdatePasswordAsync(userId, newHash).ConfigureAwait(False)
            If updated Then
                Await _auditRepo.LogAsync(userId, SessionContext.Username, "Password Changed", "User", userId.ToString(), Nothing, Nothing).ConfigureAwait(False)
                Return (True, "Password changed successfully.")
            End If
            Return (False, "Failed to update password.")
        End Function

    End Class

    Public Class ProductService

        Private ReadOnly _productRepo As New ProductRepository()
        Private ReadOnly _auditRepo As New AuditRepository()

        Public Async Function SearchProductsAsync(searchTerm As String, categoryId As Nullable(Of Integer), supplierId As Nullable(Of Integer), onlyActive As Boolean) As Task(Of List(Of Product))
            Return Await _productRepo.SearchAsync(searchTerm, categoryId, supplierId, onlyActive).ConfigureAwait(False)
        End Function

        Public Async Function GetProductByIdAsync(productId As Integer) As Task(Of Product)
            Return Await _productRepo.GetByIdAsync(productId).ConfigureAwait(False)
        End Function

        Public Async Function CreateProductAsync(product As Product) As Task(Of (Success As Boolean, Message As String, ProductId As Integer))
            If String.IsNullOrWhiteSpace(product.Sku) Then
                Return (False, "SKU is required.", 0)
            End If
            If String.IsNullOrWhiteSpace(product.Name) Then
                Return (False, "Product Name is required.", 0)
            End If
            If product.CostPrice < 0 OrElse product.SellingPrice < 0 Then
                Return (False, "Cost and selling prices cannot be negative.", 0)
            End If
            If product.MinStockLevel < 0 Then
                Return (False, "Minimum stock level cannot be negative.", 0)
            End If

            Dim skuExists = Await _productRepo.ExistsBySkuAsync(product.Sku).ConfigureAwait(False)
            If skuExists Then
                Return (False, $"SKU '{product.Sku}' is already registered to another product.", 0)
            End If

            Dim newId = Await _productRepo.InsertAsync(product).ConfigureAwait(False)
            Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Product Created", "Product", newId.ToString(), Nothing, $"{{""sku"": ""{product.Sku}"", ""name"": ""{product.Name}"", ""price"": {product.SellingPrice}}}").ConfigureAwait(False)

            Return (True, "Product created successfully.", newId)
        End Function

        Public Async Function UpdateProductAsync(product As Product) As Task(Of (Success As Boolean, Message As String))
            If product.ProductId <= 0 Then
                Return (False, "Invalid Product ID.")
            End If
            If String.IsNullOrWhiteSpace(product.Sku) Then
                Return (False, "SKU is required.")
            End If
            If String.IsNullOrWhiteSpace(product.Name) Then
                Return (False, "Product Name is required.")
            End If
            If product.CostPrice < 0 OrElse product.SellingPrice < 0 Then
                Return (False, "Cost and selling prices cannot be negative.")
            End If

            Dim skuExists = Await _productRepo.ExistsBySkuAsync(product.Sku, product.ProductId).ConfigureAwait(False)
            If skuExists Then
                Return (False, $"SKU '{product.Sku}' is already assigned to another product.")
            End If

            Dim updated = Await _productRepo.UpdateAsync(product).ConfigureAwait(False)
            If updated Then
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Product Updated", "Product", product.ProductId.ToString(), Nothing, $"{{""sku"": ""{product.Sku}"", ""name"": ""{product.Name}""}}").ConfigureAwait(False)
                Return (True, "Product updated successfully.")
            End If
            Return (False, "Failed to update product.")
        End Function

        Public Async Function DeactivateProductAsync(productId As Integer) As Task(Of (Success As Boolean, Message As String))
            Dim updated = Await _productRepo.SoftDeleteAsync(productId).ConfigureAwait(False)
            If updated Then
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Product Deactivated", "Product", productId.ToString(), Nothing, Nothing).ConfigureAwait(False)
                Return (True, "Product deactivated successfully.")
            End If
            Return (False, "Failed to deactivate product.")
        End Function

    End Class

    Public Class CategoryService

        Private ReadOnly _categoryRepo As New CategoryRepository()
        Private ReadOnly _auditRepo As New AuditRepository()

        Public Async Function GetAllCategoriesAsync(Optional onlyActive As Boolean = False) As Task(Of List(Of Category))
            Return Await _categoryRepo.GetAllAsync(onlyActive).ConfigureAwait(False)
        End Function

        Public Async Function SaveCategoryAsync(category As Category) As Task(Of (Success As Boolean, Message As String, CategoryId As Integer))
            If String.IsNullOrWhiteSpace(category.Name) Then
                Return (False, "Category name is required.", 0)
            End If

            Dim exists = Await _categoryRepo.ExistsByNameAsync(category.Name, category.CategoryId).ConfigureAwait(False)
            If exists Then
                Return (False, $"A category named '{category.Name}' already exists.", 0)
            End If

            If category.CategoryId = 0 Then
                Dim newId = Await _categoryRepo.InsertAsync(category).ConfigureAwait(False)
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Category Created", "Category", newId.ToString(), Nothing, $"{{""name"": ""{category.Name}""}}").ConfigureAwait(False)
                Return (True, "Category created successfully.", newId)
            Else
                Dim updated = Await _categoryRepo.UpdateAsync(category).ConfigureAwait(False)
                If updated Then
                    Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Category Updated", "Category", category.CategoryId.ToString(), Nothing, $"{{""name"": ""{category.Name}""}}").ConfigureAwait(False)
                    Return (True, "Category updated successfully.", category.CategoryId)
                End If
                Return (False, "Failed to update category.", category.CategoryId)
            End If
        End Function

        Public Async Function DeactivateCategoryAsync(categoryId As Integer) As Task(Of (Success As Boolean, Message As String))
            Dim updated = Await _categoryRepo.SoftDeleteAsync(categoryId).ConfigureAwait(False)
            If updated Then
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Category Deactivated", "Category", categoryId.ToString(), Nothing, Nothing).ConfigureAwait(False)
                Return (True, "Category deactivated successfully.")
            End If
            Return (False, "Failed to deactivate category.")
        End Function

    End Class

    Public Class WarehouseService

        Private ReadOnly _warehouseRepo As New WarehouseRepository()
        Private ReadOnly _auditRepo As New AuditRepository()

        Public Async Function GetAllWarehousesAsync(Optional onlyActive As Boolean = False) As Task(Of List(Of Warehouse))
            Return Await _warehouseRepo.GetAllAsync(onlyActive).ConfigureAwait(False)
        End Function

        Public Async Function SaveWarehouseAsync(w As Warehouse) As Task(Of (Success As Boolean, Message As String, WarehouseId As Integer))
            If String.IsNullOrWhiteSpace(w.Name) Then
                Return (False, "Warehouse name is required.", 0)
            End If
            If String.IsNullOrWhiteSpace(w.Code) Then
                Return (False, "Warehouse code is required.", 0)
            End If

            Dim exists = Await _warehouseRepo.ExistsByCodeAsync(w.Code, w.WarehouseId).ConfigureAwait(False)
            If exists Then
                Return (False, $"A warehouse with code '{w.Code}' already exists.", 0)
            End If

            If w.WarehouseId = 0 Then
                Dim newId = Await _warehouseRepo.InsertAsync(w).ConfigureAwait(False)
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Warehouse Created", "Warehouse", newId.ToString(), Nothing, $"{{""name"": ""{w.Name}"", ""code"": ""{w.Code}""}}").ConfigureAwait(False)
                Return (True, "Warehouse created successfully.", newId)
            Else
                Dim updated = Await _warehouseRepo.UpdateAsync(w).ConfigureAwait(False)
                If updated Then
                    Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Warehouse Updated", "Warehouse", w.WarehouseId.ToString(), Nothing, $"{{""name"": ""{w.Name}"", ""code"": ""{w.Code}""}}").ConfigureAwait(False)
                    Return (True, "Warehouse updated successfully.", w.WarehouseId)
                End If
                Return (False, "Failed to update warehouse.", w.WarehouseId)
            End If
        End Function

        Public Async Function DeactivateWarehouseAsync(warehouseId As Integer) As Task(Of (Success As Boolean, Message As String))
            Dim updated = Await _warehouseRepo.SoftDeleteAsync(warehouseId).ConfigureAwait(False)
            If updated Then
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Warehouse Deactivated", "Warehouse", warehouseId.ToString(), Nothing, Nothing).ConfigureAwait(False)
                Return (True, "Warehouse deactivated successfully.")
            End If
            Return (False, "Failed to deactivate warehouse.")
        End Function

    End Class

    Public Class SupplierService

        Private ReadOnly _supplierRepo As New SupplierRepository()
        Private ReadOnly _auditRepo As New AuditRepository()

        Public Async Function GetAllSuppliersAsync(Optional onlyActive As Boolean = False) As Task(Of List(Of Supplier))
            Return Await _supplierRepo.GetAllAsync(onlyActive).ConfigureAwait(False)
        End Function

        Public Async Function SaveSupplierAsync(s As Supplier) As Task(Of (Success As Boolean, Message As String, SupplierId As Integer))
            If String.IsNullOrWhiteSpace(s.Name) Then
                Return (False, "Supplier name is required.", 0)
            End If

            If s.SupplierId = 0 Then
                Dim newId = Await _supplierRepo.InsertAsync(s).ConfigureAwait(False)
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Supplier Created", "Supplier", newId.ToString(), Nothing, $"{{""name"": ""{s.Name}""}}").ConfigureAwait(False)
                Return (True, "Supplier registered successfully.", newId)
            Else
                Dim updated = Await _supplierRepo.UpdateAsync(s).ConfigureAwait(False)
                If updated Then
                    Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Supplier Updated", "Supplier", s.SupplierId.ToString(), Nothing, $"{{""name"": ""{s.Name}""}}").ConfigureAwait(False)
                    Return (True, "Supplier updated successfully.", s.SupplierId)
                End If
                Return (False, "Failed to update supplier.", s.SupplierId)
            End If
        End Function

        Public Async Function DeactivateSupplierAsync(supplierId As Integer) As Task(Of (Success As Boolean, Message As String))
            Dim updated = Await _supplierRepo.SoftDeleteAsync(supplierId).ConfigureAwait(False)
            If updated Then
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Supplier Deactivated", "Supplier", supplierId.ToString(), Nothing, Nothing).ConfigureAwait(False)
                Return (True, "Supplier deactivated successfully.")
            End If
            Return (False, "Failed to deactivate supplier.")
        End Function

    End Class

    Public Class CustomerService

        Private ReadOnly _customerRepo As New CustomerRepository()
        Private ReadOnly _auditRepo As New AuditRepository()

        Public Async Function GetAllCustomersAsync(Optional onlyActive As Boolean = False) As Task(Of List(Of Customer))
            Return Await _customerRepo.GetAllAsync(onlyActive).ConfigureAwait(False)
        End Function

        Public Async Function SaveCustomerAsync(c As Customer) As Task(Of (Success As Boolean, Message As String, CustomerId As Integer))
            If String.IsNullOrWhiteSpace(c.FullName) Then
                Return (False, "Customer full name is required.", 0)
            End If

            If c.CustomerId = 0 Then
                Dim newId = Await _customerRepo.InsertAsync(c).ConfigureAwait(False)
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Customer Created", "Customer", newId.ToString(), Nothing, $"{{""name"": ""{c.FullName}""}}").ConfigureAwait(False)
                Return (True, "Customer registered successfully.", newId)
            Else
                Dim updated = Await _customerRepo.UpdateAsync(c).ConfigureAwait(False)
                If updated Then
                    Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Customer Updated", "Customer", c.CustomerId.ToString(), Nothing, $"{{""name"": ""{c.FullName}""}}").ConfigureAwait(False)
                    Return (True, "Customer updated successfully.", c.CustomerId)
                End If
                Return (False, "Failed to update customer.", c.CustomerId)
            End If
        End Function

        Public Async Function DeactivateCustomerAsync(customerId As Integer) As Task(Of (Success As Boolean, Message As String))
            Dim updated = Await _customerRepo.SoftDeleteAsync(customerId).ConfigureAwait(False)
            If updated Then
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Customer Deactivated", "Customer", customerId.ToString(), Nothing, Nothing).ConfigureAwait(False)
                Return (True, "Customer deactivated successfully.")
            End If
            Return (False, "Failed to deactivate customer.")
        End Function

    End Class

    Public Class InventoryService

        Private ReadOnly _inventoryRepo As New InventoryRepository()
        Private ReadOnly _auditRepo As New AuditRepository()

        Public Async Function GetStockByWarehouseAsync(warehouseId As Integer) As Task(Of List(Of WarehouseStock))
            Return Await _inventoryRepo.GetStockByWarehouseAsync(warehouseId).ConfigureAwait(False)
        End Function

        Public Async Function GetProductStockBreakdownAsync(productId As Integer) As Task(Of List(Of WarehouseStock))
            Return Await _inventoryRepo.GetAllWarehouseStockBreakdownAsync(productId).ConfigureAwait(False)
        End Function

        Public Async Function GetRecentTransactionsAsync(Optional limit As Integer = 50) As Task(Of List(Of StockTransaction))
            Return Await _inventoryRepo.GetRecentTransactionsAsync(limit).ConfigureAwait(False)
        End Function

        Public Async Function GetStockTransfersAsync() As Task(Of List(Of StockTransfer))
            Return Await _inventoryRepo.GetStockTransfersAsync().ConfigureAwait(False)
        End Function

        Public Async Function AdjustStockAsync(productId As Integer, warehouseId As Integer, transactionType As TransactionType, quantityDelta As Integer, notes As String) As Task(Of (Success As Boolean, Message As String))
            If productId <= 0 OrElse warehouseId <= 0 Then
                Return (False, "Invalid product or warehouse selected.")
            End If
            If quantityDelta = 0 Then
                Return (False, "Quantity adjustment must not be zero.")
            End If

            Try
                Dim username = SessionContext.Username
                Dim refId = "ADJ-" & DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
                Await _inventoryRepo.AdjustStockAsync(productId, warehouseId, transactionType, quantityDelta, "MANUAL_ADJUSTMENT", refId, notes, username).ConfigureAwait(False)
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, username, "Stock Adjusted", "Inventory", productId.ToString(), Nothing, $"{{""delta"": {quantityDelta}, ""type"": ""{transactionType}""}}").ConfigureAwait(False)
                Return (True, "Stock adjusted successfully.")
            Catch ex As Exception
                Return (False, $"Failed to adjust stock: {ex.Message}")
            End Try
        End Function

        Public Async Function TransferStockAsync(sourceWarehouseId As Integer, destWarehouseId As Integer, items As List(Of (ProductId As Integer, Quantity As Integer)), notes As String) As Task(Of (Success As Boolean, Message As String, TransferNumber As String))
            If sourceWarehouseId <= 0 OrElse destWarehouseId <= 0 Then
                Return (False, "Please select valid source and destination warehouses.", "")
            End If
            If sourceWarehouseId = destWarehouseId Then
                Return (False, "Source and destination warehouses must be different.", "")
            End If
            If items Is Nothing OrElse items.Count = 0 Then
                Return (False, "Please select at least one item to transfer.", "")
            End If

            Try
                Dim username = SessionContext.Username
                Dim res = Await _inventoryRepo.TransferStockAsync(sourceWarehouseId, destWarehouseId, items, notes, username).ConfigureAwait(False)
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, username, "Stock Transferred", "Transfer", res.TransferNumber, Nothing, $"{{""from"": {sourceWarehouseId}, ""to"": {destWarehouseId}, ""itemsCount"": {items.Count}}}").ConfigureAwait(False)
                Return (True, $"Stock transfer {res.TransferNumber} completed successfully.", res.TransferNumber)
            Catch ex As Exception
                Return (False, $"Transfer failed: {ex.Message}", "")
            End Try
        End Function

    End Class

    Public Class PurchaseService

        Private ReadOnly _purchaseRepo As New PurchaseRepository()
        Private ReadOnly _auditRepo As New AuditRepository()

        Public Async Function GetAllOrdersAsync(Optional statusFilter As String = "") As Task(Of List(Of PurchaseOrder))
            Return Await _purchaseRepo.GetAllAsync(statusFilter).ConfigureAwait(False)
        End Function

        Public Async Function GetOrderByIdAsync(poId As Integer) As Task(Of PurchaseOrder)
            Return Await _purchaseRepo.GetByIdAsync(poId).ConfigureAwait(False)
        End Function

        Public Async Function CreatePurchaseOrderAsync(po As PurchaseOrder) As Task(Of (Success As Boolean, Message As String, PoId As Integer))
            If po.SupplierId <= 0 Then
                Return (False, "Please select a valid supplier.", 0)
            End If
            If po.WarehouseId <= 0 Then
                Return (False, "Please select a destination warehouse.", 0)
            End If
            If po.Items Is Nothing OrElse po.Items.Count = 0 Then
                Return (False, "Purchase order must contain at least one line item.", 0)
            End If

            po.PoNumber = "PO-" & DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
            po.CreatedBy = SessionContext.Username

            Dim subtotal As Decimal = 0
            For Each itm In po.Items
                If itm.Quantity <= 0 Then
                    Return (False, $"Item quantity must be greater than 0.", 0)
                End If
                Dim lineBeforeDisc = itm.Quantity * itm.UnitCost
                Dim discount = lineBeforeDisc * (itm.DiscountPercent / 100D)
                itm.LineTotal = lineBeforeDisc - discount
                subtotal += itm.LineTotal
            Next

            po.Subtotal = subtotal
            po.TotalAmount = (po.Subtotal + po.TaxAmount) - po.DiscountAmount

            Try
                Dim newPoId = Await _purchaseRepo.CreatePurchaseOrderAsync(po).ConfigureAwait(False)
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Purchase Order Created", "PurchaseOrder", newPoId.ToString(), Nothing, $"{{""poNumber"": ""{po.PoNumber}"", ""total"": {po.TotalAmount}}}").ConfigureAwait(False)
                Return (True, $"Purchase Order {po.PoNumber} created successfully.", newPoId)
            Catch ex As Exception
                Return (False, $"Failed to create purchase order: {ex.Message}", 0)
            End Try
        End Function

        Public Async Function ReceivePurchaseOrderAsync(poId As Integer) As Task(Of (Success As Boolean, Message As String))
            Try
                Dim username = SessionContext.Username
                Dim success = Await _purchaseRepo.ReceivePurchaseOrderAsync(poId, username).ConfigureAwait(False)
                If success Then
                    Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, username, "Purchase Order Received", "PurchaseOrder", poId.ToString(), Nothing, Nothing).ConfigureAwait(False)
                    Return (True, "Purchase order marked as Received and warehouse inventory has been updated.")
                End If
                Return (False, "Failed to receive purchase order.")
            Catch ex As Exception
                Return (False, $"Error receiving purchase order: {ex.Message}")
            End Try
        End Function

        Public Async Function UpdateStatusAsync(poId As Integer, newStatus As String) As Task(Of (Success As Boolean, Message As String))
            Dim updated = Await _purchaseRepo.UpdateStatusAsync(poId, newStatus).ConfigureAwait(False)
            If updated Then
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, $"PO Status Changed to {newStatus}", "PurchaseOrder", poId.ToString(), Nothing, Nothing).ConfigureAwait(False)
                Return (True, $"Purchase order status updated to {newStatus}.")
            End If
            Return (False, "Failed to update purchase order status.")
        End Function

    End Class

    Public Class SalesService

        Private ReadOnly _salesRepo As New SalesRepository()
        Private ReadOnly _auditRepo As New AuditRepository()

        Public Async Function GetAllOrdersAsync(Optional statusFilter As String = "") As Task(Of List(Of SalesOrder))
            Return Await _salesRepo.GetAllAsync(statusFilter).ConfigureAwait(False)
        End Function

        Public Async Function GetOrderByIdAsync(soId As Integer) As Task(Of SalesOrder)
            Return Await _salesRepo.GetByIdAsync(soId).ConfigureAwait(False)
        End Function

        Public Async Function CreateSalesOrderAsync(so As SalesOrder) As Task(Of (Success As Boolean, Message As String, SoId As Integer))
            If so.CustomerId <= 0 Then
                Return (False, "Please select a customer.", 0)
            End If
            If so.WarehouseId <= 0 Then
                Return (False, "Please select an issuing warehouse.", 0)
            End If
            If so.Items Is Nothing OrElse so.Items.Count = 0 Then
                Return (False, "Sales order must contain at least one line item.", 0)
            End If

            so.SoNumber = "SO-" & DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")
            so.CreatedBy = SessionContext.Username

            Dim subtotal As Decimal = 0
            For Each itm In so.Items
                If itm.Quantity <= 0 Then
                    Return (False, $"Item quantity must be greater than 0.", 0)
                End If
                Dim lineBeforeDisc = itm.Quantity * itm.UnitPrice
                Dim discount = lineBeforeDisc * (itm.DiscountPercent / 100D)
                itm.LineTotal = lineBeforeDisc - discount
                subtotal += itm.LineTotal
            Next

            so.Subtotal = subtotal
            so.TotalAmount = (so.Subtotal + so.TaxAmount) - so.DiscountAmount

            Try
                Dim newSoId = Await _salesRepo.CreateSalesOrderAsync(so).ConfigureAwait(False)
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Sales Order Created", "SalesOrder", newSoId.ToString(), Nothing, $"{{""soNumber"": ""{so.SoNumber}"", ""total"": {so.TotalAmount}}}").ConfigureAwait(False)
                Return (True, $"Sales Order {so.SoNumber} created successfully.", newSoId)
            Catch ex As Exception
                Return (False, $"Failed to create sales order: {ex.Message}", 0)
            End Try
        End Function

        Public Async Function CompleteSalesOrderAsync(soId As Integer) As Task(Of (Success As Boolean, Message As String))
            Try
                Dim username = SessionContext.Username
                Dim success = Await _salesRepo.CompleteSalesOrderAsync(soId, username).ConfigureAwait(False)
                If success Then
                    Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, username, "Sales Order Completed", "SalesOrder", soId.ToString(), Nothing, Nothing).ConfigureAwait(False)
                    Return (True, "Sales order marked as Completed and inventory has been deducted.")
                End If
                Return (False, "Failed to complete sales order.")
            Catch ex As Exception
                Return (False, $"Error completing sales order: {ex.Message}")
            End Try
        End Function

        Public Async Function UpdateStatusAsync(soId As Integer, newStatus As String) As Task(Of (Success As Boolean, Message As String))
            Dim updated = Await _salesRepo.UpdateStatusAsync(soId, newStatus).ConfigureAwait(False)
            If updated Then
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, $"SO Status Changed to {newStatus}", "SalesOrder", soId.ToString(), Nothing, Nothing).ConfigureAwait(False)
                Return (True, $"Sales order status updated to {newStatus}.")
            End If
            Return (False, "Failed to update sales order status.")
        End Function

    End Class

    Public Class ReportService

        Private ReadOnly _reportRepo As New ReportRepository()

        Public Async Function GetDashboardSummaryAsync() As Task(Of ReportRepository.DashboardSummary)
            Return Await _reportRepo.GetDashboardSummaryAsync().ConfigureAwait(False)
        End Function

        Public Async Function GetCurrentInventoryValuationReportAsync(warehouseId As Nullable(Of Integer), categoryId As Nullable(Of Integer)) As Task(Of DataTable)
            Return Await _reportRepo.GetCurrentInventoryValuationReportAsync(warehouseId, categoryId).ConfigureAwait(False)
        End Function

        Public Async Function GetLowStockReportAsync(warehouseId As Nullable(Of Integer)) As Task(Of DataTable)
            Return Await _reportRepo.GetLowStockReportAsync(warehouseId).ConfigureAwait(False)
        End Function

        Public Async Function GetStockMovementReportAsync(fromDate As DateTime, toDate As DateTime, productId As Nullable(Of Integer), warehouseId As Nullable(Of Integer)) As Task(Of DataTable)
            Return Await _reportRepo.GetStockMovementReportAsync(fromDate, toDate, productId, warehouseId).ConfigureAwait(False)
        End Function

        Public Async Function GetSalesReportAsync(fromDate As DateTime, toDate As DateTime, customerId As Nullable(Of Integer), warehouseId As Nullable(Of Integer)) As Task(Of DataTable)
            Return Await _reportRepo.GetSalesReportAsync(fromDate, toDate, customerId, warehouseId).ConfigureAwait(False)
        End Function

        Public Async Function GetPurchasesReportAsync(fromDate As DateTime, toDate As DateTime, supplierId As Nullable(Of Integer), warehouseId As Nullable(Of Integer)) As Task(Of DataTable)
            Return Await _reportRepo.GetPurchasesReportAsync(fromDate, toDate, supplierId, warehouseId).ConfigureAwait(False)
        End Function

        Public Shared Function ExportDataTableToCsv(dt As DataTable, filePath As String) As (Success As Boolean, Message As String)
            Try
                Dim sb As New StringBuilder()

                Dim colNames As New List(Of String)()
                For Each col As DataColumn In dt.Columns
                    colNames.Add($"""{col.ColumnName.Replace("""", """""")}""")
                Next
                sb.AppendLine(String.Join(",", colNames))

                For Each row As DataRow In dt.Rows
                    Dim rowValues As New List(Of String)()
                    For Each col As DataColumn In dt.Columns
                        Dim val = If(row.IsNull(col), "", row(col).ToString())
                        rowValues.Add($"""{val.Replace("""", """""")}""")
                    Next
                    sb.AppendLine(String.Join(",", rowValues))
                Next

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8)
                Return (True, "Export completed successfully.")
            Catch ex As Exception
                Return (False, $"Export failed: {ex.Message}")
            End Try
        End Function

    End Class

    Public Class UserService

        Private ReadOnly _userRepo As New UserRepository()
        Private ReadOnly _auditRepo As New AuditRepository()

        Public Async Function GetAllUsersAsync() As Task(Of List(Of User))
            Return Await _userRepo.GetAllAsync().ConfigureAwait(False)
        End Function

        Public Async Function GetAllRolesAsync() As Task(Of List(Of Role))
            Return Await _userRepo.GetAllRolesAsync().ConfigureAwait(False)
        End Function

        Public Async Function CreateUserAsync(user As User, plainPassword As String) As Task(Of (Success As Boolean, Message As String, UserId As Integer))
            If String.IsNullOrWhiteSpace(user.Username) Then
                Return (False, "Username is required.", 0)
            End If
            If String.IsNullOrWhiteSpace(plainPassword) OrElse plainPassword.Length < 6 Then
                Return (False, "Password must be at least 6 characters.", 0)
            End If
            If String.IsNullOrWhiteSpace(user.FullName) Then
                Return (False, "Full Name is required.", 0)
            End If
            If user.RoleId <= 0 Then
                Return (False, "Please select a valid role.", 0)
            End If

            Dim existing = Await _userRepo.GetByUsernameAsync(user.Username).ConfigureAwait(False)
            If existing IsNot Nothing Then
                Return (False, $"Username '{user.Username}' is already taken.", 0)
            End If

            user.PasswordHash = PasswordHasher.HashPassword(plainPassword)
            Dim newId = Await _userRepo.InsertAsync(user).ConfigureAwait(False)
            Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "User Created", "User", newId.ToString(), Nothing, $"{{""username"": ""{user.Username}"", ""roleId"": {user.RoleId}}}").ConfigureAwait(False)
            Return (True, "User account created successfully.", newId)
        End Function

        Public Async Function UpdateUserAsync(user As User) As Task(Of (Success As Boolean, Message As String))
            If user.UserId <= 0 Then
                Return (False, "Invalid User ID.")
            End If
            If String.IsNullOrWhiteSpace(user.FullName) Then
                Return (False, "Full Name is required.")
            End If

            Dim updated = Await _userRepo.UpdateAsync(user).ConfigureAwait(False)
            If updated Then
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "User Updated", "User", user.UserId.ToString(), Nothing, $"{{""fullName"": ""{user.FullName}"", ""roleId"": {user.RoleId}, ""isActive"": {user.IsActive}}}").ConfigureAwait(False)
                Return (True, "User updated successfully.")
            End If
            Return (False, "Failed to update user.")
        End Function

        Public Async Function ResetPasswordAsync(userId As Integer, newPassword As String) As Task(Of (Success As Boolean, Message As String))
            If String.IsNullOrWhiteSpace(newPassword) OrElse newPassword.Length < 6 Then
                Return (False, "Password must be at least 6 characters.")
            End If

            Dim newHash = PasswordHasher.HashPassword(newPassword)
            Dim updated = Await _userRepo.UpdatePasswordAsync(userId, newHash).ConfigureAwait(False)
            If updated Then
                Await _auditRepo.LogAsync(SessionContext.CurrentUser?.UserId, SessionContext.Username, "Admin Reset User Password", "User", userId.ToString(), Nothing, Nothing).ConfigureAwait(False)
                Return (True, "Password has been reset.")
            End If
            Return (False, "Failed to reset password.")
        End Function

    End Class

    Public Class AuditService

        Private ReadOnly _auditRepo As New AuditRepository()

        Public Async Function GetRecentLogsAsync(Optional limitCount As Integer = 100) As Task(Of List(Of AuditLog))
            Return Await _auditRepo.GetRecentLogsAsync(limitCount).ConfigureAwait(False)
        End Function

    End Class

End Namespace
