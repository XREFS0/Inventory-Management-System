# MASA Inventory Management System

Repository: [https://github.com/XREFS0/Inventory-Management-System](https://github.com/XREFS0/Inventory-Management-System)

A desktop inventory and warehouse management system developed with VB.NET, .NET 8 (Windows Forms), and PostgreSQL (Supabase).

All rights reserved to XREFS0.

---

## Overview

MASA Inventory Management System is designed for multi-warehouse inventory control, procurement, sales tracking, stock adjustments, and analytical reporting. It features a layered architecture with compile-time type safety (`Option Strict On`, `Option Explicit On`), parameterized data access via Npgsql, and a typography-focused desktop interface.

---

## Architecture

The project follows a 4-tier layered architecture:

- **Domain Layer**: Entity models (`Product`, `Category`, `Warehouse`, `WarehouseStock`, `StockTransaction`, `StockTransfer`, `PurchaseOrder`, `SalesOrder`, `Supplier`, `Customer`, `User`, `Role`, `Permission`) and domain enums (`TransactionType`, `OrderStatus`, `PaymentStatus`, `UserRoleType`).
- **Infrastructure Layer**: Database connection factory (`DbConnectionFactory`), embedded DDL schema migrator (`DatabaseInitializer`), repository layer with parameterized SQL queries via `Npgsql`, and BCrypt security services (`PasswordHasher`, `SessionContext`).
- **Application Layer**: Business services (`AuthService`, `ProductService`, `CategoryService`, `WarehouseService`, `InventoryService`, `SupplierService`, `CustomerService`, `PurchaseService`, `SalesService`, `ReportService`, `UserService`, `AuditService`).
- **Presentation Layer**: Windows Forms interface, custom card panels, stat cards, searchable data grids, modal dialogs, and navigation shell.

---

## Key Features

- **Authentication & RBAC**: Role-based access control supporting Administrator, Manager, Warehouse Staff, and Sales Staff roles with BCrypt password hashing.
- **Operations Dashboard**: Real-time KPI metrics for active catalog, in-stock unit balance, cost valuation, low-stock warnings, sales totals, purchase totals, and transaction activity.
- **Inventory Ledger**: Complete audit trail for all stock transactions (Stock In, Stock Out, Purchases, Sales, Adjustments, Transfers, Returns).
- **Multi-Warehouse Transfers**: Inter-facility stock transfers with validation against source inventory balances.
- **Purchase Order Workflow**: Purchase order lifecycle from Draft to Confirmed to Received, with automatic inventory increments.
- **Sales Order Fulfillment**: Sales order management with real-time stock validation, customer assignment, and dispatch deductions.
- **Reports & Exporting**: Inventory valuation reports, low-stock alerts, movement summaries, and sales analytics with CSV export.
- **Database Self-Provisioning**: Built-in schema creation and initial seeding directly from the application.

---

## Requirements

- .NET 8.0 SDK or Runtime (Windows Desktop)
- Windows 10, Windows 11, or Windows Server
- PostgreSQL 14+ or a Supabase PostgreSQL instance

---

## Setup and Installation

### 1. Clone the Repository

```powershell
git clone https://github.com/XREFS0/Inventory-Management-System.git
cd Inventory-Management-System
```

### 2. Configure Database Connection

Create `appsettings.json` based on `appsettings.example.json`:

```json
{
  "ConnectionStrings": {
    "PostgreSql": "Host=db.your-ref.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR_PASSWORD;Pooling=true;SSL Mode=Require;Trust Server Certificate=true"
  },
  "Supabase": {
    "Url": "https://your-ref.supabase.co",
    "ProjectRef": "your-ref",
    "AnonKey": "your-anon-key",
    "DirectHost": "db.your-ref.supabase.co",
    "Port": 5432,
    "Username": "postgres",
    "Database": "postgres"
  }
}
```

The database connection can also be tested, saved, and provisioned through the **Configure Database** dialog on the login screen.

### 3. Build and Run

```powershell
dotnet build -c Release
dotnet run -c Release
```

---

## Default Credentials

- **Username**: `admin`
- **Password**: `Admin@123`

---

## Screenshots

Application screenshots are available in the [ScreenShot/](./ScreenShot/) directory:

- [01_Dashboard.png](./ScreenShot/01_Dashboard.png) - Dashboard and live KPI cards
- [02_Products_Catalog.png](./ScreenShot/02_Products_Catalog.png) - Product catalog and pricing
- [03_Categories.png](./ScreenShot/03_Categories.png) - Product categories
- [04_Warehouses.png](./ScreenShot/04_Warehouses.png) - Warehouse facilities
- [05_Inventory_Stock.png](./ScreenShot/05_Inventory_Stock.png) - Stock levels and ledger
- [06_Stock_Transfers.png](./ScreenShot/06_Stock_Transfers.png) - Inter-warehouse transfers
- [07_Suppliers.png](./ScreenShot/07_Suppliers.png) - Supplier management
- [08_Customers.png](./ScreenShot/08_Customers.png) - Customer accounts
- [09_Purchase_Orders.png](./ScreenShot/09_Purchase_Orders.png) - Purchase orders
- [10_Sales_Orders.png](./ScreenShot/10_Sales_Orders.png) - Sales orders
- [11_Reports_Analytics.png](./ScreenShot/11_Reports_Analytics.png) - Business reports
- [12_Users_Management.png](./ScreenShot/12_Users_Management.png) - User management and permissions
- [13_Audit_Logs.png](./ScreenShot/13_Audit_Logs.png) - System audit trail
- [14_System_Settings.png](./ScreenShot/14_System_Settings.png) - Database and application settings

---

## Repository

- **GitHub Repository**: [https://github.com/XREFS0/Inventory-Management-System](https://github.com/XREFS0/Inventory-Management-System)
- **Author & Copyright**: XREFS0

---

## License

This project is licensed under the [MIT License](LICENSE). All rights reserved to XREFS0.
