Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports MASA.InventoryManagementSystem.Application.Services
Imports MASA.InventoryManagementSystem.Domain.Entities
Imports MASA.InventoryManagementSystem.Domain.Enums
Imports MASA.InventoryManagementSystem.Presentation.Common
Imports MASA.InventoryManagementSystem.Presentation.Controls
Imports MASA.InventoryManagementSystem.Presentation.Forms

Namespace Presentation.Views

    Public Class InventoryView
        Inherits UserControl

        Private ReadOnly _inventoryService As New InventoryService()
        Private ReadOnly _warehouseService As New WarehouseService()

        Private cboWarehouseSelect As ComboBox
        Private btnAdjustStock As Button
        Private btnNewTransfer As Button
        Private dgvWarehouseStock As DataGridView
        Private dgvTransactions As DataGridView
        Private tabControl As TabControl
        Private lblWarehouseTotal As Label

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Inventory & Stock Levels", .Font = UITheme.FontHero, .ForeColor = UITheme.TextPrimary, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            btnNewTransfer = New Button() With {.Text = "Transfer Stock", .Size = New Size(140, 36), .Location = New Point(Width - 300, 2), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UITheme.StyleSecondaryButton(btnNewTransfer)
            AddHandler btnNewTransfer.Click, AddressOf BtnNewTransfer_Click
            pnlHeader.Controls.Add(btnNewTransfer)

            btnAdjustStock = New Button() With {.Text = "Stock Adjustment", .Size = New Size(150, 36), .Location = New Point(Width - 150, 2), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UITheme.StylePrimaryButton(btnAdjustStock)
            AddHandler btnAdjustStock.Click, AddressOf BtnAdjustStock_Click
            pnlHeader.Controls.Add(btnAdjustStock)

            Controls.Add(pnlHeader)

            Dim pnlSelector As New CardPanel() With {.Dock = DockStyle.Top, .Height = 65, .Padding = New Padding(12)}
            Dim lblWh As New Label() With {.Text = "Select Warehouse Facility:", .Location = New Point(12, 18), .AutoSize = True, .Font = UITheme.FontBodyBold}
            pnlSelector.Controls.Add(lblWh)

            cboWarehouseSelect = New ComboBox() With {.Location = New Point(185, 16), .Width = 260, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            AddHandler cboWarehouseSelect.SelectedIndexChanged, AddressOf WarehouseSelectionChanged
            pnlSelector.Controls.Add(cboWarehouseSelect)

            lblWarehouseTotal = New Label() With {.Text = "Total Items: 0", .Location = New Point(470, 18), .AutoSize = True, .ForeColor = UITheme.PrimaryDark, .Font = UITheme.FontBodyBold}
            pnlSelector.Controls.Add(lblWarehouseTotal)

            Controls.Add(pnlSelector)
            pnlSelector.BringToFront()

            Dim pnlCard As New CardPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(12)}

            tabControl = New TabControl() With {.Dock = DockStyle.Fill, .Font = UITheme.FontBodyBold}

            Dim tabStock As New TabPage("Facility Inventory Breakdown")
            dgvWarehouseStock = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyModernDataGridStyle(dgvWarehouseStock)
            dgvWarehouseStock.Columns.Add("Sku", "SKU")
            dgvWarehouseStock.Columns.Add("ProductName", "Product Name")
            dgvWarehouseStock.Columns.Add("Quantity", "In-Stock Quantity")
            dgvWarehouseStock.Columns.Add("MinStock", "Min Threshold")
            dgvWarehouseStock.Columns.Add("CostPrice", "Unit Cost")
            dgvWarehouseStock.Columns.Add("Valuation", "Total Valuation")
            dgvWarehouseStock.Columns.Add("SellingPrice", "Unit Price")
            dgvWarehouseStock.Columns.Add("LastUpdated", "Last Updated")

            dgvWarehouseStock.Columns(0).Width = 120
            dgvWarehouseStock.Columns(1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            dgvWarehouseStock.Columns(2).Width = 130
            dgvWarehouseStock.Columns(3).Width = 110
            dgvWarehouseStock.Columns(4).Width = 100
            dgvWarehouseStock.Columns(5).Width = 120
            dgvWarehouseStock.Columns(6).Width = 100
            dgvWarehouseStock.Columns(7).Width = 130

            tabStock.Controls.Add(dgvWarehouseStock)
            tabControl.TabPages.Add(tabStock)

            Dim tabLedger As New TabPage("Complete Stock Ledger Transactions")
            dgvTransactions = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyModernDataGridStyle(dgvTransactions)
            dgvTransactions.Columns.Add("Date", "Timestamp")
            dgvTransactions.Columns.Add("Sku", "SKU")
            dgvTransactions.Columns.Add("ProductName", "Product Name")
            dgvTransactions.Columns.Add("Action", "Movement Type")
            dgvTransactions.Columns.Add("Qty", "Quantity")
            dgvTransactions.Columns.Add("Prev", "Previous")
            dgvTransactions.Columns.Add("New", "New Balance")
            dgvTransactions.Columns.Add("Ref", "Reference / Notes")
            dgvTransactions.Columns.Add("User", "User")

            dgvTransactions.Columns(0).Width = 130
            dgvTransactions.Columns(1).Width = 110
            dgvTransactions.Columns(2).Width = 180
            dgvTransactions.Columns(3).Width = 120
            dgvTransactions.Columns(4).Width = 70
            dgvTransactions.Columns(5).Width = 70
            dgvTransactions.Columns(6).Width = 90
            dgvTransactions.Columns(7).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            dgvTransactions.Columns(8).Width = 90

            tabLedger.Controls.Add(dgvTransactions)
            tabControl.TabPages.Add(tabLedger)

            pnlCard.Controls.Add(tabControl)
            Controls.Add(pnlCard)
            pnlCard.BringToFront()
        End Sub

        Public Async Sub LoadDataAsync()
            Try
                Dim warehouses = Await _warehouseService.GetAllWarehousesAsync(onlyActive:=True)
                cboWarehouseSelect.Items.Clear()
                For Each wh In warehouses
                    cboWarehouseSelect.Items.Add(New With {.Key = wh.WarehouseId, .Value = $"{wh.Name} ({wh.Code})"})
                Next
                cboWarehouseSelect.DisplayMember = "Value"
                cboWarehouseSelect.ValueMember = "Key"
                If cboWarehouseSelect.Items.Count > 0 Then
                    cboWarehouseSelect.SelectedIndex = 0
                End If

                Await LoadTransactionsAsync()
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Inventory load error: {ex.Message}")
            End Try
        End Sub

        Private Async Sub WarehouseSelectionChanged(sender As Object, e As EventArgs)
            If cboWarehouseSelect.SelectedItem Is Nothing Then Return
            Dim item = CType(cboWarehouseSelect.SelectedItem, Object)
            Dim whId = CInt(item.GetType().GetProperty("Key").GetValue(item, Nothing))

            Dim stocks = Await _inventoryService.GetStockByWarehouseAsync(whId)
            dgvWarehouseStock.Rows.Clear()
            Dim totalUnits As Integer = 0
            Dim totalVal As Decimal = 0

            For Each s In stocks
                totalUnits += s.Quantity
                Dim val = s.Quantity * s.CostPrice
                totalVal += val

                Dim rowIdx = dgvWarehouseStock.Rows.Add(
                    s.ProductSku,
                    s.ProductName,
                    s.Quantity,
                    s.MinStockLevel,
                    s.CostPrice.ToString("C2"),
                    val.ToString("C2"),
                    s.SellingPrice.ToString("C2"),
                    s.LastUpdatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
                )

                If s.Quantity <= s.MinStockLevel Then
                    dgvWarehouseStock.Rows(rowIdx).Cells("Quantity").Style.ForeColor = UITheme.Danger
                    dgvWarehouseStock.Rows(rowIdx).Cells("Quantity").Style.Font = UITheme.FontBodyBold
                End If
            Next

            lblWarehouseTotal.Text = $"Total Units: {totalUnits:N0}  |  Facility Stock Valuation: {totalVal:C2}"
        End Sub

        Private Async Function LoadTransactionsAsync() As Task
            Dim txList = Await _inventoryService.GetRecentTransactionsAsync(100)
            dgvTransactions.Rows.Clear()
            For Each tx In txList
                Dim notesFull = If(String.IsNullOrWhiteSpace(tx.ReferenceId), tx.Notes, $"{tx.ReferenceType} ({tx.ReferenceId}) - {tx.Notes}")
                dgvTransactions.Rows.Add(
                    tx.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    tx.ProductSku,
                    tx.ProductName,
                    tx.TransactionType,
                    tx.Quantity,
                    tx.PreviousQty,
                    tx.NewQty,
                    notesFull,
                    tx.CreatedBy
                )
            Next
        End Function

        Private Async Sub BtnAdjustStock_Click(sender As Object, e As EventArgs)
            Dim whId As Integer = 0
            If cboWarehouseSelect.SelectedItem IsNot Nothing Then
                Dim item = CType(cboWarehouseSelect.SelectedItem, Object)
                whId = CInt(item.GetType().GetProperty("Key").GetValue(item, Nothing))
            End If

            Using dlg As New StockAdjustmentDialog(preselectedWarehouseId:=whId)
                If dlg.ShowDialog() = DialogResult.OK Then
                    WarehouseSelectionChanged(Nothing, EventArgs.Empty)
                    Await LoadTransactionsAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnNewTransfer_Click(sender As Object, e As EventArgs)
            Using dlg As New NewTransferDialog()
                If dlg.ShowDialog() = DialogResult.OK Then
                    WarehouseSelectionChanged(Nothing, EventArgs.Empty)
                    Await LoadTransactionsAsync()
                End If
            End Using
        End Sub
    End Class

    Public Class TransfersView
        Inherits UserControl

        Private ReadOnly _inventoryService As New InventoryService()
        Private dgvTransfers As DataGridView
        Private btnNewTransfer As Button

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Inter-Warehouse Stock Transfers", .Font = UITheme.FontHero, .ForeColor = UITheme.TextPrimary, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            btnNewTransfer = New Button() With {.Text = "+ New Transfer Order", .Size = New Size(170, 36), .Location = New Point(Width - 194, 2), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UITheme.StylePrimaryButton(btnNewTransfer)
            AddHandler btnNewTransfer.Click, AddressOf BtnNewTransfer_Click
            pnlHeader.Controls.Add(btnNewTransfer)
            Controls.Add(pnlHeader)

            Dim pnlContent As New CardPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(16)}

            dgvTransfers = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyModernDataGridStyle(dgvTransfers)
            dgvTransfers.Columns.Add("TransferNumber", "Transfer #")
            dgvTransfers.Columns.Add("Date", "Transfer Date")
            dgvTransfers.Columns.Add("Source", "Origin Warehouse")
            dgvTransfers.Columns.Add("Destination", "Destination Warehouse")
            dgvTransfers.Columns.Add("Status", "Status")
            dgvTransfers.Columns.Add("User", "Authorized By")
            dgvTransfers.Columns.Add("Notes", "Reference Notes")

            dgvTransfers.Columns(0).Width = 180
            dgvTransfers.Columns(1).Width = 140
            dgvTransfers.Columns(2).Width = 180
            dgvTransfers.Columns(3).Width = 180
            dgvTransfers.Columns(4).Width = 110
            dgvTransfers.Columns(5).Width = 110
            dgvTransfers.Columns(6).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill

            pnlContent.Controls.Add(dgvTransfers)
            Controls.Add(pnlContent)
            pnlContent.BringToFront()
        End Sub

        Public Async Sub LoadDataAsync()
            Try
                Dim transfers = Await _inventoryService.GetStockTransfersAsync()
                dgvTransfers.Rows.Clear()
                For Each t In transfers
                    dgvTransfers.Rows.Add(
                        t.TransferNumber,
                        t.TransferDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        t.SourceWarehouseName,
                        t.DestinationWarehouseName,
                        t.Status,
                        t.CreatedBy,
                        t.Notes
                    )
                Next
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Transfers load error: {ex.Message}")
            End Try
        End Sub

        Private Sub BtnNewTransfer_Click(sender As Object, e As EventArgs)
            Using dlg As New NewTransferDialog()
                If dlg.ShowDialog() = DialogResult.OK Then
                    LoadDataAsync()
                End If
            End Using
        End Sub
    End Class

    Public Class SuppliersView
        Inherits UserControl

        Private ReadOnly _supplierService As New SupplierService()
        Private dgvSuppliers As DataGridView
        Private btnNewSupplier As Button
        Private btnEditSupplier As Button
        Private btnDeactivateSupplier As Button

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Supplier & Vendor Directory", .Font = UITheme.FontHero, .ForeColor = UITheme.TextPrimary, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            btnNewSupplier = New Button() With {.Text = "+ Register Supplier", .Size = New Size(150, 36), .Location = New Point(Width - 174, 2), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UITheme.StylePrimaryButton(btnNewSupplier)
            AddHandler btnNewSupplier.Click, AddressOf BtnNewSupplier_Click
            pnlHeader.Controls.Add(btnNewSupplier)
            Controls.Add(pnlHeader)

            Dim pnlContent As New CardPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(16)}

            Dim pnlActions As New Panel() With {.Dock = DockStyle.Top, .Height = 40}
            btnEditSupplier = New Button() With {.Text = "Edit Selected", .Size = New Size(120, 32), .Location = New Point(0, 2)}
            UITheme.StyleSecondaryButton(btnEditSupplier)
            AddHandler btnEditSupplier.Click, AddressOf BtnEditSupplier_Click
            pnlActions.Controls.Add(btnEditSupplier)

            btnDeactivateSupplier = New Button() With {.Text = "Deactivate", .Size = New Size(110, 32), .Location = New Point(130, 2)}
            UITheme.StyleDangerButton(btnDeactivateSupplier)
            AddHandler btnDeactivateSupplier.Click, AddressOf BtnDeactivateSupplier_Click
            pnlActions.Controls.Add(btnDeactivateSupplier)

            pnlContent.Controls.Add(pnlActions)

            dgvSuppliers = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyModernDataGridStyle(dgvSuppliers)
            dgvSuppliers.Columns.Add("SupplierId", "ID")
            dgvSuppliers.Columns.Add("Name", "Supplier Name")
            dgvSuppliers.Columns.Add("Company", "Company")
            dgvSuppliers.Columns.Add("Contact", "Contact Person")
            dgvSuppliers.Columns.Add("Email", "Email")
            dgvSuppliers.Columns.Add("Phone", "Phone")
            dgvSuppliers.Columns.Add("Tax", "Tax ID")
            dgvSuppliers.Columns.Add("Status", "Status")

            dgvSuppliers.Columns(0).Visible = False
            dgvSuppliers.Columns(1).Width = 180
            dgvSuppliers.Columns(2).Width = 180
            dgvSuppliers.Columns(3).Width = 140
            dgvSuppliers.Columns(4).Width = 180
            dgvSuppliers.Columns(5).Width = 130
            dgvSuppliers.Columns(6).Width = 120
            dgvSuppliers.Columns(7).Width = 85

            AddHandler dgvSuppliers.CellDoubleClick, AddressOf DgvSuppliers_CellDoubleClick

            pnlContent.Controls.Add(dgvSuppliers)
            dgvSuppliers.BringToFront()

            Controls.Add(pnlContent)
            pnlContent.BringToFront()
        End Sub

        Public Async Sub LoadDataAsync()
            Try
                Dim suppliers = Await _supplierService.GetAllSuppliersAsync()
                dgvSuppliers.Rows.Clear()
                For Each s In suppliers
                    dgvSuppliers.Rows.Add(
                        s.SupplierId,
                        s.Name,
                        s.CompanyName,
                        s.ContactPerson,
                        s.Email,
                        s.Phone,
                        s.TaxNumber,
                        If(s.IsActive, "Active", "Archived")
                    )
                Next
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Suppliers load error: {ex.Message}")
            End Try
        End Sub

        Private Sub BtnNewSupplier_Click(sender As Object, e As EventArgs)
            Using dlg As New SupplierEditDialog()
                If dlg.ShowDialog() = DialogResult.OK Then
                    LoadDataAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnEditSupplier_Click(sender As Object, e As EventArgs)
            If dgvSuppliers.SelectedRows.Count = 0 Then Return
            Dim supId = Convert.ToInt32(dgvSuppliers.SelectedRows(0).Cells("SupplierId").Value)
            Dim suppliers = Await _supplierService.GetAllSuppliersAsync()
            Dim sup = suppliers.Find(Function(s) s.SupplierId = supId)
            If sup IsNot Nothing Then
                Using dlg As New SupplierEditDialog(sup)
                    If dlg.ShowDialog() = DialogResult.OK Then
                        LoadDataAsync()
                    End If
                End Using
            End If
        End Sub

        Private Sub DgvSuppliers_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 Then BtnEditSupplier_Click(sender, EventArgs.Empty)
        End Sub

        Private Async Sub BtnDeactivateSupplier_Click(sender As Object, e As EventArgs)
            If dgvSuppliers.SelectedRows.Count = 0 Then Return
            Dim supId = Convert.ToInt32(dgvSuppliers.SelectedRows(0).Cells("SupplierId").Value)
            Dim name = dgvSuppliers.SelectedRows(0).Cells("Name").Value.ToString()

            If MessageBox.Show($"Deactivate supplier '{name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                Dim res = Await _supplierService.DeactivateSupplierAsync(supId)
                If res.Success Then
                    LoadDataAsync()
                Else
                    MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            End If
        End Sub
    End Class

    Public Class CustomersView
        Inherits UserControl

        Private ReadOnly _customerService As New CustomerService()
        Private dgvCustomers As DataGridView
        Private btnNewCustomer As Button
        Private btnEditCustomer As Button
        Private btnDeactivateCustomer As Button

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Customer & Client Management", .Font = UITheme.FontHero, .ForeColor = UITheme.TextPrimary, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            btnNewCustomer = New Button() With {.Text = "+ Register Customer", .Size = New Size(160, 36), .Location = New Point(Width - 184, 2), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UITheme.StylePrimaryButton(btnNewCustomer)
            AddHandler btnNewCustomer.Click, AddressOf BtnNewCustomer_Click
            pnlHeader.Controls.Add(btnNewCustomer)
            Controls.Add(pnlHeader)

            Dim pnlContent As New CardPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(16)}

            Dim pnlActions As New Panel() With {.Dock = DockStyle.Top, .Height = 40}
            btnEditCustomer = New Button() With {.Text = "Edit Selected", .Size = New Size(120, 32), .Location = New Point(0, 2)}
            UITheme.StyleSecondaryButton(btnEditCustomer)
            AddHandler btnEditCustomer.Click, AddressOf BtnEditCustomer_Click
            pnlActions.Controls.Add(btnEditCustomer)

            btnDeactivateCustomer = New Button() With {.Text = "Deactivate", .Size = New Size(110, 32), .Location = New Point(130, 2)}
            UITheme.StyleDangerButton(btnDeactivateCustomer)
            AddHandler btnDeactivateCustomer.Click, AddressOf BtnDeactivateCustomer_Click
            pnlActions.Controls.Add(btnDeactivateCustomer)

            pnlContent.Controls.Add(pnlActions)

            dgvCustomers = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyModernDataGridStyle(dgvCustomers)
            dgvCustomers.Columns.Add("CustomerId", "ID")
            dgvCustomers.Columns.Add("FullName", "Customer Full Name")
            dgvCustomers.Columns.Add("Company", "Company / Organization")
            dgvCustomers.Columns.Add("Email", "Email")
            dgvCustomers.Columns.Add("Phone", "Phone")
            dgvCustomers.Columns.Add("Tax", "Tax ID")
            dgvCustomers.Columns.Add("Status", "Status")

            dgvCustomers.Columns(0).Visible = False
            dgvCustomers.Columns(1).Width = 200
            dgvCustomers.Columns(2).Width = 200
            dgvCustomers.Columns(3).Width = 180
            dgvCustomers.Columns(4).Width = 140
            dgvCustomers.Columns(5).Width = 120
            dgvCustomers.Columns(6).Width = 85

            AddHandler dgvCustomers.CellDoubleClick, AddressOf DgvCustomers_CellDoubleClick

            pnlContent.Controls.Add(dgvCustomers)
            dgvCustomers.BringToFront()

            Controls.Add(pnlContent)
            pnlContent.BringToFront()
        End Sub

        Public Async Sub LoadDataAsync()
            Try
                Dim customers = Await _customerService.GetAllCustomersAsync()
                dgvCustomers.Rows.Clear()
                For Each c In customers
                    dgvCustomers.Rows.Add(
                        c.CustomerId,
                        c.FullName,
                        c.CompanyName,
                        c.Email,
                        c.Phone,
                        c.TaxNumber,
                        If(c.IsActive, "Active", "Archived")
                    )
                Next
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Customers load error: {ex.Message}")
            End Try
        End Sub

        Private Sub BtnNewCustomer_Click(sender As Object, e As EventArgs)
            Using dlg As New CustomerEditDialog()
                If dlg.ShowDialog() = DialogResult.OK Then
                    LoadDataAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnEditCustomer_Click(sender As Object, e As EventArgs)
            If dgvCustomers.SelectedRows.Count = 0 Then Return
            Dim custId = Convert.ToInt32(dgvCustomers.SelectedRows(0).Cells("CustomerId").Value)
            Dim customers = Await _customerService.GetAllCustomersAsync()
            Dim cust = customers.Find(Function(c) c.CustomerId = custId)
            If cust IsNot Nothing Then
                Using dlg As New CustomerEditDialog(cust)
                    If dlg.ShowDialog() = DialogResult.OK Then
                        LoadDataAsync()
                    End If
                End Using
            End If
        End Sub

        Private Sub DgvCustomers_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 Then BtnEditCustomer_Click(sender, EventArgs.Empty)
        End Sub

        Private Async Sub BtnDeactivateCustomer_Click(sender As Object, e As EventArgs)
            If dgvCustomers.SelectedRows.Count = 0 Then Return
            Dim custId = Convert.ToInt32(dgvCustomers.SelectedRows(0).Cells("CustomerId").Value)
            Dim name = dgvCustomers.SelectedRows(0).Cells("FullName").Value.ToString()

            If MessageBox.Show($"Deactivate customer '{name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                Dim res = Await _customerService.DeactivateCustomerAsync(custId)
                If res.Success Then
                    LoadDataAsync()
                Else
                    MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            End If
        End Sub
    End Class

    Public Class PurchasesView
        Inherits UserControl

        Private ReadOnly _purchaseService As New PurchaseService()
        Private dgvPurchases As DataGridView
        Private btnNewPo As Button
        Private btnReceivePo As Button
        Private btnConfirmPo As Button
        Private cboStatusFilter As ComboBox

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Purchase Orders & Receiving", .Font = UITheme.FontHero, .ForeColor = UITheme.TextPrimary, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            btnNewPo = New Button() With {.Text = "+ Create Purchase Order", .Size = New Size(180, 36), .Location = New Point(Width - 204, 2), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UITheme.StylePrimaryButton(btnNewPo)
            AddHandler btnNewPo.Click, AddressOf BtnNewPo_Click
            pnlHeader.Controls.Add(btnNewPo)
            Controls.Add(pnlHeader)

            Dim pnlContent As New CardPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(16)}

            Dim pnlActions As New Panel() With {.Dock = DockStyle.Top, .Height = 40}

            btnConfirmPo = New Button() With {.Text = "Confirm Order", .Size = New Size(130, 32), .Location = New Point(0, 2)}
            UITheme.StyleSecondaryButton(btnConfirmPo)
            AddHandler btnConfirmPo.Click, AddressOf BtnConfirmPo_Click
            pnlActions.Controls.Add(btnConfirmPo)

            btnReceivePo = New Button() With {.Text = "Receive Goods (Update Stock)", .Size = New Size(220, 32), .Location = New Point(140, 2)}
            UITheme.StyleSuccessButton(btnReceivePo)
            AddHandler btnReceivePo.Click, AddressOf BtnReceivePo_Click
            pnlActions.Controls.Add(btnReceivePo)

            Dim lblFilter As New Label() With {.Text = "Filter Status:", .Location = New Point(380, 8), .AutoSize = True, .Font = UITheme.FontBodyBold}
            pnlActions.Controls.Add(lblFilter)

            cboStatusFilter = New ComboBox() With {.Location = New Point(470, 6), .Width = 140, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            cboStatusFilter.Items.AddRange(New Object() {"All Statuses", "Draft", "Confirmed", "Received", "Cancelled"})
            cboStatusFilter.SelectedIndex = 0
            AddHandler cboStatusFilter.SelectedIndexChanged, AddressOf FilterChanged
            pnlActions.Controls.Add(cboStatusFilter)

            pnlContent.Controls.Add(pnlActions)

            dgvPurchases = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyModernDataGridStyle(dgvPurchases)
            dgvPurchases.Columns.Add("PoId", "ID")
            dgvPurchases.Columns.Add("PoNumber", "PO Number")
            dgvPurchases.Columns.Add("Supplier", "Supplier")
            dgvPurchases.Columns.Add("Warehouse", "Destination Facility")
            dgvPurchases.Columns.Add("OrderDate", "Order Date")
            dgvPurchases.Columns.Add("ExpectedDate", "Expected Date")
            dgvPurchases.Columns.Add("Total", "Total Amount")
            dgvPurchases.Columns.Add("Status", "Status")
            dgvPurchases.Columns.Add("CreatedBy", "Created By")

            dgvPurchases.Columns(0).Visible = False
            dgvPurchases.Columns(1).Width = 160
            dgvPurchases.Columns(2).Width = 180
            dgvPurchases.Columns(3).Width = 180
            dgvPurchases.Columns(4).Width = 110
            dgvPurchases.Columns(5).Width = 110
            dgvPurchases.Columns(6).Width = 120
            dgvPurchases.Columns(7).Width = 110
            dgvPurchases.Columns(8).Width = 100

            pnlContent.Controls.Add(dgvPurchases)
            dgvPurchases.BringToFront()

            Controls.Add(pnlContent)
            pnlContent.BringToFront()
        End Sub

        Public Async Sub LoadDataAsync()
            Try
                Dim statusFilter = If(cboStatusFilter.SelectedIndex > 0, cboStatusFilter.SelectedItem.ToString(), "")
                Dim orders = Await _purchaseService.GetAllOrdersAsync(statusFilter)
                dgvPurchases.Rows.Clear()
                For Each po In orders
                    Dim rowIdx = dgvPurchases.Rows.Add(
                        po.PoId,
                        po.PoNumber,
                        po.SupplierName,
                        po.WarehouseName,
                        po.OrderDate.ToString("yyyy-MM-dd"),
                        If(po.ExpectedDate.HasValue, po.ExpectedDate.Value.ToString("yyyy-MM-dd"), "--"),
                        po.TotalAmount.ToString("C2"),
                        po.Status,
                        po.CreatedBy
                    )

                    If String.Equals(po.Status, "Received", StringComparison.OrdinalIgnoreCase) Then
                        dgvPurchases.Rows(rowIdx).Cells("Status").Style.ForeColor = UITheme.Success
                        dgvPurchases.Rows(rowIdx).Cells("Status").Style.Font = UITheme.FontBodyBold
                    ElseIf String.Equals(po.Status, "Draft", StringComparison.OrdinalIgnoreCase) Then
                        dgvPurchases.Rows(rowIdx).Cells("Status").Style.ForeColor = UITheme.TextSecondary
                    End If
                Next
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Purchases load error: {ex.Message}")
            End Try
        End Sub

        Private Sub FilterChanged(sender As Object, e As EventArgs)
            LoadDataAsync()
        End Sub

        Private Sub BtnNewPo_Click(sender As Object, e As EventArgs)
            Using dlg As New NewPurchaseOrderDialog()
                If dlg.ShowDialog() = DialogResult.OK Then
                    LoadDataAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnConfirmPo_Click(sender As Object, e As EventArgs)
            If dgvPurchases.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select a purchase order.", "Selection", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim poId = Convert.ToInt32(dgvPurchases.SelectedRows(0).Cells("PoId").Value)
            Dim status = dgvPurchases.SelectedRows(0).Cells("Status").Value.ToString()

            If Not String.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase) Then
                MessageBox.Show("Only draft purchase orders can be confirmed.", "Status", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim res = Await _purchaseService.UpdateStatusAsync(poId, "Confirmed")
            If res.Success Then
                LoadDataAsync()
            Else
                MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        End Sub

        Private Async Sub BtnReceivePo_Click(sender As Object, e As EventArgs)
            If dgvPurchases.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select a purchase order to receive.", "Selection", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim poId = Convert.ToInt32(dgvPurchases.SelectedRows(0).Cells("PoId").Value)
            Dim poNum = dgvPurchases.SelectedRows(0).Cells("PoNumber").Value.ToString()
            Dim status = dgvPurchases.SelectedRows(0).Cells("Status").Value.ToString()

            If String.Equals(status, "Received", StringComparison.OrdinalIgnoreCase) Then
                MessageBox.Show("This purchase order is already marked as received.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            If MessageBox.Show($"Receive goods for Purchase Order '{poNum}'?{vbCrLf}This will increment inventory stock in the receiving facility.", "Confirm Goods Receipt", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                Dim res = Await _purchaseService.ReceivePurchaseOrderAsync(poId)
                If res.Success Then
                    MessageBox.Show(res.Message, "Goods Received", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    LoadDataAsync()
                Else
                    MessageBox.Show(res.Message, "Receiving Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            End If
        End Sub
    End Class

    Public Class SalesView
        Inherits UserControl

        Private ReadOnly _salesService As New SalesService()
        Private dgvSales As DataGridView
        Private btnNewSo As Button
        Private btnCompleteSo As Button
        Private btnConfirmSo As Button
        Private cboStatusFilter As ComboBox

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Sales Orders & Fulfillment", .Font = UITheme.FontHero, .ForeColor = UITheme.TextPrimary, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            btnNewSo = New Button() With {.Text = "+ Create Sales Order", .Size = New Size(170, 36), .Location = New Point(Width - 194, 2), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UITheme.StylePrimaryButton(btnNewSo)
            AddHandler btnNewSo.Click, AddressOf BtnNewSo_Click
            pnlHeader.Controls.Add(btnNewSo)
            Controls.Add(pnlHeader)

            Dim pnlContent As New CardPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(16)}

            Dim pnlActions As New Panel() With {.Dock = DockStyle.Top, .Height = 40}

            btnConfirmSo = New Button() With {.Text = "Confirm Order", .Size = New Size(130, 32), .Location = New Point(0, 2)}
            UITheme.StyleSecondaryButton(btnConfirmSo)
            AddHandler btnConfirmSo.Click, AddressOf BtnConfirmSo_Click
            pnlActions.Controls.Add(btnConfirmSo)

            btnCompleteSo = New Button() With {.Text = "Fulfill / Dispatch (Deduct Stock)", .Size = New Size(240, 32), .Location = New Point(140, 2)}
            UITheme.StyleSuccessButton(btnCompleteSo)
            AddHandler btnCompleteSo.Click, AddressOf BtnCompleteSo_Click
            pnlActions.Controls.Add(btnCompleteSo)

            Dim lblFilter As New Label() With {.Text = "Filter Status:", .Location = New Point(400, 8), .AutoSize = True, .Font = UITheme.FontBodyBold}
            pnlActions.Controls.Add(lblFilter)

            cboStatusFilter = New ComboBox() With {.Location = New Point(490, 6), .Width = 140, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            cboStatusFilter.Items.AddRange(New Object() {"All Statuses", "Draft", "Confirmed", "Completed", "Cancelled"})
            cboStatusFilter.SelectedIndex = 0
            AddHandler cboStatusFilter.SelectedIndexChanged, AddressOf FilterChanged
            pnlActions.Controls.Add(cboStatusFilter)

            pnlContent.Controls.Add(pnlActions)

            dgvSales = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyModernDataGridStyle(dgvSales)
            dgvSales.Columns.Add("SoId", "ID")
            dgvSales.Columns.Add("SoNumber", "SO Number")
            dgvSales.Columns.Add("Customer", "Customer")
            dgvSales.Columns.Add("Warehouse", "Issuing Warehouse")
            dgvSales.Columns.Add("OrderDate", "Order Date")
            dgvSales.Columns.Add("Total", "Total Amount")
            dgvSales.Columns.Add("Payment", "Payment")
            dgvSales.Columns.Add("Status", "Status")
            dgvSales.Columns.Add("CreatedBy", "Sales Rep")

            dgvSales.Columns(0).Visible = False
            dgvSales.Columns(1).Width = 160
            dgvSales.Columns(2).Width = 180
            dgvSales.Columns(3).Width = 180
            dgvSales.Columns(4).Width = 110
            dgvSales.Columns(5).Width = 120
            dgvSales.Columns(6).Width = 100
            dgvSales.Columns(7).Width = 110
            dgvSales.Columns(8).Width = 100

            pnlContent.Controls.Add(dgvSales)
            dgvSales.BringToFront()

            Controls.Add(pnlContent)
            pnlContent.BringToFront()
        End Sub

        Public Async Sub LoadDataAsync()
            Try
                Dim statusFilter = If(cboStatusFilter.SelectedIndex > 0, cboStatusFilter.SelectedItem.ToString(), "")
                Dim orders = Await _salesService.GetAllOrdersAsync(statusFilter)
                dgvSales.Rows.Clear()
                For Each so In orders
                    Dim rowIdx = dgvSales.Rows.Add(
                        so.SoId,
                        so.SoNumber,
                        so.CustomerName,
                        so.WarehouseName,
                        so.OrderDate.ToString("yyyy-MM-dd"),
                        so.TotalAmount.ToString("C2"),
                        so.PaymentStatus,
                        so.Status,
                        so.CreatedBy
                    )

                    If String.Equals(so.Status, "Completed", StringComparison.OrdinalIgnoreCase) Then
                        dgvSales.Rows(rowIdx).Cells("Status").Style.ForeColor = UITheme.Success
                        dgvSales.Rows(rowIdx).Cells("Status").Style.Font = UITheme.FontBodyBold
                    End If
                Next
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Sales load error: {ex.Message}")
            End Try
        End Sub

        Private Sub FilterChanged(sender As Object, e As EventArgs)
            LoadDataAsync()
        End Sub

        Private Sub BtnNewSo_Click(sender As Object, e As EventArgs)
            Using dlg As New NewSalesOrderDialog()
                If dlg.ShowDialog() = DialogResult.OK Then
                    LoadDataAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnConfirmSo_Click(sender As Object, e As EventArgs)
            If dgvSales.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select a sales order.", "Selection", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim soId = Convert.ToInt32(dgvSales.SelectedRows(0).Cells("SoId").Value)
            Dim status = dgvSales.SelectedRows(0).Cells("Status").Value.ToString()

            If Not String.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase) Then
                MessageBox.Show("Only draft sales orders can be confirmed.", "Status", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim res = Await _salesService.UpdateStatusAsync(soId, "Confirmed")
            If res.Success Then
                LoadDataAsync()
            Else
                MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
        End Sub

        Private Async Sub BtnCompleteSo_Click(sender As Object, e As EventArgs)
            If dgvSales.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select a sales order to fulfill.", "Selection", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim soId = Convert.ToInt32(dgvSales.SelectedRows(0).Cells("SoId").Value)
            Dim soNum = dgvSales.SelectedRows(0).Cells("SoNumber").Value.ToString()
            Dim status = dgvSales.SelectedRows(0).Cells("Status").Value.ToString()

            If String.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase) Then
                MessageBox.Show("This sales order is already completed.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            If MessageBox.Show($"Fulfill Sales Order '{soNum}'?{vbCrLf}This will verify stock availability and deduct units from the issuing warehouse.", "Confirm Fulfillment", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                Dim res = Await _salesService.CompleteSalesOrderAsync(soId)
                If res.Success Then
                    MessageBox.Show(res.Message, "Order Completed", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    LoadDataAsync()
                Else
                    MessageBox.Show(res.Message, "Fulfillment Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            End If
        End Sub
    End Class

End Namespace
