Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic

Namespace Domain.Entities

    Public Class Role
        Public Property RoleId As Integer
        Public Property RoleName As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property IsSystem As Boolean
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property Permissions As List(Of Permission) = New List(Of Permission)()
    End Class

    Public Class Permission
        Public Property PermissionId As Integer
        Public Property PermissionCode As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property ModuleName As String = String.Empty
    End Class

    Public Class User
        Public Property UserId As Integer
        Public Property Username As String = String.Empty
        Public Property PasswordHash As String = String.Empty
        Public Property FullName As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property RoleId As Integer
        Public Property RoleName As String = String.Empty
        Public Property IsActive As Boolean = True
        Public Property LastLoginAt As Nullable(Of DateTime)
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow
    End Class

    Public Class Category
        Public Property CategoryId As Integer
        Public Property Name As String = String.Empty
        Public Property Code As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property IsActive As Boolean = True
        Public Property ProductCount As Integer
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow
    End Class

    Public Class Supplier
        Public Property SupplierId As Integer
        Public Property Name As String = String.Empty
        Public Property CompanyName As String = String.Empty
        Public Property ContactPerson As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Address As String = String.Empty
        Public Property TaxNumber As String = String.Empty
        Public Property Notes As String = String.Empty
        Public Property IsActive As Boolean = True
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow
    End Class

    Public Class Customer
        Public Property CustomerId As Integer
        Public Property FullName As String = String.Empty
        Public Property CompanyName As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Address As String = String.Empty
        Public Property TaxNumber As String = String.Empty
        Public Property Notes As String = String.Empty
        Public Property IsActive As Boolean = True
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow
    End Class

    Public Class Warehouse
        Public Property WarehouseId As Integer
        Public Property Name As String = String.Empty
        Public Property Code As String = String.Empty
        Public Property Location As String = String.Empty
        Public Property ManagerName As String = String.Empty
        Public Property ContactPhone As String = String.Empty
        Public Property IsActive As Boolean = True
        Public Property TotalStockCount As Integer
        Public Property TotalInventoryValue As Decimal
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow
    End Class

    Public Class Product
        Public Property ProductId As Integer
        Public Property Sku As String = String.Empty
        Public Property Barcode As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property CategoryId As Nullable(Of Integer)
        Public Property CategoryName As String = String.Empty
        Public Property SupplierId As Nullable(Of Integer)
        Public Property SupplierName As String = String.Empty
        Public Property CostPrice As Decimal
        Public Property SellingPrice As Decimal
        Public Property MinStockLevel As Integer = 5
        Public Property UnitOfMeasure As String = "Units"
        Public Property IsActive As Boolean = True
        Public Property TotalStock As Integer
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow
    End Class

    Public Class WarehouseStock
        Public Property StockId As Integer
        Public Property WarehouseId As Integer
        Public Property WarehouseName As String = String.Empty
        Public Property ProductId As Integer
        Public Property ProductName As String = String.Empty
        Public Property ProductSku As String = String.Empty
        Public Property Quantity As Integer
        Public Property MinStockLevel As Integer
        Public Property CostPrice As Decimal
        Public Property SellingPrice As Decimal
        Public Property LastUpdatedAt As DateTime = DateTime.UtcNow
    End Class

    Public Class StockTransaction
        Public Property TransactionId As Integer
        Public Property ProductId As Integer
        Public Property ProductName As String = String.Empty
        Public Property ProductSku As String = String.Empty
        Public Property WarehouseId As Integer
        Public Property WarehouseName As String = String.Empty
        Public Property TransactionType As String = String.Empty
        Public Property Quantity As Integer
        Public Property PreviousQty As Integer
        Public Property NewQty As Integer
        Public Property ReferenceType As String = String.Empty
        Public Property ReferenceId As String = String.Empty
        Public Property Notes As String = String.Empty
        Public Property CreatedBy As String = String.Empty
        Public Property CreatedAt As DateTime = DateTime.UtcNow
    End Class

    Public Class StockTransfer
        Public Property TransferId As Integer
        Public Property TransferNumber As String = String.Empty
        Public Property SourceWarehouseId As Integer
        Public Property SourceWarehouseName As String = String.Empty
        Public Property DestinationWarehouseId As Integer
        Public Property DestinationWarehouseName As String = String.Empty
        Public Property Status As String = "Completed"
        Public Property Notes As String = String.Empty
        Public Property TransferDate As DateTime = DateTime.UtcNow
        Public Property CreatedBy As String = String.Empty
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow
        Public Property Items As List(Of StockTransferItem) = New List(Of StockTransferItem)()
    End Class

    Public Class StockTransferItem
        Public Property ItemId As Integer
        Public Property TransferId As Integer
        Public Property ProductId As Integer
        Public Property ProductName As String = String.Empty
        Public Property ProductSku As String = String.Empty
        Public Property Quantity As Integer
        Public Property Notes As String = String.Empty
    End Class

    Public Class PurchaseOrder
        Public Property PoId As Integer
        Public Property PoNumber As String = String.Empty
        Public Property SupplierId As Integer
        Public Property SupplierName As String = String.Empty
        Public Property WarehouseId As Integer
        Public Property WarehouseName As String = String.Empty
        Public Property Status As String = "Draft"
        Public Property OrderDate As DateTime = DateTime.Today
        Public Property ExpectedDate As Nullable(Of DateTime)
        Public Property ReceivedDate As Nullable(Of DateTime)
        Public Property Subtotal As Decimal
        Public Property TaxAmount As Decimal
        Public Property DiscountAmount As Decimal
        Public Property TotalAmount As Decimal
        Public Property Notes As String = String.Empty
        Public Property CreatedBy As String = String.Empty
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow
        Public Property Items As List(Of PurchaseOrderItem) = New List(Of PurchaseOrderItem)()
    End Class

    Public Class PurchaseOrderItem
        Public Property ItemId As Integer
        Public Property PoId As Integer
        Public Property ProductId As Integer
        Public Property ProductName As String = String.Empty
        Public Property ProductSku As String = String.Empty
        Public Property Quantity As Integer
        Public Property UnitCost As Decimal
        Public Property DiscountPercent As Decimal
        Public Property LineTotal As Decimal
        Public Property Notes As String = String.Empty
    End Class

    Public Class SalesOrder
        Public Property SoId As Integer
        Public Property SoNumber As String = String.Empty
        Public Property CustomerId As Integer
        Public Property CustomerName As String = String.Empty
        Public Property WarehouseId As Integer
        Public Property WarehouseName As String = String.Empty
        Public Property Status As String = "Draft"
        Public Property OrderDate As DateTime = DateTime.Today
        Public Property DeliveryDate As Nullable(Of DateTime)
        Public Property Subtotal As Decimal
        Public Property TaxAmount As Decimal
        Public Property DiscountAmount As Decimal
        Public Property TotalAmount As Decimal
        Public Property PaymentStatus As String = "Pending"
        Public Property Notes As String = String.Empty
        Public Property CreatedBy As String = String.Empty
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow
        Public Property Items As List(Of SalesOrderItem) = New List(Of SalesOrderItem)()
    End Class

    Public Class SalesOrderItem
        Public Property ItemId As Integer
        Public Property SoId As Integer
        Public Property ProductId As Integer
        Public Property ProductName As String = String.Empty
        Public Property ProductSku As String = String.Empty
        Public Property Quantity As Integer
        Public Property UnitPrice As Decimal
        Public Property DiscountPercent As Decimal
        Public Property LineTotal As Decimal
        Public Property Notes As String = String.Empty
    End Class

    Public Class AuditLog
        Public Property LogId As Integer
        Public Property UserId As Nullable(Of Integer)
        Public Property Username As String = String.Empty
        Public Property Action As String = String.Empty
        Public Property EntityType As String = String.Empty
        Public Property EntityId As String = String.Empty
        Public Property OldValues As String = String.Empty
        Public Property NewValues As String = String.Empty
        Public Property IpAddress As String = String.Empty
        Public Property CreatedAt As DateTime = DateTime.UtcNow
    End Class

    Public Class ApplicationSetting
        Public Property SettingKey As String = String.Empty
        Public Property SettingValue As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property UpdatedAt As DateTime = DateTime.UtcNow
    End Class

End Namespace
