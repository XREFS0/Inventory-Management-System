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
Imports MASA.InventoryManagementSystem.Infrastructure.Configuration
Imports MASA.InventoryManagementSystem.Infrastructure.Security
Imports MASA.InventoryManagementSystem.Presentation.Common
Imports MASA.InventoryManagementSystem.Presentation.Controls
Imports MASA.InventoryManagementSystem.Presentation.Forms

Namespace Presentation.Views

    Public Class DashboardView
        Inherits UserControl

        Private ReadOnly _reportService As New ReportService()
        Private ReadOnly _inventoryService As New InventoryService()

        Private statProducts As StatCard
        Private statTotalUnits As StatCard
        Private statValuation As StatCard
        Private statLowStock As StatCard
        Private statSales As StatCard
        Private statPurchases As StatCard
        Private dgvRecentTx As DataGridView
        Private btnRefresh As Button

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            AutoScroll = True
            Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {
                .Dock = DockStyle.Top,
                .Height = 50
            }

            Dim lblTitle As New Label() With {
                .Text = "Inventory Operations Dashboard",
                .Font = UITheme.FontHero,
                .ForeColor = UITheme.TextPrimary,
                .Location = New Point(0, 0),
                .AutoSize = True
            }
            pnlHeader.Controls.Add(lblTitle)

            Dim lblSubtitle As New Label() With {
                .Text = "Live operational analytics, inventory health, and stock movement telemetry",
                .Font = UITheme.FontSmall,
                .ForeColor = UITheme.TextSecondary,
                .Location = New Point(0, 32),
                .AutoSize = True
            }
            pnlHeader.Controls.Add(lblSubtitle)

            btnRefresh = New Button() With {
                .Text = "Refresh Data",
                .Size = New Size(120, 34),
                .Location = New Point(Width - 144, 4),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right
            }
            UITheme.StyleSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, AddressOf BtnRefresh_Click
            pnlHeader.Controls.Add(btnRefresh)

            Controls.Add(pnlHeader)

            Dim pnlKpi As New FlowLayoutPanel() With {
                .Dock = DockStyle.Top,
                .Height = 240,
                .AutoScroll = False,
                .Padding = New Padding(0, 16, 0, 0)
            }

            statProducts = New StatCard() With {.CardTitle = "Active Catalog", .CardValue = "--", .Subtitle = "Total registered products", .AccentColor = UITheme.Primary}
            statTotalUnits = New StatCard() With {.CardTitle = "In-Stock Balance", .CardValue = "--", .Subtitle = "Total physical units", .AccentColor = UITheme.Info}
            statValuation = New StatCard() With {.CardTitle = "Inventory Valuation", .CardValue = "--", .Subtitle = "Cost price basis", .AccentColor = UITheme.Success}
            statLowStock = New StatCard() With {.CardTitle = "Low Stock Alerts", .CardValue = "--", .Subtitle = "SKUs at/below threshold", .AccentColor = UITheme.Danger}
            statSales = New StatCard() With {.CardTitle = "Completed Sales", .CardValue = "--", .Subtitle = "Total revenue", .AccentColor = Color.FromArgb(139, 92, 246)}
            statPurchases = New StatCard() With {.CardTitle = "Goods Received", .CardValue = "--", .Subtitle = "Total spend", .AccentColor = Color.FromArgb(249, 115, 22)}

            pnlKpi.Controls.AddRange(New Control() {statProducts, statTotalUnits, statValuation, statLowStock, statSales, statPurchases})
            Controls.Add(pnlKpi)
            pnlKpi.BringToFront()

            Dim pnlRecent As New CardPanel() With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(16)
            }

            Dim lblGridHeader As New Label() With {
                .Text = "Recent Inventory Ledger Transactions",
                .Font = UITheme.FontSubtitle,
                .ForeColor = UITheme.TextPrimary,
                .Dock = DockStyle.Top,
                .Height = 30
            }
            pnlRecent.Controls.Add(lblGridHeader)

            dgvRecentTx = New DataGridView() With {
                .Dock = DockStyle.Fill
            }
            UITheme.ApplyModernDataGridStyle(dgvRecentTx)
            dgvRecentTx.Columns.Add("CreatedAt", "Timestamp")
            dgvRecentTx.Columns.Add("Sku", "SKU")
            dgvRecentTx.Columns.Add("ProductName", "Product")
            dgvRecentTx.Columns.Add("Warehouse", "Warehouse")
            dgvRecentTx.Columns.Add("Action", "Action")
            dgvRecentTx.Columns.Add("Qty", "Quantity")
            dgvRecentTx.Columns.Add("NewQty", "Balance")
            dgvRecentTx.Columns.Add("Ref", "Reference")
            dgvRecentTx.Columns.Add("User", "User")

            dgvRecentTx.Columns(0).Width = 140
            dgvRecentTx.Columns(1).Width = 110
            dgvRecentTx.Columns(2).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            dgvRecentTx.Columns(3).Width = 140
            dgvRecentTx.Columns(4).Width = 120
            dgvRecentTx.Columns(5).Width = 70
            dgvRecentTx.Columns(6).Width = 70
            dgvRecentTx.Columns(7).Width = 120
            dgvRecentTx.Columns(8).Width = 90

            pnlRecent.Controls.Add(dgvRecentTx)
            dgvRecentTx.BringToFront()

            Controls.Add(pnlRecent)
            pnlRecent.BringToFront()
        End Sub

        Public Async Sub LoadDataAsync()
            Try
                Dim summary = Await _reportService.GetDashboardSummaryAsync()
                statProducts.CardValue = summary.TotalProductsCount.ToString("N0")
                statTotalUnits.CardValue = summary.TotalStockUnits.ToString("N0")
                statValuation.CardValue = summary.TotalInventoryValue.ToString("C0")
                statLowStock.CardValue = summary.LowStockCount.ToString("N0")
                statSales.CardValue = summary.TotalSalesAmount.ToString("C0")
                statPurchases.CardValue = summary.TotalPurchasesAmount.ToString("C0")

                Dim transactions = Await _inventoryService.GetRecentTransactionsAsync(30)
                dgvRecentTx.Rows.Clear()
                For Each tx In transactions
                    dgvRecentTx.Rows.Add(
                        tx.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        tx.ProductSku,
                        tx.ProductName,
                        tx.WarehouseName,
                        tx.TransactionType,
                        tx.Quantity,
                        tx.NewQty,
                        If(String.IsNullOrEmpty(tx.ReferenceId), tx.ReferenceType, tx.ReferenceId),
                        tx.CreatedBy
                    )
                Next
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Dashboard load error: {ex.Message}")
            End Try
        End Sub

        Private Sub BtnRefresh_Click(sender As Object, e As EventArgs)
            LoadDataAsync()
        End Sub
    End Class

    Public Class ProductsView
        Inherits UserControl

        Private ReadOnly _productService As New ProductService()
        Private ReadOnly _categoryService As New CategoryService()

        Private txtSearch As TextBox
        Private cboFilterCategory As ComboBox
        Private chkOnlyActive As CheckBox
        Private btnNewProduct As Button
        Private btnEditProduct As Button
        Private btnAdjustStock As Button
        Private btnDeactivate As Button
        Private dgvProducts As DataGridView
        Private lblRecordCount As Label

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Product Inventory Catalog", .Font = UITheme.FontHero, .ForeColor = UITheme.TextPrimary, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            btnNewProduct = New Button() With {.Text = "+ Create Product", .Size = New Size(140, 36), .Location = New Point(Width - 164, 2), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UITheme.StylePrimaryButton(btnNewProduct)
            AddHandler btnNewProduct.Click, AddressOf BtnNewProduct_Click
            pnlHeader.Controls.Add(btnNewProduct)
            Controls.Add(pnlHeader)

            Dim pnlFilter As New CardPanel() With {.Dock = DockStyle.Top, .Height = 65, .Padding = New Padding(12)}
            Dim lblSearch As New Label() With {.Text = "Search SKU / Name:", .Location = New Point(12, 18), .AutoSize = True, .Font = UITheme.FontBodyBold}
            pnlFilter.Controls.Add(lblSearch)

            txtSearch = New TextBox() With {.Location = New Point(150, 16), .Width = 220, .Font = UITheme.FontBody}
            AddHandler txtSearch.TextChanged, AddressOf FilterChanged
            pnlFilter.Controls.Add(txtSearch)

            Dim lblCat As New Label() With {.Text = "Category:", .Location = New Point(390, 18), .AutoSize = True, .Font = UITheme.FontBodyBold}
            pnlFilter.Controls.Add(lblCat)

            cboFilterCategory = New ComboBox() With {.Location = New Point(460, 16), .Width = 180, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            AddHandler cboFilterCategory.SelectedIndexChanged, AddressOf FilterChanged
            pnlFilter.Controls.Add(cboFilterCategory)

            chkOnlyActive = New CheckBox() With {.Text = "Active SKUs Only", .Location = New Point(660, 18), .AutoSize = True, .Checked = True, .Font = UITheme.FontBodyBold}
            AddHandler chkOnlyActive.CheckedChanged, AddressOf FilterChanged
            pnlFilter.Controls.Add(chkOnlyActive)

            Controls.Add(pnlFilter)
            pnlFilter.BringToFront()

            Dim pnlContent As New CardPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(16)}

            Dim pnlActions As New Panel() With {.Dock = DockStyle.Top, .Height = 40}
            btnEditProduct = New Button() With {.Text = "Edit Selected", .Size = New Size(120, 32), .Location = New Point(0, 2)}
            UITheme.StyleSecondaryButton(btnEditProduct)
            AddHandler btnEditProduct.Click, AddressOf BtnEditProduct_Click
            pnlActions.Controls.Add(btnEditProduct)

            btnAdjustStock = New Button() With {.Text = "Stock Adjustment", .Size = New Size(140, 32), .Location = New Point(130, 2)}
            UITheme.StyleSecondaryButton(btnAdjustStock)
            AddHandler btnAdjustStock.Click, AddressOf BtnAdjustStock_Click
            pnlActions.Controls.Add(btnAdjustStock)

            btnDeactivate = New Button() With {.Text = "Deactivate", .Size = New Size(110, 32), .Location = New Point(280, 2)}
            UITheme.StyleDangerButton(btnDeactivate)
            AddHandler btnDeactivate.Click, AddressOf BtnDeactivate_Click
            pnlActions.Controls.Add(btnDeactivate)

            lblRecordCount = New Label() With {.Text = "Total Items: 0", .Location = New Point(Width - 180, 8), .Anchor = AnchorStyles.Top Or AnchorStyles.Right, .ForeColor = UITheme.TextSecondary, .Font = UITheme.FontBodyBold}
            pnlActions.Controls.Add(lblRecordCount)

            pnlContent.Controls.Add(pnlActions)

            dgvProducts = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyModernDataGridStyle(dgvProducts)
            dgvProducts.Columns.Add("ProductId", "ID")
            dgvProducts.Columns.Add("Sku", "SKU")
            dgvProducts.Columns.Add("Name", "Product Name")
            dgvProducts.Columns.Add("Category", "Category")
            dgvProducts.Columns.Add("Supplier", "Supplier")
            dgvProducts.Columns.Add("CostPrice", "Cost")
            dgvProducts.Columns.Add("SellingPrice", "Price")
            dgvProducts.Columns.Add("TotalStock", "Total Stock")
            dgvProducts.Columns.Add("MinStock", "Min Level")
            dgvProducts.Columns.Add("Status", "Status")

            dgvProducts.Columns(0).Visible = False
            dgvProducts.Columns(1).Width = 120
            dgvProducts.Columns(2).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            dgvProducts.Columns(3).Width = 140
            dgvProducts.Columns(4).Width = 140
            dgvProducts.Columns(5).Width = 90
            dgvProducts.Columns(6).Width = 90
            dgvProducts.Columns(7).Width = 90
            dgvProducts.Columns(8).Width = 85
            dgvProducts.Columns(9).Width = 85

            AddHandler dgvProducts.CellDoubleClick, AddressOf DgvProducts_CellDoubleClick

            pnlContent.Controls.Add(dgvProducts)
            dgvProducts.BringToFront()

            Controls.Add(pnlContent)
            pnlContent.BringToFront()
        End Sub

        Public Async Sub LoadDataAsync()
            Try
                Dim cats = Await _categoryService.GetAllCategoriesAsync()
                cboFilterCategory.Items.Clear()
                cboFilterCategory.Items.Add(New With {.Key = 0, .Value = "All Categories"})
                For Each cat In cats
                    cboFilterCategory.Items.Add(New With {.Key = cat.CategoryId, .Value = cat.Name})
                Next
                cboFilterCategory.DisplayMember = "Value"
                cboFilterCategory.ValueMember = "Key"
                cboFilterCategory.SelectedIndex = 0

                Await RefreshProductsGridAsync()
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Products load error: {ex.Message}")
            End Try
        End Sub

        Private Async Function RefreshProductsGridAsync() As Task
            Dim catItem = CType(cboFilterCategory.SelectedItem, Object)
            Dim catId As Integer = If(catItem IsNot Nothing, CInt(catItem.GetType().GetProperty("Key").GetValue(catItem, Nothing)), 0)

            Dim products = Await _productService.SearchProductsAsync(txtSearch.Text.Trim(), If(catId > 0, CType(catId, Nullable(Of Integer)), Nothing), Nothing, chkOnlyActive.Checked)

            dgvProducts.Rows.Clear()
            For Each p In products
                Dim rowIdx = dgvProducts.Rows.Add(
                    p.ProductId,
                    p.Sku,
                    p.Name,
                    p.CategoryName,
                    p.SupplierName,
                    p.CostPrice.ToString("C2"),
                    p.SellingPrice.ToString("C2"),
                    p.TotalStock,
                    p.MinStockLevel,
                    If(p.IsActive, "Active", "Archived")
                )

                If p.TotalStock <= p.MinStockLevel Then
                    dgvProducts.Rows(rowIdx).Cells("TotalStock").Style.ForeColor = UITheme.Danger
                    dgvProducts.Rows(rowIdx).Cells("TotalStock").Style.Font = UITheme.FontBodyBold
                End If
            Next
            lblRecordCount.Text = $"Total Items: {products.Count}"
        End Function

        Private Async Sub FilterChanged(sender As Object, e As EventArgs)
            Await RefreshProductsGridAsync()
        End Sub

        Private Sub BtnNewProduct_Click(sender As Object, e As EventArgs)
            Using dlg As New ProductEditDialog()
                If dlg.ShowDialog() = DialogResult.OK Then
                    FilterChanged(Nothing, EventArgs.Empty)
                End If
            End Using
        End Sub

        Private Async Sub BtnEditProduct_Click(sender As Object, e As EventArgs)
            If dgvProducts.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select a product to edit.", "Selection", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim prodId = Convert.ToInt32(dgvProducts.SelectedRows(0).Cells("ProductId").Value)
            Dim prod = Await _productService.GetProductByIdAsync(prodId)
            If prod IsNot Nothing Then
                Using dlg As New ProductEditDialog(prod)
                    If dlg.ShowDialog() = DialogResult.OK Then
                        FilterChanged(Nothing, EventArgs.Empty)
                    End If
                End Using
            End If
        End Sub

        Private Sub DgvProducts_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 Then
                BtnEditProduct_Click(sender, EventArgs.Empty)
            End If
        End Sub

        Private Sub BtnAdjustStock_Click(sender As Object, e As EventArgs)
            Dim prodId As Integer = 0
            If dgvProducts.SelectedRows.Count > 0 Then
                prodId = Convert.ToInt32(dgvProducts.SelectedRows(0).Cells("ProductId").Value)
            End If

            Using dlg As New StockAdjustmentDialog(preselectedProductId:=prodId)
                If dlg.ShowDialog() = DialogResult.OK Then
                    FilterChanged(Nothing, EventArgs.Empty)
                End If
            End Using
        End Sub

        Private Async Sub BtnDeactivate_Click(sender As Object, e As EventArgs)
            If dgvProducts.SelectedRows.Count = 0 Then
                MessageBox.Show("Please select a product to deactivate.", "Selection", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim prodId = Convert.ToInt32(dgvProducts.SelectedRows(0).Cells("ProductId").Value)
            Dim sku = dgvProducts.SelectedRows(0).Cells("Sku").Value.ToString()

            If MessageBox.Show($"Are you sure you want to deactivate SKU '{sku}'?", "Confirm Deactivation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                Dim res = Await _productService.DeactivateProductAsync(prodId)
                If res.Success Then
                    FilterChanged(Nothing, EventArgs.Empty)
                Else
                    MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            End If
        End Sub
    End Class

    Public Class CategoriesView
        Inherits UserControl

        Private ReadOnly _categoryService As New CategoryService()
        Private dgvCategories As DataGridView
        Private btnNewCat As Button
        Private btnEditCat As Button
        Private btnDeactivateCat As Button

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Product Categories", .Font = UITheme.FontHero, .ForeColor = UITheme.TextPrimary, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            btnNewCat = New Button() With {.Text = "+ New Category", .Size = New Size(140, 36), .Location = New Point(Width - 164, 2), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UITheme.StylePrimaryButton(btnNewCat)
            AddHandler btnNewCat.Click, AddressOf BtnNewCat_Click
            pnlHeader.Controls.Add(btnNewCat)
            Controls.Add(pnlHeader)

            Dim pnlContent As New CardPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(16)}

            Dim pnlActions As New Panel() With {.Dock = DockStyle.Top, .Height = 40}
            btnEditCat = New Button() With {.Text = "Edit Selected", .Size = New Size(120, 32), .Location = New Point(0, 2)}
            UITheme.StyleSecondaryButton(btnEditCat)
            AddHandler btnEditCat.Click, AddressOf BtnEditCat_Click
            pnlActions.Controls.Add(btnEditCat)

            btnDeactivateCat = New Button() With {.Text = "Deactivate", .Size = New Size(110, 32), .Location = New Point(130, 2)}
            UITheme.StyleDangerButton(btnDeactivateCat)
            AddHandler btnDeactivateCat.Click, AddressOf BtnDeactivateCat_Click
            pnlActions.Controls.Add(btnDeactivateCat)

            pnlContent.Controls.Add(pnlActions)

            dgvCategories = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyModernDataGridStyle(dgvCategories)
            dgvCategories.Columns.Add("CategoryId", "ID")
            dgvCategories.Columns.Add("Name", "Category Name")
            dgvCategories.Columns.Add("Code", "Category Code")
            dgvCategories.Columns.Add("Description", "Description")
            dgvCategories.Columns.Add("ProductsCount", "Active Products")
            dgvCategories.Columns.Add("Status", "Status")

            dgvCategories.Columns(0).Visible = False
            dgvCategories.Columns(1).Width = 200
            dgvCategories.Columns(2).Width = 140
            dgvCategories.Columns(3).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            dgvCategories.Columns(4).Width = 130
            dgvCategories.Columns(5).Width = 90

            AddHandler dgvCategories.CellDoubleClick, AddressOf DgvCategories_CellDoubleClick

            pnlContent.Controls.Add(dgvCategories)
            dgvCategories.BringToFront()

            Controls.Add(pnlContent)
            pnlContent.BringToFront()
        End Sub

        Public Async Sub LoadDataAsync()
            Try
                Dim categories = Await _categoryService.GetAllCategoriesAsync()
                dgvCategories.Rows.Clear()
                For Each c In categories
                    dgvCategories.Rows.Add(c.CategoryId, c.Name, c.Code, c.Description, c.ProductCount, If(c.IsActive, "Active", "Archived"))
                Next
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Categories load error: {ex.Message}")
            End Try
        End Sub

        Private Sub BtnNewCat_Click(sender As Object, e As EventArgs)
            Using dlg As New CategoryEditDialog()
                If dlg.ShowDialog() = DialogResult.OK Then
                    LoadDataAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnEditCat_Click(sender As Object, e As EventArgs)
            If dgvCategories.SelectedRows.Count = 0 Then Return
            Dim catId = Convert.ToInt32(dgvCategories.SelectedRows(0).Cells("CategoryId").Value)
            Dim cats = Await _categoryService.GetAllCategoriesAsync()
            Dim cat = cats.Find(Function(c) c.CategoryId = catId)
            If cat IsNot Nothing Then
                Using dlg As New CategoryEditDialog(cat)
                    If dlg.ShowDialog() = DialogResult.OK Then
                        LoadDataAsync()
                    End If
                End Using
            End If
        End Sub

        Private Sub DgvCategories_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 Then BtnEditCat_Click(sender, EventArgs.Empty)
        End Sub

        Private Async Sub BtnDeactivateCat_Click(sender As Object, e As EventArgs)
            If dgvCategories.SelectedRows.Count = 0 Then Return
            Dim catId = Convert.ToInt32(dgvCategories.SelectedRows(0).Cells("CategoryId").Value)
            Dim name = dgvCategories.SelectedRows(0).Cells("Name").Value.ToString()

            If MessageBox.Show($"Deactivate category '{name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                Dim res = Await _categoryService.DeactivateCategoryAsync(catId)
                If res.Success Then
                    LoadDataAsync()
                Else
                    MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            End If
        End Sub
    End Class

    Public Class WarehousesView
        Inherits UserControl

        Private ReadOnly _warehouseService As New WarehouseService()
        Private dgvWarehouses As DataGridView
        Private btnNewWh As Button
        Private btnEditWh As Button
        Private btnDeactivateWh As Button

        Public Sub New()
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Dock = DockStyle.Fill
            BackColor = UITheme.AppBackground
            Padding = New Padding(24)

            Dim pnlHeader As New Panel() With {.Dock = DockStyle.Top, .Height = 45}
            Dim lblTitle As New Label() With {.Text = "Warehouses & Storage Facilities", .Font = UITheme.FontHero, .ForeColor = UITheme.TextPrimary, .AutoSize = True}
            pnlHeader.Controls.Add(lblTitle)

            btnNewWh = New Button() With {.Text = "+ Add Facility", .Size = New Size(140, 36), .Location = New Point(Width - 164, 2), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
            UITheme.StylePrimaryButton(btnNewWh)
            AddHandler btnNewWh.Click, AddressOf BtnNewWh_Click
            pnlHeader.Controls.Add(btnNewWh)
            Controls.Add(pnlHeader)

            Dim pnlContent As New CardPanel() With {.Dock = DockStyle.Fill, .Padding = New Padding(16)}

            Dim pnlActions As New Panel() With {.Dock = DockStyle.Top, .Height = 40}
            btnEditWh = New Button() With {.Text = "Edit Selected", .Size = New Size(120, 32), .Location = New Point(0, 2)}
            UITheme.StyleSecondaryButton(btnEditWh)
            AddHandler btnEditWh.Click, AddressOf BtnEditWh_Click
            pnlActions.Controls.Add(btnEditWh)

            btnDeactivateWh = New Button() With {.Text = "Deactivate", .Size = New Size(110, 32), .Location = New Point(130, 2)}
            UITheme.StyleDangerButton(btnDeactivateWh)
            AddHandler btnDeactivateWh.Click, AddressOf BtnDeactivateWh_Click
            pnlActions.Controls.Add(btnDeactivateWh)

            pnlContent.Controls.Add(pnlActions)

            dgvWarehouses = New DataGridView() With {.Dock = DockStyle.Fill}
            UITheme.ApplyModernDataGridStyle(dgvWarehouses)
            dgvWarehouses.Columns.Add("WarehouseId", "ID")
            dgvWarehouses.Columns.Add("Name", "Warehouse Name")
            dgvWarehouses.Columns.Add("Code", "Code")
            dgvWarehouses.Columns.Add("Location", "Location Address")
            dgvWarehouses.Columns.Add("Manager", "Manager")
            dgvWarehouses.Columns.Add("Phone", "Phone")
            dgvWarehouses.Columns.Add("StockCount", "Total Units")
            dgvWarehouses.Columns.Add("TotalValue", "Stock Valuation")
            dgvWarehouses.Columns.Add("Status", "Status")

            dgvWarehouses.Columns(0).Visible = False
            dgvWarehouses.Columns(1).Width = 180
            dgvWarehouses.Columns(2).Width = 100
            dgvWarehouses.Columns(3).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            dgvWarehouses.Columns(4).Width = 130
            dgvWarehouses.Columns(5).Width = 120
            dgvWarehouses.Columns(6).Width = 100
            dgvWarehouses.Columns(7).Width = 130
            dgvWarehouses.Columns(8).Width = 85

            AddHandler dgvWarehouses.CellDoubleClick, AddressOf DgvWarehouses_CellDoubleClick

            pnlContent.Controls.Add(dgvWarehouses)
            dgvWarehouses.BringToFront()

            Controls.Add(pnlContent)
            pnlContent.BringToFront()
        End Sub

        Public Async Sub LoadDataAsync()
            Try
                Dim warehouses = Await _warehouseService.GetAllWarehousesAsync()
                dgvWarehouses.Rows.Clear()
                For Each w In warehouses
                    dgvWarehouses.Rows.Add(
                        w.WarehouseId,
                        w.Name,
                        w.Code,
                        w.Location,
                        w.ManagerName,
                        w.ContactPhone,
                        w.TotalStockCount.ToString("N0"),
                        w.TotalInventoryValue.ToString("C2"),
                        If(w.IsActive, "Active", "Archived")
                    )
                Next
            Catch ex As Exception
                System.Diagnostics.Debug.WriteLine($"Warehouses load error: {ex.Message}")
            End Try
        End Sub

        Private Sub BtnNewWh_Click(sender As Object, e As EventArgs)
            Using dlg As New WarehouseEditDialog()
                If dlg.ShowDialog() = DialogResult.OK Then
                    LoadDataAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnEditWh_Click(sender As Object, e As EventArgs)
            If dgvWarehouses.SelectedRows.Count = 0 Then Return
            Dim whId = Convert.ToInt32(dgvWarehouses.SelectedRows(0).Cells("WarehouseId").Value)
            Dim warehouses = Await _warehouseService.GetAllWarehousesAsync()
            Dim wh = warehouses.Find(Function(w) w.WarehouseId = whId)
            If wh IsNot Nothing Then
                Using dlg As New WarehouseEditDialog(wh)
                    If dlg.ShowDialog() = DialogResult.OK Then
                        LoadDataAsync()
                    End If
                End Using
            End If
        End Sub

        Private Sub DgvWarehouses_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 Then BtnEditWh_Click(sender, EventArgs.Empty)
        End Sub

        Private Async Sub BtnDeactivateWh_Click(sender As Object, e As EventArgs)
            If dgvWarehouses.SelectedRows.Count = 0 Then Return
            Dim whId = Convert.ToInt32(dgvWarehouses.SelectedRows(0).Cells("WarehouseId").Value)
            Dim name = dgvWarehouses.SelectedRows(0).Cells("Name").Value.ToString()

            If MessageBox.Show($"Deactivate warehouse '{name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                Dim res = Await _warehouseService.DeactivateWarehouseAsync(whId)
                If res.Success Then
                    LoadDataAsync()
                Else
                    MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            End If
        End Sub
    End Class

End Namespace
