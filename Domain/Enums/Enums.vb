Option Strict On
Option Explicit On

Namespace Domain.Enums

    Public Enum TransactionType
        StockIn
        StockOut
        Purchase
        Sale
        AdjustmentIncrease
        AdjustmentDecrease
        TransferOut
        TransferIn
        [Return]
    End Enum

    Public Enum OrderStatus
        Draft
        Confirmed
        Received
        Completed
        Cancelled
    End Enum

    Public Enum PaymentStatus
        Pending
        Paid
        [Partial]
        Refunded
    End Enum

    Public Enum UserRoleType
        Administrator = 1
        Manager = 2
        WarehouseStaff = 3
        SalesStaff = 4
    End Enum

End Namespace
