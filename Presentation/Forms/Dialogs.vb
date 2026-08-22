Option Strict On
Option Explicit On

Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports MASA.InventoryManagementSystem.Application.Services
Imports MASA.InventoryManagementSystem.Domain.Entities
Imports MASA.InventoryManagementSystem.Domain.Enums
Imports MASA.InventoryManagementSystem.Infrastructure.Configuration
Imports MASA.InventoryManagementSystem.Infrastructure.Database
Imports MASA.InventoryManagementSystem.Presentation.Common

Namespace Presentation.Forms

    Public Class ProductEditDialog
        Inherits Form

        Private ReadOnly _productService As New ProductService()
        Private ReadOnly _categoryService As New CategoryService()
        Private ReadOnly _supplierService As New SupplierService()
        Private _product As Product

        Private txtSku As TextBox
        Private txtBarcode As TextBox
        Private txtName As TextBox
        Private txtDescription As TextBox
        Private cboCategory As ComboBox
        Private cboSupplier As ComboBox
        Private numCostPrice As NumericUpDown
        Private numSellingPrice As NumericUpDown
        Private numMinStock As NumericUpDown
        Private txtUom As TextBox
        Private chkIsActive As CheckBox
        Private btnSave As Button
        Private btnCancel As Button

        Public Property SavedProduct As Product
            Get
                Return _product
            End Get
            Private Set(value As Product)
                _product = value
            End Set
        End Property

        Public Sub New(Optional existingProduct As Product = Nothing)
            _product = If(existingProduct, New Product())
            InitializeComponent()
            LoadDropdownsAndData()
        End Sub

        Private Sub InitializeComponent()
            Text = If(_product.ProductId > 0, "Edit Product - " & _product.Name, "Create New Product")
            Size = New Size(540, 620)
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.White
            Font = UITheme.FontBody

            Dim lblTitle As New Label() With {
                .Text = If(_product.ProductId > 0, "Edit Product Details", "New Product Information"),
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.TextPrimary,
                .Location = New Point(24, 18),
                .AutoSize = True
            }
            Controls.Add(lblTitle)

            Dim top = 60

            AddLabel("SKU / Item Code *", 24, top)
            txtSku = AddTextBox(24, top + 20, 230)
            txtSku.Text = _product.Sku

            AddLabel("Barcode", 270, top)
            txtBarcode = AddTextBox(270, top + 20, 230)
            txtBarcode.Text = _product.Barcode

            top += 60
            AddLabel("Product Name *", 24, top)
            txtName = AddTextBox(24, top + 20, 476)
            txtName.Text = _product.Name

            top += 60
            AddLabel("Category", 24, top)
            cboCategory = New ComboBox() With {
                .Location = New Point(24, top + 20),
                .Width = 230,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = UITheme.FontBody
            }
            Controls.Add(cboCategory)

            AddLabel("Preferred Supplier", 270, top)
            cboSupplier = New ComboBox() With {
                .Location = New Point(270, top + 20),
                .Width = 230,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = UITheme.FontBody
            }
            Controls.Add(cboSupplier)

            top += 60
            AddLabel("Cost Price ($) *", 24, top)
            numCostPrice = New NumericUpDown() With {
                .Location = New Point(24, top + 20),
                .Width = 145,
                .DecimalPlaces = 2,
                .Maximum = 1000000D,
                .Value = _product.CostPrice,
                .Font = UITheme.FontBody
            }
            Controls.Add(numCostPrice)

            AddLabel("Selling Price ($) *", 185, top)
            numSellingPrice = New NumericUpDown() With {
                .Location = New Point(185, top + 20),
                .Width = 145,
                .DecimalPlaces = 2,
                .Maximum = 1000000D,
                .Value = _product.SellingPrice,
                .Font = UITheme.FontBody
            }
            Controls.Add(numSellingPrice)

            AddLabel("Min Stock Alert", 350, top)
            numMinStock = New NumericUpDown() With {
                .Location = New Point(350, top + 20),
                .Width = 150,
                .Maximum = 100000D,
                .Value = _product.MinStockLevel,
                .Font = UITheme.FontBody
            }
            Controls.Add(numMinStock)

            top += 60
            AddLabel("Unit of Measure", 24, top)
            txtUom = AddTextBox(24, top + 20, 230)
            txtUom.Text = If(String.IsNullOrWhiteSpace(_product.UnitOfMeasure), "Units", _product.UnitOfMeasure)

            chkIsActive = New CheckBox() With {
                .Text = "Product is Active for Operations",
                .Location = New Point(270, top + 22),
                .AutoSize = True,
                .Checked = _product.IsActive,
                .Font = UITheme.FontBodyBold
            }
            Controls.Add(chkIsActive)

            top += 60
            AddLabel("Description / Specifications", 24, top)
            txtDescription = New TextBox() With {
                .Location = New Point(24, top + 20),
                .Width = 476,
                .Height = 70,
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical,
                .Text = _product.Description,
                .Font = UITheme.FontBody
            }
            Controls.Add(txtDescription)

            btnSave = New Button() With {
                .Text = "Save Product",
                .Location = New Point(270, 520),
                .Width = 130,
                .Height = 38
            }
            UITheme.StylePrimaryButton(btnSave)
            AddHandler btnSave.Click, AddressOf BtnSave_Click
            Controls.Add(btnSave)

            btnCancel = New Button() With {
                .Text = "Cancel",
                .Location = New Point(410, 520),
                .Width = 90,
                .Height = 38,
                .DialogResult = DialogResult.Cancel
            }
            UITheme.StyleSecondaryButton(btnCancel)
            Controls.Add(btnCancel)

            AcceptButton = btnSave
            CancelButton = btnCancel
        End Sub

        Private Sub AddLabel(text As String, x As Integer, y As Integer)
            Dim lbl As New Label() With {
                .Text = text,
                .Location = New Point(x, y),
                .AutoSize = True,
                .ForeColor = UITheme.TextSecondary,
                .Font = UITheme.FontBodyBold
            }
            Controls.Add(lbl)
        End Sub

        Private Function AddTextBox(x As Integer, y As Integer, width As Integer) As TextBox
            Dim tb As New TextBox() With {
                .Location = New Point(x, y),
                .Width = width,
                .Font = UITheme.FontBody
            }
            Controls.Add(tb)
            Return tb
        End Function

        Private Async Sub LoadDropdownsAndData()
            Try
                Dim cats = Await _categoryService.GetAllCategoriesAsync(onlyActive:=True)
                cboCategory.Items.Clear()
                cboCategory.Items.Add(New With {.Key = 0, .Value = "-- Select Category --"})
                For Each cat In cats
                    cboCategory.Items.Add(New With {.Key = cat.CategoryId, .Value = cat.Name})
                Next
                cboCategory.DisplayMember = "Value"
                cboCategory.ValueMember = "Key"
                cboCategory.SelectedIndex = 0

                If _product.CategoryId.HasValue Then
                    For i As Integer = 0 To cboCategory.Items.Count - 1
                        Dim item = CType(cboCategory.Items(i), Object)
                        Dim keyVal As Integer = CInt(item.GetType().GetProperty("Key").GetValue(item, Nothing))
                        If keyVal = _product.CategoryId.Value Then
                            cboCategory.SelectedIndex = i
                            Exit For
                        End If
                    Next
                End If

                Dim supps = Await _supplierService.GetAllSuppliersAsync(onlyActive:=True)
                cboSupplier.Items.Clear()
                cboSupplier.Items.Add(New With {.Key = 0, .Value = "-- Select Supplier --"})
                For Each sup In supps
                    cboSupplier.Items.Add(New With {.Key = sup.SupplierId, .Value = sup.Name})
                Next
                cboSupplier.DisplayMember = "Value"
                cboSupplier.ValueMember = "Key"
                cboSupplier.SelectedIndex = 0

                If _product.SupplierId.HasValue Then
                    For i As Integer = 0 To cboSupplier.Items.Count - 1
                        Dim item = CType(cboSupplier.Items(i), Object)
                        Dim keyVal As Integer = CInt(item.GetType().GetProperty("Key").GetValue(item, Nothing))
                        If keyVal = _product.SupplierId.Value Then
                            cboSupplier.SelectedIndex = i
                            Exit For
                        End If
                    Next
                End If
            Catch ex As Exception
                MessageBox.Show($"Failed to load categories/suppliers: {ex.Message}", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Async Sub BtnSave_Click(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(txtSku.Text) Then
                MessageBox.Show("Please specify a SKU.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtSku.Focus()
                Return
            End If
            If String.IsNullOrWhiteSpace(txtName.Text) Then
                MessageBox.Show("Please enter a product name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtName.Focus()
                Return
            End If

            _product.Sku = txtSku.Text.Trim()
            _product.Barcode = txtBarcode.Text.Trim()
            _product.Name = txtName.Text.Trim()
            _product.Description = txtDescription.Text.Trim()
            _product.CostPrice = numCostPrice.Value
            _product.SellingPrice = numSellingPrice.Value
            _product.MinStockLevel = Convert.ToInt32(numMinStock.Value)
            _product.UnitOfMeasure = txtUom.Text.Trim()
            _product.IsActive = chkIsActive.Checked

            Dim catItem = CType(cboCategory.SelectedItem, Object)
            Dim catKey As Integer = If(catItem IsNot Nothing, CInt(catItem.GetType().GetProperty("Key").GetValue(catItem, Nothing)), 0)
            _product.CategoryId = If(catKey > 0, CType(catKey, Nullable(Of Integer)), Nothing)

            Dim supItem = CType(cboSupplier.SelectedItem, Object)
            Dim supKey As Integer = If(supItem IsNot Nothing, CInt(supItem.GetType().GetProperty("Key").GetValue(supItem, Nothing)), 0)
            _product.SupplierId = If(supKey > 0, CType(supKey, Nullable(Of Integer)), Nothing)

            btnSave.Enabled = False
            Try
                If _product.ProductId = 0 Then
                    Dim res = Await _productService.CreateProductAsync(_product)
                    If res.Success Then
                        _product.ProductId = res.ProductId
                        DialogResult = DialogResult.OK
                        Close()
                    Else
                        MessageBox.Show(res.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If
                Else
                    Dim res = Await _productService.UpdateProductAsync(_product)
                    If res.Success Then
                        DialogResult = DialogResult.OK
                        Close()
                    Else
                        MessageBox.Show(res.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If
                End If
            Finally
                btnSave.Enabled = True
            End Try
        End Sub
    End Class

    Public Class CategoryEditDialog
        Inherits Form

        Private ReadOnly _categoryService As New CategoryService()
        Private _category As Category

        Private txtName As TextBox
        Private txtCode As TextBox
        Private txtDescription As TextBox
        Private chkIsActive As CheckBox
        Private btnSave As Button
        Private btnCancel As Button

        Public Sub New(Optional category As Category = Nothing)
            _category = If(category, New Category())
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Text = If(_category.CategoryId > 0, "Edit Category", "New Category")
            Size = New Size(420, 360)
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.White
            Font = UITheme.FontBody

            Dim lblTitle As New Label() With {
                .Text = If(_category.CategoryId > 0, "Edit Category", "New Product Category"),
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.TextPrimary,
                .Location = New Point(24, 18),
                .AutoSize = True
            }
            Controls.Add(lblTitle)

            Dim lblName As New Label() With {.Text = "Category Name *", .Location = New Point(24, 60), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblName)
            txtName = New TextBox() With {.Location = New Point(24, 80), .Width = 356, .Text = _category.Name, .Font = UITheme.FontBody}
            Controls.Add(txtName)

            Dim lblCode As New Label() With {.Text = "Category Code (e.g. CAT-RAW)", .Location = New Point(24, 115), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblCode)
            txtCode = New TextBox() With {.Location = New Point(24, 135), .Width = 356, .Text = _category.Code, .Font = UITheme.FontBody}
            Controls.Add(txtCode)

            Dim lblDesc As New Label() With {.Text = "Description", .Location = New Point(24, 170), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblDesc)
            txtDescription = New TextBox() With {.Location = New Point(24, 190), .Width = 356, .Height = 50, .Multiline = True, .Text = _category.Description, .Font = UITheme.FontBody}
            Controls.Add(txtDescription)

            chkIsActive = New CheckBox() With {.Text = "Active Category", .Location = New Point(24, 250), .Checked = _category.IsActive, .AutoSize = True}
            Controls.Add(chkIsActive)

            btnSave = New Button() With {.Text = "Save", .Location = New Point(190, 275), .Width = 100, .Height = 35}
            UITheme.StylePrimaryButton(btnSave)
            AddHandler btnSave.Click, AddressOf BtnSave_Click
            Controls.Add(btnSave)

            btnCancel = New Button() With {.Text = "Cancel", .Location = New Point(300, 275), .Width = 80, .Height = 35, .DialogResult = DialogResult.Cancel}
            UITheme.StyleSecondaryButton(btnCancel)
            Controls.Add(btnCancel)

            AcceptButton = btnSave
            CancelButton = btnCancel
        End Sub

        Private Async Sub BtnSave_Click(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(txtName.Text) Then
                MessageBox.Show("Category Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtName.Focus()
                Return
            End If

            _category.Name = txtName.Text.Trim()
            _category.Code = txtCode.Text.Trim()
            _category.Description = txtDescription.Text.Trim()
            _category.IsActive = chkIsActive.Checked

            btnSave.Enabled = False
            Try
                Dim res = Await _categoryService.SaveCategoryAsync(_category)
                If res.Success Then
                    DialogResult = DialogResult.OK
                    Close()
                Else
                    MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            Finally
                btnSave.Enabled = True
            End Try
        End Sub
    End Class

    Public Class WarehouseEditDialog
        Inherits Form

        Private ReadOnly _warehouseService As New WarehouseService()
        Private _warehouse As Warehouse

        Private txtName As TextBox
        Private txtCode As TextBox
        Private txtLocation As TextBox
        Private txtManager As TextBox
        Private txtPhone As TextBox
        Private chkIsActive As CheckBox
        Private btnSave As Button
        Private btnCancel As Button

        Public Sub New(Optional warehouse As Warehouse = Nothing)
            _warehouse = If(warehouse, New Warehouse())
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Text = If(_warehouse.WarehouseId > 0, "Edit Warehouse", "New Warehouse")
            Size = New Size(460, 430)
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.White
            Font = UITheme.FontBody

            Dim lblTitle As New Label() With {
                .Text = If(_warehouse.WarehouseId > 0, "Edit Warehouse Details", "Register New Warehouse Facility"),
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.TextPrimary,
                .Location = New Point(24, 18),
                .AutoSize = True
            }
            Controls.Add(lblTitle)

            Dim lblName As New Label() With {.Text = "Warehouse Name *", .Location = New Point(24, 60), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblName)
            txtName = New TextBox() With {.Location = New Point(24, 80), .Width = 396, .Text = _warehouse.Name, .Font = UITheme.FontBody}
            Controls.Add(txtName)

            Dim lblCode As New Label() With {.Text = "Warehouse Code * (e.g. WH-EAST)", .Location = New Point(24, 115), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblCode)
            txtCode = New TextBox() With {.Location = New Point(24, 135), .Width = 396, .Text = _warehouse.Code, .Font = UITheme.FontBody}
            Controls.Add(txtCode)

            Dim lblLoc As New Label() With {.Text = "Physical Location / Address", .Location = New Point(24, 170), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblLoc)
            txtLocation = New TextBox() With {.Location = New Point(24, 190), .Width = 396, .Text = _warehouse.Location, .Font = UITheme.FontBody}
            Controls.Add(txtLocation)

            Dim lblMgr As New Label() With {.Text = "Manager / Contact Person", .Location = New Point(24, 225), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblMgr)
            txtManager = New TextBox() With {.Location = New Point(24, 245), .Width = 190, .Text = _warehouse.ManagerName, .Font = UITheme.FontBody}
            Controls.Add(txtManager)

            Dim lblPhone As New Label() With {.Text = "Contact Phone", .Location = New Point(230, 225), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblPhone)
            txtPhone = New TextBox() With {.Location = New Point(230, 245), .Width = 190, .Text = _warehouse.ContactPhone, .Font = UITheme.FontBody}
            Controls.Add(txtPhone)

            chkIsActive = New CheckBox() With {.Text = "Active Facility for Stock Operations", .Location = New Point(24, 290), .Checked = _warehouse.IsActive, .AutoSize = True}
            Controls.Add(chkIsActive)

            btnSave = New Button() With {.Text = "Save Warehouse", .Location = New Point(200, 335), .Width = 130, .Height = 35}
            UITheme.StylePrimaryButton(btnSave)
            AddHandler btnSave.Click, AddressOf BtnSave_Click
            Controls.Add(btnSave)

            btnCancel = New Button() With {.Text = "Cancel", .Location = New Point(340, 335), .Width = 80, .Height = 35, .DialogResult = DialogResult.Cancel}
            UITheme.StyleSecondaryButton(btnCancel)
            Controls.Add(btnCancel)

            AcceptButton = btnSave
            CancelButton = btnCancel
        End Sub

        Private Async Sub BtnSave_Click(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(txtName.Text) Then
                MessageBox.Show("Warehouse Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtName.Focus()
                Return
            End If
            If String.IsNullOrWhiteSpace(txtCode.Text) Then
                MessageBox.Show("Warehouse Code is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtCode.Focus()
                Return
            End If

            _warehouse.Name = txtName.Text.Trim()
            _warehouse.Code = txtCode.Text.Trim().ToUpperInvariant()
            _warehouse.Location = txtLocation.Text.Trim()
            _warehouse.ManagerName = txtManager.Text.Trim()
            _warehouse.ContactPhone = txtPhone.Text.Trim()
            _warehouse.IsActive = chkIsActive.Checked

            btnSave.Enabled = False
            Try
                Dim res = Await _warehouseService.SaveWarehouseAsync(_warehouse)
                If res.Success Then
                    DialogResult = DialogResult.OK
                    Close()
                Else
                    MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            Finally
                btnSave.Enabled = True
            End Try
        End Sub
    End Class

    Public Class SupplierEditDialog
        Inherits Form

        Private ReadOnly _supplierService As New SupplierService()
        Private _supplier As Supplier

        Private txtName As TextBox
        Private txtCompany As TextBox
        Private txtContact As TextBox
        Private txtEmail As TextBox
        Private txtPhone As TextBox
        Private txtAddress As TextBox
        Private txtTax As TextBox
        Private txtNotes As TextBox
        Private chkIsActive As CheckBox
        Private btnSave As Button
        Private btnCancel As Button

        Public Sub New(Optional supplier As Supplier = Nothing)
            _supplier = If(supplier, New Supplier())
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Text = If(_supplier.SupplierId > 0, "Edit Supplier", "New Supplier")
            Size = New Size(480, 520)
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.White
            Font = UITheme.FontBody

            Dim lblTitle As New Label() With {
                .Text = If(_supplier.SupplierId > 0, "Edit Supplier Record", "Register New Supplier"),
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.TextPrimary,
                .Location = New Point(24, 18),
                .AutoSize = True
            }
            Controls.Add(lblTitle)

            Dim top = 60
            AddLabel("Supplier Name *", 24, top)
            txtName = AddTextBox(24, top + 20, 200, _supplier.Name)

            AddLabel("Company / Organization", 240, top)
            txtCompany = AddTextBox(240, top + 20, 200, _supplier.CompanyName)

            top += 55
            AddLabel("Contact Person", 24, top)
            txtContact = AddTextBox(24, top + 20, 200, _supplier.ContactPerson)

            AddLabel("Email Address", 240, top)
            txtEmail = AddTextBox(240, top + 20, 200, _supplier.Email)

            top += 55
            AddLabel("Phone Number", 24, top)
            txtPhone = AddTextBox(24, top + 20, 200, _supplier.Phone)

            AddLabel("Tax / Registration #", 240, top)
            txtTax = AddTextBox(240, top + 20, 200, _supplier.TaxNumber)

            top += 55
            AddLabel("Physical Address", 24, top)
            txtAddress = AddTextBox(24, top + 20, 416, _supplier.Address)

            top += 55
            AddLabel("Notes & Remarks", 24, top)
            txtNotes = New TextBox() With {.Location = New Point(24, top + 20), .Width = 416, .Height = 45, .Multiline = True, .Text = _supplier.Notes, .Font = UITheme.FontBody}
            Controls.Add(txtNotes)

            top += 75
            chkIsActive = New CheckBox() With {.Text = "Active Supplier", .Location = New Point(24, top), .Checked = _supplier.IsActive, .AutoSize = True}
            Controls.Add(chkIsActive)

            btnSave = New Button() With {.Text = "Save Supplier", .Location = New Point(220, top + 35), .Width = 120, .Height = 35}
            UITheme.StylePrimaryButton(btnSave)
            AddHandler btnSave.Click, AddressOf BtnSave_Click
            Controls.Add(btnSave)

            btnCancel = New Button() With {.Text = "Cancel", .Location = New Point(350, top + 35), .Width = 90, .Height = 35, .DialogResult = DialogResult.Cancel}
            UITheme.StyleSecondaryButton(btnCancel)
            Controls.Add(btnCancel)

            AcceptButton = btnSave
            CancelButton = btnCancel
        End Sub

        Private Sub AddLabel(text As String, x As Integer, y As Integer)
            Dim lbl As New Label() With {.Text = text, .Location = New Point(x, y), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lbl)
        End Sub

        Private Function AddTextBox(x As Integer, y As Integer, width As Integer, initialText As String) As TextBox
            Dim tb As New TextBox() With {.Location = New Point(x, y), .Width = width, .Text = initialText, .Font = UITheme.FontBody}
            Controls.Add(tb)
            Return tb
        End Function

        Private Async Sub BtnSave_Click(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(txtName.Text) Then
                MessageBox.Show("Supplier Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtName.Focus()
                Return
            End If

            _supplier.Name = txtName.Text.Trim()
            _supplier.CompanyName = txtCompany.Text.Trim()
            _supplier.ContactPerson = txtContact.Text.Trim()
            _supplier.Email = txtEmail.Text.Trim()
            _supplier.Phone = txtPhone.Text.Trim()
            _supplier.TaxNumber = txtTax.Text.Trim()
            _supplier.Address = txtAddress.Text.Trim()
            _supplier.Notes = txtNotes.Text.Trim()
            _supplier.IsActive = chkIsActive.Checked

            btnSave.Enabled = False
            Try
                Dim res = Await _supplierService.SaveSupplierAsync(_supplier)
                If res.Success Then
                    DialogResult = DialogResult.OK
                    Close()
                Else
                    MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            Finally
                btnSave.Enabled = True
            End Try
        End Sub
    End Class

    Public Class CustomerEditDialog
        Inherits Form

        Private ReadOnly _customerService As New CustomerService()
        Private _customer As Customer

        Private txtFullName As TextBox
        Private txtCompany As TextBox
        Private txtEmail As TextBox
        Private txtPhone As TextBox
        Private txtAddress As TextBox
        Private txtTax As TextBox
        Private txtNotes As TextBox
        Private chkIsActive As CheckBox
        Private btnSave As Button
        Private btnCancel As Button

        Public Sub New(Optional customer As Customer = Nothing)
            _customer = If(customer, New Customer())
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Text = If(_customer.CustomerId > 0, "Edit Customer", "New Customer")
            Size = New Size(480, 520)
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.White
            Font = UITheme.FontBody

            Dim lblTitle As New Label() With {
                .Text = If(_customer.CustomerId > 0, "Edit Customer Record", "Register New Customer"),
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.TextPrimary,
                .Location = New Point(24, 18),
                .AutoSize = True
            }
            Controls.Add(lblTitle)

            Dim top = 60
            AddLabel("Customer Full Name *", 24, top)
            txtFullName = AddTextBox(24, top + 20, 200, _customer.FullName)

            AddLabel("Company / Organization", 240, top)
            txtCompany = AddTextBox(240, top + 20, 200, _customer.CompanyName)

            top += 55
            AddLabel("Email Address", 24, top)
            txtEmail = AddTextBox(24, top + 20, 200, _customer.Email)

            AddLabel("Phone Number", 240, top)
            txtPhone = AddTextBox(240, top + 20, 200, _customer.Phone)

            top += 55
            AddLabel("Tax / Registration #", 24, top)
            txtTax = AddTextBox(24, top + 20, 200, _customer.TaxNumber)

            top += 55
            AddLabel("Physical / Shipping Address", 24, top)
            txtAddress = AddTextBox(24, top + 20, 416, _customer.Address)

            top += 55
            AddLabel("Notes & Remarks", 24, top)
            txtNotes = New TextBox() With {.Location = New Point(24, top + 20), .Width = 416, .Height = 45, .Multiline = True, .Text = _customer.Notes, .Font = UITheme.FontBody}
            Controls.Add(txtNotes)

            top += 75
            chkIsActive = New CheckBox() With {.Text = "Active Customer Account", .Location = New Point(24, top), .Checked = _customer.IsActive, .AutoSize = True}
            Controls.Add(chkIsActive)

            btnSave = New Button() With {.Text = "Save Customer", .Location = New Point(220, top + 35), .Width = 120, .Height = 35}
            UITheme.StylePrimaryButton(btnSave)
            AddHandler btnSave.Click, AddressOf BtnSave_Click
            Controls.Add(btnSave)

            btnCancel = New Button() With {.Text = "Cancel", .Location = New Point(350, top + 35), .Width = 90, .Height = 35, .DialogResult = DialogResult.Cancel}
            UITheme.StyleSecondaryButton(btnCancel)
            Controls.Add(btnCancel)

            AcceptButton = btnSave
            CancelButton = btnCancel
        End Sub

        Private Sub AddLabel(text As String, x As Integer, y As Integer)
            Dim lbl As New Label() With {.Text = text, .Location = New Point(x, y), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lbl)
        End Sub

        Private Function AddTextBox(x As Integer, y As Integer, width As Integer, initialText As String) As TextBox
            Dim tb As New TextBox() With {.Location = New Point(x, y), .Width = width, .Text = initialText, .Font = UITheme.FontBody}
            Controls.Add(tb)
            Return tb
        End Function

        Private Async Sub BtnSave_Click(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(txtFullName.Text) Then
                MessageBox.Show("Customer Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtFullName.Focus()
                Return
            End If

            _customer.FullName = txtFullName.Text.Trim()
            _customer.CompanyName = txtCompany.Text.Trim()
            _customer.Email = txtEmail.Text.Trim()
            _customer.Phone = txtPhone.Text.Trim()
            _customer.TaxNumber = txtTax.Text.Trim()
            _customer.Address = txtAddress.Text.Trim()
            _customer.Notes = txtNotes.Text.Trim()
            _customer.IsActive = chkIsActive.Checked

            btnSave.Enabled = False
            Try
                Dim res = Await _customerService.SaveCustomerAsync(_customer)
                If res.Success Then
                    DialogResult = DialogResult.OK
                    Close()
                Else
                    MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            Finally
                btnSave.Enabled = True
            End Try
        End Sub
    End Class

    Public Class StockAdjustmentDialog
        Inherits Form

        Private ReadOnly _inventoryService As New InventoryService()
        Private ReadOnly _productService As New ProductService()
        Private ReadOnly _warehouseService As New WarehouseService()

        Private cboProduct As ComboBox
        Private cboWarehouse As ComboBox
        Private cboAdjustmentType As ComboBox
        Private numQuantity As NumericUpDown
        Private txtNotes As TextBox
        Private lblCurrentStock As Label
        Private btnSave As Button
        Private btnCancel As Button

        Public Sub New(Optional preselectedProductId As Integer = 0, Optional preselectedWarehouseId As Integer = 0)
            InitializeComponent()
            LoadData(preselectedProductId, preselectedWarehouseId)
        End Sub

        Private Sub InitializeComponent()
            Text = "Manual Inventory Stock Adjustment"
            Size = New Size(480, 440)
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.White
            Font = UITheme.FontBody

            Dim lblTitle As New Label() With {
                .Text = "Record Stock Adjustment",
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.TextPrimary,
                .Location = New Point(24, 18),
                .AutoSize = True
            }
            Controls.Add(lblTitle)

            Dim lblWh As New Label() With {.Text = "Warehouse Facility *", .Location = New Point(24, 60), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblWh)
            cboWarehouse = New ComboBox() With {.Location = New Point(24, 80), .Width = 416, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            AddHandler cboWarehouse.SelectedIndexChanged, AddressOf UpdateCurrentStockLabel
            Controls.Add(cboWarehouse)

            Dim lblProd As New Label() With {.Text = "Product *", .Location = New Point(24, 115), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblProd)
            cboProduct = New ComboBox() With {.Location = New Point(24, 135), .Width = 416, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            AddHandler cboProduct.SelectedIndexChanged, AddressOf UpdateCurrentStockLabel
            Controls.Add(cboProduct)

            lblCurrentStock = New Label() With {.Text = "Current In-Stock: --", .Location = New Point(24, 165), .AutoSize = True, .ForeColor = UITheme.TextSecondary, .Font = UITheme.FontSmall}
            Controls.Add(lblCurrentStock)

            Dim lblType As New Label() With {.Text = "Adjustment Action *", .Location = New Point(24, 195), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblType)
            cboAdjustmentType = New ComboBox() With {.Location = New Point(24, 215), .Width = 200, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            cboAdjustmentType.Items.Add("Adjustment Increase (+)")
            cboAdjustmentType.Items.Add("Adjustment Decrease (-)")
            cboAdjustmentType.Items.Add("Stock In (+)")
            cboAdjustmentType.Items.Add("Stock Out (-)")
            cboAdjustmentType.Items.Add("Return (+)")
            cboAdjustmentType.SelectedIndex = 0
            Controls.Add(cboAdjustmentType)

            Dim lblQty As New Label() With {.Text = "Units Count *", .Location = New Point(240, 195), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblQty)
            numQuantity = New NumericUpDown() With {.Location = New Point(240, 215), .Width = 200, .Minimum = 1, .Maximum = 1000000, .Value = 1, .Font = UITheme.FontBody}
            Controls.Add(numQuantity)

            Dim lblNotes As New Label() With {.Text = "Reason / Audit Notes *", .Location = New Point(24, 255), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblNotes)
            txtNotes = New TextBox() With {.Location = New Point(24, 275), .Width = 416, .Height = 50, .Multiline = True, .Font = UITheme.FontBody}
            Controls.Add(txtNotes)

            btnSave = New Button() With {.Text = "Apply Adjustment", .Location = New Point(210, 345), .Width = 140, .Height = 35}
            UITheme.StylePrimaryButton(btnSave)
            AddHandler btnSave.Click, AddressOf BtnSave_Click
            Controls.Add(btnSave)

            btnCancel = New Button() With {.Text = "Cancel", .Location = New Point(360, 345), .Width = 80, .Height = 35, .DialogResult = DialogResult.Cancel}
            UITheme.StyleSecondaryButton(btnCancel)
            Controls.Add(btnCancel)

            AcceptButton = btnSave
            CancelButton = btnCancel
        End Sub

        Private Async Sub LoadData(preselectedProductId As Integer, preselectedWarehouseId As Integer)
            Try
                Dim warehouses = Await _warehouseService.GetAllWarehousesAsync(onlyActive:=True)
                cboWarehouse.Items.Clear()
                For Each wh In warehouses
                    cboWarehouse.Items.Add(New With {.Key = wh.WarehouseId, .Value = $"{wh.Name} ({wh.Code})"})
                Next
                cboWarehouse.DisplayMember = "Value"
                cboWarehouse.ValueMember = "Key"
                If cboWarehouse.Items.Count > 0 Then
                    cboWarehouse.SelectedIndex = 0
                End If

                Dim products = Await _productService.SearchProductsAsync("", Nothing, Nothing, onlyActive:=True)
                cboProduct.Items.Clear()
                For Each p In products
                    cboProduct.Items.Add(New With {.Key = p.ProductId, .Value = $"{p.Sku} - {p.Name}"})
                Next
                cboProduct.DisplayMember = "Value"
                cboProduct.ValueMember = "Key"
                If cboProduct.Items.Count > 0 Then
                    cboProduct.SelectedIndex = 0
                End If

                If preselectedWarehouseId > 0 Then
                    For i As Integer = 0 To cboWarehouse.Items.Count - 1
                        Dim item = CType(cboWarehouse.Items(i), Object)
                        If CInt(item.GetType().GetProperty("Key").GetValue(item, Nothing)) = preselectedWarehouseId Then
                            cboWarehouse.SelectedIndex = i
                            Exit For
                        End If
                    Next
                End If

                If preselectedProductId > 0 Then
                    For i As Integer = 0 To cboProduct.Items.Count - 1
                        Dim item = CType(cboProduct.Items(i), Object)
                        If CInt(item.GetType().GetProperty("Key").GetValue(item, Nothing)) = preselectedProductId Then
                            cboProduct.SelectedIndex = i
                            Exit For
                        End If
                    Next
                End If

                UpdateCurrentStockLabel(Nothing, EventArgs.Empty)
            Catch ex As Exception
                MessageBox.Show($"Error loading data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Async Sub UpdateCurrentStockLabel(sender As Object, e As EventArgs)
            If cboWarehouse.SelectedItem Is Nothing OrElse cboProduct.SelectedItem Is Nothing Then Return

            Try
                Dim whItem = CType(cboWarehouse.SelectedItem, Object)
                Dim prodItem = CType(cboProduct.SelectedItem, Object)
                Dim whId = CInt(whItem.GetType().GetProperty("Key").GetValue(whItem, Nothing))
                Dim prodId = CInt(prodItem.GetType().GetProperty("Key").GetValue(prodItem, Nothing))

                Dim stocks = Await _inventoryService.GetStockByWarehouseAsync(whId)
                Dim matched = stocks.Find(Function(s) s.ProductId = prodId)
                Dim qty = If(matched IsNot Nothing, matched.Quantity, 0)
                lblCurrentStock.Text = $"Current In-Stock in this warehouse: {qty} units"
            Catch
                lblCurrentStock.Text = "Current In-Stock: --"
            End Try
        End Sub

        Private Async Sub BtnSave_Click(sender As Object, e As EventArgs)
            If cboWarehouse.SelectedItem Is Nothing OrElse cboProduct.SelectedItem Is Nothing Then
                MessageBox.Show("Please select both product and warehouse.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            If String.IsNullOrWhiteSpace(txtNotes.Text) Then
                MessageBox.Show("Please provide a reason / note for the adjustment.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtNotes.Focus()
                Return
            End If

            Dim whItem = CType(cboWarehouse.SelectedItem, Object)
            Dim prodItem = CType(cboProduct.SelectedItem, Object)
            Dim whId = CInt(whItem.GetType().GetProperty("Key").GetValue(whItem, Nothing))
            Dim prodId = CInt(prodItem.GetType().GetProperty("Key").GetValue(prodItem, Nothing))

            Dim qtyVal = Convert.ToInt32(numQuantity.Value)
            Dim txType As TransactionType
            Dim delta As Integer

            Select Case cboAdjustmentType.SelectedIndex
                Case 0
                    txType = TransactionType.AdjustmentIncrease
                    delta = qtyVal
                Case 1
                    txType = TransactionType.AdjustmentDecrease
                    delta = -qtyVal
                Case 2
                    txType = TransactionType.StockIn
                    delta = qtyVal
                Case 3
                    txType = TransactionType.StockOut
                    delta = -qtyVal
                Case 4
                    txType = TransactionType.Return
                    delta = qtyVal
                Case Else
                    txType = TransactionType.AdjustmentIncrease
                    delta = qtyVal
            End Select

            btnSave.Enabled = False
            Try
                Dim res = Await _inventoryService.AdjustStockAsync(prodId, whId, txType, delta, txtNotes.Text.Trim())
                If res.Success Then
                    DialogResult = DialogResult.OK
                    Close()
                Else
                    MessageBox.Show(res.Message, "Adjustment Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            Finally
                btnSave.Enabled = True
            End Try
        End Sub
    End Class

    Public Class NewTransferDialog
        Inherits Form

        Private ReadOnly _inventoryService As New InventoryService()
        Private ReadOnly _productService As New ProductService()
        Private ReadOnly _warehouseService As New WarehouseService()

        Private cboSourceWh As ComboBox
        Private cboDestWh As ComboBox
        Private cboProduct As ComboBox
        Private numQuantity As NumericUpDown
        Private txtNotes As TextBox
        Private btnAddItem As Button
        Private btnRemoveItem As Button
        Private dgvItems As DataGridView
        Private btnSubmit As Button
        Private btnCancel As Button

        Private ReadOnly _itemsList As New List(Of (ProductId As Integer, ProductName As String, Sku As String, Quantity As Integer))()

        Public Sub New()
            InitializeComponent()
            LoadWarehousesAndProducts()
        End Sub

        Private Sub InitializeComponent()
            Text = "New Inter-Warehouse Stock Transfer"
            Size = New Size(620, 560)
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.White
            Font = UITheme.FontBody

            Dim lblTitle As New Label() With {
                .Text = "Inter-Warehouse Stock Transfer",
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.TextPrimary,
                .Location = New Point(24, 18),
                .AutoSize = True
            }
            Controls.Add(lblTitle)

            Dim lblSrc As New Label() With {.Text = "Source Warehouse (From) *", .Location = New Point(24, 60), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblSrc)
            cboSourceWh = New ComboBox() With {.Location = New Point(24, 80), .Width = 270, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            Controls.Add(cboSourceWh)

            Dim lblDst As New Label() With {.Text = "Destination Warehouse (To) *", .Location = New Point(310, 60), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblDst)
            cboDestWh = New ComboBox() With {.Location = New Point(310, 80), .Width = 270, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            Controls.Add(cboDestWh)

            Dim lblProd As New Label() With {.Text = "Select Product to Add", .Location = New Point(24, 120), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblProd)
            cboProduct = New ComboBox() With {.Location = New Point(24, 140), .Width = 320, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            Controls.Add(cboProduct)

            Dim lblQty As New Label() With {.Text = "Qty", .Location = New Point(355, 120), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblQty)
            numQuantity = New NumericUpDown() With {.Location = New Point(355, 140), .Width = 80, .Minimum = 1, .Maximum = 100000, .Value = 1, .Font = UITheme.FontBody}
            Controls.Add(numQuantity)

            btnAddItem = New Button() With {.Text = "+ Add Item", .Location = New Point(450, 138), .Width = 130, .Height = 28}
            UITheme.StylePrimaryButton(btnAddItem)
            AddHandler btnAddItem.Click, AddressOf BtnAddItem_Click
            Controls.Add(btnAddItem)

            dgvItems = New DataGridView() With {
                .Location = New Point(24, 180),
                .Size = New Size(556, 190)
            }
            UITheme.ApplyModernDataGridStyle(dgvItems)
            dgvItems.Columns.Add("Sku", "SKU")
            dgvItems.Columns.Add("ProductName", "Product Name")
            dgvItems.Columns.Add("Quantity", "Transfer Quantity")
            dgvItems.Columns(0).Width = 120
            dgvItems.Columns(1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            dgvItems.Columns(2).Width = 140
            Controls.Add(dgvItems)

            Dim lblNotes As New Label() With {.Text = "Transfer Reference / Transport Notes", .Location = New Point(24, 380), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblNotes)
            txtNotes = New TextBox() With {.Location = New Point(24, 400), .Width = 556, .Height = 45, .Multiline = True, .Font = UITheme.FontBody}
            Controls.Add(txtNotes)

            btnSubmit = New Button() With {.Text = "Execute Transfer", .Location = New Point(320, 465), .Width = 150, .Height = 36}
            UITheme.StyleSuccessButton(btnSubmit)
            AddHandler btnSubmit.Click, AddressOf BtnSubmit_Click
            Controls.Add(btnSubmit)

            btnCancel = New Button() With {.Text = "Cancel", .Location = New Point(480, 465), .Width = 100, .Height = 36, .DialogResult = DialogResult.Cancel}
            UITheme.StyleSecondaryButton(btnCancel)
            Controls.Add(btnCancel)

            AcceptButton = btnSubmit
            CancelButton = btnCancel
        End Sub

        Private Async Sub LoadWarehousesAndProducts()
            Try
                Dim warehouses = Await _warehouseService.GetAllWarehousesAsync(onlyActive:=True)
                cboSourceWh.Items.Clear()
                cboDestWh.Items.Clear()
                For Each wh In warehouses
                    cboSourceWh.Items.Add(New With {.Key = wh.WarehouseId, .Value = $"{wh.Name} ({wh.Code})"})
                    cboDestWh.Items.Add(New With {.Key = wh.WarehouseId, .Value = $"{wh.Name} ({wh.Code})"})
                Next
                cboSourceWh.DisplayMember = "Value"
                cboSourceWh.ValueMember = "Key"
                cboDestWh.DisplayMember = "Value"
                cboDestWh.ValueMember = "Key"

                If cboSourceWh.Items.Count > 0 Then cboSourceWh.SelectedIndex = 0
                If cboDestWh.Items.Count > 1 Then cboDestWh.SelectedIndex = 1

                Dim products = Await _productService.SearchProductsAsync("", Nothing, Nothing, onlyActive:=True)
                cboProduct.Items.Clear()
                For Each p In products
                    cboProduct.Items.Add(New With {.Key = p.ProductId, .Sku = p.Sku, .Name = p.Name, .Value = $"{p.Sku} - {p.Name}"})
                Next
                cboProduct.DisplayMember = "Value"
                cboProduct.ValueMember = "Key"
                If cboProduct.Items.Count > 0 Then cboProduct.SelectedIndex = 0
            Catch ex As Exception
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Sub BtnAddItem_Click(sender As Object, e As EventArgs)
            If cboProduct.SelectedItem Is Nothing Then Return
            Dim item = CType(cboProduct.SelectedItem, Object)
            Dim prodId = CInt(item.GetType().GetProperty("Key").GetValue(item, Nothing))
            Dim sku = item.GetType().GetProperty("Sku").GetValue(item, Nothing).ToString()
            Dim name = item.GetType().GetProperty("Name").GetValue(item, Nothing).ToString()
            Dim qty = Convert.ToInt32(numQuantity.Value)

            Dim existingIdx = _itemsList.FindIndex(Function(x) x.ProductId = prodId)
            If existingIdx >= 0 Then
                Dim old = _itemsList(existingIdx)
                _itemsList(existingIdx) = (old.ProductId, old.ProductName, old.Sku, old.Quantity + qty)
            Else
                _itemsList.Add((prodId, name, sku, qty))
            End If

            RefreshGrid()
        End Sub

        Private Sub RefreshGrid()
            dgvItems.Rows.Clear()
            For Each itm In _itemsList
                dgvItems.Rows.Add(itm.Sku, itm.ProductName, itm.Quantity)
            Next
        End Sub

        Private Async Sub BtnSubmit_Click(sender As Object, e As EventArgs)
            If cboSourceWh.SelectedItem Is Nothing OrElse cboDestWh.SelectedItem Is Nothing Then
                MessageBox.Show("Please select both source and destination warehouses.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim srcItem = CType(cboSourceWh.SelectedItem, Object)
            Dim dstItem = CType(cboDestWh.SelectedItem, Object)
            Dim srcId = CInt(srcItem.GetType().GetProperty("Key").GetValue(srcItem, Nothing))
            Dim dstId = CInt(dstItem.GetType().GetProperty("Key").GetValue(dstItem, Nothing))

            If srcId = dstId Then
                MessageBox.Show("Source and destination warehouses must be different.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If _itemsList.Count = 0 Then
                MessageBox.Show("Please add at least one product item to transfer.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim itemsToTransfer = New List(Of (ProductId As Integer, Quantity As Integer))()
            For Each itm In _itemsList
                itemsToTransfer.Add((itm.ProductId, itm.Quantity))
            Next

            btnSubmit.Enabled = False
            Try
                Dim res = Await _inventoryService.TransferStockAsync(srcId, dstId, itemsToTransfer, txtNotes.Text.Trim())
                If res.Success Then
                    MessageBox.Show(res.Message, "Transfer Completed", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    DialogResult = DialogResult.OK
                    Close()
                Else
                    MessageBox.Show(res.Message, "Transfer Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            Finally
                btnSubmit.Enabled = True
            End Try
        End Sub
    End Class

    Public Class NewPurchaseOrderDialog
        Inherits Form

        Private ReadOnly _purchaseService As New PurchaseService()
        Private ReadOnly _supplierService As New SupplierService()
        Private ReadOnly _warehouseService As New WarehouseService()
        Private ReadOnly _productService As New ProductService()

        Private cboSupplier As ComboBox
        Private cboWarehouse As ComboBox
        Private dtpOrderDate As DateTimePicker
        Private dtpExpectedDate As DateTimePicker
        Private cboProduct As ComboBox
        Private numQty As NumericUpDown
        Private numCost As NumericUpDown
        Private btnAddItem As Button
        Private dgvItems As DataGridView
        Private txtNotes As TextBox
        Private lblSubtotal As Label
        Private lblTotal As Label
        Private btnSaveDraft As Button
        Private btnCancel As Button

        Private ReadOnly _items As New List(Of PurchaseOrderItem)()

        Public Sub New()
            InitializeComponent()
            LoadData()
        End Sub

        Private Sub InitializeComponent()
            Text = "Create Purchase Order"
            Size = New Size(720, 620)
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.White
            Font = UITheme.FontBody

            Dim lblTitle As New Label() With {
                .Text = "New Supplier Purchase Order",
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.TextPrimary,
                .Location = New Point(24, 18),
                .AutoSize = True
            }
            Controls.Add(lblTitle)

            Dim lblSup As New Label() With {.Text = "Supplier *", .Location = New Point(24, 60), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblSup)
            cboSupplier = New ComboBox() With {.Location = New Point(24, 80), .Width = 320, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            Controls.Add(cboSupplier)

            Dim lblWh As New Label() With {.Text = "Receiving Warehouse *", .Location = New Point(360, 60), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblWh)
            cboWarehouse = New ComboBox() With {.Location = New Point(360, 80), .Width = 320, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            Controls.Add(cboWarehouse)

            Dim lblDate As New Label() With {.Text = "Order Date", .Location = New Point(24, 115), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblDate)
            dtpOrderDate = New DateTimePicker() With {.Location = New Point(24, 135), .Width = 150, .Format = DateTimePickerFormat.Short, .Font = UITheme.FontBody}
            Controls.Add(dtpOrderDate)

            Dim lblExp As New Label() With {.Text = "Expected Delivery", .Location = New Point(190, 115), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblExp)
            dtpExpectedDate = New DateTimePicker() With {.Location = New Point(190, 135), .Width = 154, .Format = DateTimePickerFormat.Short, .Font = UITheme.FontBody}
            Controls.Add(dtpExpectedDate)

            Dim lblAddTitle As New Label() With {.Text = "Line Items", .Location = New Point(24, 175), .AutoSize = True, .Font = UITheme.FontSubtitle, .ForeColor = UITheme.PrimaryDark}
            Controls.Add(lblAddTitle)

            cboProduct = New ComboBox() With {.Location = New Point(24, 200), .Width = 280, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            AddHandler cboProduct.SelectedIndexChanged, AddressOf CboProduct_SelectedIndexChanged
            Controls.Add(cboProduct)

            numQty = New NumericUpDown() With {.Location = New Point(315, 200), .Width = 70, .Minimum = 1, .Maximum = 100000, .Value = 1, .Font = UITheme.FontBody}
            Controls.Add(numQty)

            numCost = New NumericUpDown() With {.Location = New Point(395, 200), .Width = 100, .DecimalPlaces = 2, .Maximum = 1000000D, .Font = UITheme.FontBody}
            Controls.Add(numCost)

            btnAddItem = New Button() With {.Text = "+ Add to Order", .Location = New Point(505, 198), .Width = 120, .Height = 28}
            UITheme.StylePrimaryButton(btnAddItem)
            AddHandler btnAddItem.Click, AddressOf BtnAddItem_Click
            Controls.Add(btnAddItem)

            dgvItems = New DataGridView() With {.Location = New Point(24, 235), .Size = New Size(656, 170)}
            UITheme.ApplyModernDataGridStyle(dgvItems)
            dgvItems.Columns.Add("Sku", "SKU")
            dgvItems.Columns.Add("Name", "Product Name")
            dgvItems.Columns.Add("Qty", "Qty")
            dgvItems.Columns.Add("Cost", "Unit Cost")
            dgvItems.Columns.Add("Total", "Line Total")
            dgvItems.Columns(0).Width = 100
            dgvItems.Columns(1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            dgvItems.Columns(2).Width = 70
            dgvItems.Columns(3).Width = 100
            dgvItems.Columns(4).Width = 110
            Controls.Add(dgvItems)

            lblTotal = New Label() With {
                .Text = "Total Amount: $0.00",
                .Location = New Point(450, 415),
                .Size = New Size(230, 25),
                .TextAlign = ContentAlignment.MiddleRight,
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.PrimaryDark
            }
            Controls.Add(lblTotal)

            Dim lblNotes As New Label() With {.Text = "Purchase Order Notes", .Location = New Point(24, 420), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblNotes)
            txtNotes = New TextBox() With {.Location = New Point(24, 440), .Width = 400, .Height = 50, .Multiline = True, .Font = UITheme.FontBody}
            Controls.Add(txtNotes)

            btnSaveDraft = New Button() With {.Text = "Save Purchase Order", .Location = New Point(430, 520), .Width = 150, .Height = 36}
            UITheme.StylePrimaryButton(btnSaveDraft)
            AddHandler btnSaveDraft.Click, AddressOf BtnSaveDraft_Click
            Controls.Add(btnSaveDraft)

            btnCancel = New Button() With {.Text = "Cancel", .Location = New Point(590, 520), .Width = 90, .Height = 36, .DialogResult = DialogResult.Cancel}
            UITheme.StyleSecondaryButton(btnCancel)
            Controls.Add(btnCancel)

            AcceptButton = btnSaveDraft
            CancelButton = btnCancel
        End Sub

        Private Async Sub LoadData()
            Try
                Dim suppliers = Await _supplierService.GetAllSuppliersAsync(onlyActive:=True)
                cboSupplier.Items.Clear()
                For Each s In suppliers
                    cboSupplier.Items.Add(New With {.Key = s.SupplierId, .Value = s.Name})
                Next
                cboSupplier.DisplayMember = "Value"
                cboSupplier.ValueMember = "Key"
                If cboSupplier.Items.Count > 0 Then cboSupplier.SelectedIndex = 0

                Dim warehouses = Await _warehouseService.GetAllWarehousesAsync(onlyActive:=True)
                cboWarehouse.Items.Clear()
                For Each wh In warehouses
                    cboWarehouse.Items.Add(New With {.Key = wh.WarehouseId, .Value = $"{wh.Name} ({wh.Code})"})
                Next
                cboWarehouse.DisplayMember = "Value"
                cboWarehouse.ValueMember = "Key"
                If cboWarehouse.Items.Count > 0 Then cboWarehouse.SelectedIndex = 0

                Dim products = Await _productService.SearchProductsAsync("", Nothing, Nothing, onlyActive:=True)
                cboProduct.Items.Clear()
                For Each p In products
                    cboProduct.Items.Add(New With {.Key = p.ProductId, .Sku = p.Sku, .Name = p.Name, .Cost = p.CostPrice, .Value = $"{p.Sku} - {p.Name}"})
                Next
                cboProduct.DisplayMember = "Value"
                cboProduct.ValueMember = "Key"
                If cboProduct.Items.Count > 0 Then cboProduct.SelectedIndex = 0
            Catch ex As Exception
                MessageBox.Show($"Error loading: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Sub CboProduct_SelectedIndexChanged(sender As Object, e As EventArgs)
            If cboProduct.SelectedItem Is Nothing Then Return
            Dim itm = CType(cboProduct.SelectedItem, Object)
            Dim cost = CDec(itm.GetType().GetProperty("Cost").GetValue(itm, Nothing))
            numCost.Value = cost
        End Sub

        Private Sub BtnAddItem_Click(sender As Object, e As EventArgs)
            If cboProduct.SelectedItem Is Nothing Then Return
            Dim itm = CType(cboProduct.SelectedItem, Object)
            Dim prodId = CInt(itm.GetType().GetProperty("Key").GetValue(itm, Nothing))
            Dim sku = itm.GetType().GetProperty("Sku").GetValue(itm, Nothing).ToString()
            Dim name = itm.GetType().GetProperty("Name").GetValue(itm, Nothing).ToString()
            Dim qty = Convert.ToInt32(numQty.Value)
            Dim cost = numCost.Value

            _items.Add(New PurchaseOrderItem() With {
                .ProductId = prodId,
                .ProductSku = sku,
                .ProductName = name,
                .Quantity = qty,
                .UnitCost = cost,
                .DiscountPercent = 0,
                .LineTotal = qty * cost
            })

            RefreshGrid()
        End Sub

        Private Sub RefreshGrid()
            dgvItems.Rows.Clear()
            Dim total As Decimal = 0
            For Each itm In _items
                dgvItems.Rows.Add(itm.ProductSku, itm.ProductName, itm.Quantity, itm.UnitCost.ToString("C2"), itm.LineTotal.ToString("C2"))
                total += itm.LineTotal
            Next
            lblTotal.Text = $"Total: {total.ToString("C2")}"
        End Sub

        Private Async Sub BtnSaveDraft_Click(sender As Object, e As EventArgs)
            If cboSupplier.SelectedItem Is Nothing OrElse cboWarehouse.SelectedItem Is Nothing Then
                MessageBox.Show("Please select supplier and destination warehouse.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If _items.Count = 0 Then
                MessageBox.Show("Please add at least one product item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim supItem = CType(cboSupplier.SelectedItem, Object)
            Dim whItem = CType(cboWarehouse.SelectedItem, Object)
            Dim supId = CInt(supItem.GetType().GetProperty("Key").GetValue(supItem, Nothing))
            Dim whId = CInt(whItem.GetType().GetProperty("Key").GetValue(whItem, Nothing))

            Dim po As New PurchaseOrder() With {
                .SupplierId = supId,
                .WarehouseId = whId,
                .Status = "Draft",
                .OrderDate = dtpOrderDate.Value.Date,
                .ExpectedDate = dtpExpectedDate.Value.Date,
                .Notes = txtNotes.Text.Trim(),
                .Items = _items
            }

            btnSaveDraft.Enabled = False
            Try
                Dim res = Await _purchaseService.CreatePurchaseOrderAsync(po)
                If res.Success Then
                    MessageBox.Show(res.Message, "Purchase Order Created", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    DialogResult = DialogResult.OK
                    Close()
                Else
                    MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            Finally
                btnSaveDraft.Enabled = True
            End Try
        End Sub
    End Class

    Public Class NewSalesOrderDialog
        Inherits Form

        Private ReadOnly _salesService As New SalesService()
        Private ReadOnly _customerService As New CustomerService()
        Private ReadOnly _warehouseService As New WarehouseService()
        Private ReadOnly _productService As New ProductService()

        Private cboCustomer As ComboBox
        Private cboWarehouse As ComboBox
        Private dtpOrderDate As DateTimePicker
        Private dtpDeliveryDate As DateTimePicker
        Private cboProduct As ComboBox
        Private numQty As NumericUpDown
        Private numPrice As NumericUpDown
        Private btnAddItem As Button
        Private dgvItems As DataGridView
        Private txtNotes As TextBox
        Private lblTotal As Label
        Private btnSaveDraft As Button
        Private btnCancel As Button

        Private ReadOnly _items As New List(Of SalesOrderItem)()

        Public Sub New()
            InitializeComponent()
            LoadData()
        End Sub

        Private Sub InitializeComponent()
            Text = "Create Sales Order"
            Size = New Size(720, 620)
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.White
            Font = UITheme.FontBody

            Dim lblTitle As New Label() With {
                .Text = "New Customer Sales Order",
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.TextPrimary,
                .Location = New Point(24, 18),
                .AutoSize = True
            }
            Controls.Add(lblTitle)

            Dim lblCust As New Label() With {.Text = "Customer *", .Location = New Point(24, 60), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblCust)
            cboCustomer = New ComboBox() With {.Location = New Point(24, 80), .Width = 320, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            Controls.Add(cboCustomer)

            Dim lblWh As New Label() With {.Text = "Issuing Warehouse *", .Location = New Point(360, 60), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblWh)
            cboWarehouse = New ComboBox() With {.Location = New Point(360, 80), .Width = 320, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            Controls.Add(cboWarehouse)

            Dim lblDate As New Label() With {.Text = "Order Date", .Location = New Point(24, 115), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblDate)
            dtpOrderDate = New DateTimePicker() With {.Location = New Point(24, 135), .Width = 150, .Format = DateTimePickerFormat.Short, .Font = UITheme.FontBody}
            Controls.Add(dtpOrderDate)

            Dim lblExp As New Label() With {.Text = "Requested Delivery", .Location = New Point(190, 115), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblExp)
            dtpDeliveryDate = New DateTimePicker() With {.Location = New Point(190, 135), .Width = 154, .Format = DateTimePickerFormat.Short, .Font = UITheme.FontBody}
            Controls.Add(dtpDeliveryDate)

            Dim lblAddTitle As New Label() With {.Text = "Order Line Items", .Location = New Point(24, 175), .AutoSize = True, .Font = UITheme.FontSubtitle, .ForeColor = UITheme.PrimaryDark}
            Controls.Add(lblAddTitle)

            cboProduct = New ComboBox() With {.Location = New Point(24, 200), .Width = 280, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            AddHandler cboProduct.SelectedIndexChanged, AddressOf CboProduct_SelectedIndexChanged
            Controls.Add(cboProduct)

            numQty = New NumericUpDown() With {.Location = New Point(315, 200), .Width = 70, .Minimum = 1, .Maximum = 100000, .Value = 1, .Font = UITheme.FontBody}
            Controls.Add(numQty)

            numPrice = New NumericUpDown() With {.Location = New Point(395, 200), .Width = 100, .DecimalPlaces = 2, .Maximum = 1000000D, .Font = UITheme.FontBody}
            Controls.Add(numPrice)

            btnAddItem = New Button() With {.Text = "+ Add Line", .Location = New Point(505, 198), .Width = 120, .Height = 28}
            UITheme.StylePrimaryButton(btnAddItem)
            AddHandler btnAddItem.Click, AddressOf BtnAddItem_Click
            Controls.Add(btnAddItem)

            dgvItems = New DataGridView() With {.Location = New Point(24, 235), .Size = New Size(656, 170)}
            UITheme.ApplyModernDataGridStyle(dgvItems)
            dgvItems.Columns.Add("Sku", "SKU")
            dgvItems.Columns.Add("Name", "Product Name")
            dgvItems.Columns.Add("Qty", "Qty")
            dgvItems.Columns.Add("Price", "Unit Price")
            dgvItems.Columns.Add("Total", "Line Total")
            dgvItems.Columns(0).Width = 100
            dgvItems.Columns(1).AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            dgvItems.Columns(2).Width = 70
            dgvItems.Columns(3).Width = 100
            dgvItems.Columns(4).Width = 110
            Controls.Add(dgvItems)

            lblTotal = New Label() With {
                .Text = "Total Amount: $0.00",
                .Location = New Point(450, 415),
                .Size = New Size(230, 25),
                .TextAlign = ContentAlignment.MiddleRight,
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.PrimaryDark
            }
            Controls.Add(lblTotal)

            Dim lblNotes As New Label() With {.Text = "Customer / Shipping Notes", .Location = New Point(24, 420), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblNotes)
            txtNotes = New TextBox() With {.Location = New Point(24, 440), .Width = 400, .Height = 50, .Multiline = True, .Font = UITheme.FontBody}
            Controls.Add(txtNotes)

            btnSaveDraft = New Button() With {.Text = "Create Sales Order", .Location = New Point(430, 520), .Width = 150, .Height = 36}
            UITheme.StylePrimaryButton(btnSaveDraft)
            AddHandler btnSaveDraft.Click, AddressOf BtnSaveDraft_Click
            Controls.Add(btnSaveDraft)

            btnCancel = New Button() With {.Text = "Cancel", .Location = New Point(590, 520), .Width = 90, .Height = 36, .DialogResult = DialogResult.Cancel}
            UITheme.StyleSecondaryButton(btnCancel)
            Controls.Add(btnCancel)

            AcceptButton = btnSaveDraft
            CancelButton = btnCancel
        End Sub

        Private Async Sub LoadData()
            Try
                Dim customers = Await _customerService.GetAllCustomersAsync(onlyActive:=True)
                cboCustomer.Items.Clear()
                For Each c In customers
                    cboCustomer.Items.Add(New With {.Key = c.CustomerId, .Value = c.FullName})
                Next
                cboCustomer.DisplayMember = "Value"
                cboCustomer.ValueMember = "Key"
                If cboCustomer.Items.Count > 0 Then cboCustomer.SelectedIndex = 0

                Dim warehouses = Await _warehouseService.GetAllWarehousesAsync(onlyActive:=True)
                cboWarehouse.Items.Clear()
                For Each wh In warehouses
                    cboWarehouse.Items.Add(New With {.Key = wh.WarehouseId, .Value = $"{wh.Name} ({wh.Code})"})
                Next
                cboWarehouse.DisplayMember = "Value"
                cboWarehouse.ValueMember = "Key"
                If cboWarehouse.Items.Count > 0 Then cboWarehouse.SelectedIndex = 0

                Dim products = Await _productService.SearchProductsAsync("", Nothing, Nothing, onlyActive:=True)
                cboProduct.Items.Clear()
                For Each p In products
                    cboProduct.Items.Add(New With {.Key = p.ProductId, .Sku = p.Sku, .Name = p.Name, .Price = p.SellingPrice, .Value = $"{p.Sku} - {p.Name}"})
                Next
                cboProduct.DisplayMember = "Value"
                cboProduct.ValueMember = "Key"
                If cboProduct.Items.Count > 0 Then cboProduct.SelectedIndex = 0
            Catch ex As Exception
                MessageBox.Show($"Error loading: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Sub CboProduct_SelectedIndexChanged(sender As Object, e As EventArgs)
            If cboProduct.SelectedItem Is Nothing Then Return
            Dim itm = CType(cboProduct.SelectedItem, Object)
            Dim price = CDec(itm.GetType().GetProperty("Price").GetValue(itm, Nothing))
            numPrice.Value = price
        End Sub

        Private Sub BtnAddItem_Click(sender As Object, e As EventArgs)
            If cboProduct.SelectedItem Is Nothing Then Return
            Dim itm = CType(cboProduct.SelectedItem, Object)
            Dim prodId = CInt(itm.GetType().GetProperty("Key").GetValue(itm, Nothing))
            Dim sku = itm.GetType().GetProperty("Sku").GetValue(itm, Nothing).ToString()
            Dim name = itm.GetType().GetProperty("Name").GetValue(itm, Nothing).ToString()
            Dim qty = Convert.ToInt32(numQty.Value)
            Dim price = numPrice.Value

            _items.Add(New SalesOrderItem() With {
                .ProductId = prodId,
                .ProductSku = sku,
                .ProductName = name,
                .Quantity = qty,
                .UnitPrice = price,
                .DiscountPercent = 0,
                .LineTotal = qty * price
            })

            RefreshGrid()
        End Sub

        Private Sub RefreshGrid()
            dgvItems.Rows.Clear()
            Dim total As Decimal = 0
            For Each itm In _items
                dgvItems.Rows.Add(itm.ProductSku, itm.ProductName, itm.Quantity, itm.UnitPrice.ToString("C2"), itm.LineTotal.ToString("C2"))
                total += itm.LineTotal
            Next
            lblTotal.Text = $"Total: {total.ToString("C2")}"
        End Sub

        Private Async Sub BtnSaveDraft_Click(sender As Object, e As EventArgs)
            If cboCustomer.SelectedItem Is Nothing OrElse cboWarehouse.SelectedItem Is Nothing Then
                MessageBox.Show("Please select customer and issuing warehouse.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If _items.Count = 0 Then
                MessageBox.Show("Please add at least one product item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim custItem = CType(cboCustomer.SelectedItem, Object)
            Dim whItem = CType(cboWarehouse.SelectedItem, Object)
            Dim custId = CInt(custItem.GetType().GetProperty("Key").GetValue(custItem, Nothing))
            Dim whId = CInt(whItem.GetType().GetProperty("Key").GetValue(whItem, Nothing))

            Dim so As New SalesOrder() With {
                .CustomerId = custId,
                .WarehouseId = whId,
                .Status = "Draft",
                .OrderDate = dtpOrderDate.Value.Date,
                .DeliveryDate = dtpDeliveryDate.Value.Date,
                .Notes = txtNotes.Text.Trim(),
                .Items = _items
            }

            btnSaveDraft.Enabled = False
            Try
                Dim res = Await _salesService.CreateSalesOrderAsync(so)
                If res.Success Then
                    MessageBox.Show(res.Message, "Sales Order Created", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    DialogResult = DialogResult.OK
                    Close()
                Else
                    MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            Finally
                btnSaveDraft.Enabled = True
            End Try
        End Sub
    End Class

    Public Class UserEditDialog
        Inherits Form

        Private ReadOnly _userService As New UserService()
        Private _user As User

        Private txtUsername As TextBox
        Private txtPassword As TextBox
        Private txtFullName As TextBox
        Private txtEmail As TextBox
        Private txtPhone As TextBox
        Private cboRole As ComboBox
        Private chkIsActive As CheckBox
        Private btnSave As Button
        Private btnCancel As Button

        Public Sub New(Optional user As User = Nothing)
            _user = If(user, New User())
            InitializeComponent()
            LoadRoles()
        End Sub

        Private Sub InitializeComponent()
            Text = If(_user.UserId > 0, "Edit User Account", "New User Account")
            Size = New Size(460, 480)
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.White
            Font = UITheme.FontBody

            Dim lblTitle As New Label() With {
                .Text = If(_user.UserId > 0, "Edit User Account Details", "Create User Account"),
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.TextPrimary,
                .Location = New Point(24, 18),
                .AutoSize = True
            }
            Controls.Add(lblTitle)

            Dim lblUser As New Label() With {.Text = "Username *", .Location = New Point(24, 60), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblUser)
            txtUsername = New TextBox() With {.Location = New Point(24, 80), .Width = 396, .Text = _user.Username, .Enabled = (_user.UserId = 0), .Font = UITheme.FontBody}
            Controls.Add(txtUsername)

            Dim lblPass As New Label() With {.Text = If(_user.UserId > 0, "Reset Password (leave blank to keep current)", "Initial Password * (min 6 chars)"), .Location = New Point(24, 115), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblPass)
            txtPassword = New TextBox() With {.Location = New Point(24, 135), .Width = 396, .UseSystemPasswordChar = True, .Font = UITheme.FontBody}
            Controls.Add(txtPassword)

            Dim lblName As New Label() With {.Text = "Full Name *", .Location = New Point(24, 170), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblName)
            txtFullName = New TextBox() With {.Location = New Point(24, 190), .Width = 396, .Text = _user.FullName, .Font = UITheme.FontBody}
            Controls.Add(txtFullName)

            Dim lblEmail As New Label() With {.Text = "Email Address", .Location = New Point(24, 225), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblEmail)
            txtEmail = New TextBox() With {.Location = New Point(24, 245), .Width = 190, .Text = _user.Email, .Font = UITheme.FontBody}
            Controls.Add(txtEmail)

            Dim lblPhone As New Label() With {.Text = "Phone Number", .Location = New Point(230, 225), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblPhone)
            txtPhone = New TextBox() With {.Location = New Point(230, 245), .Width = 190, .Text = _user.Phone, .Font = UITheme.FontBody}
            Controls.Add(txtPhone)

            Dim lblRole As New Label() With {.Text = "Assigned Role *", .Location = New Point(24, 280), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblRole)
            cboRole = New ComboBox() With {.Location = New Point(24, 300), .Width = 396, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            Controls.Add(cboRole)

            chkIsActive = New CheckBox() With {.Text = "Account is Active and Enabled", .Location = New Point(24, 340), .Checked = _user.IsActive, .AutoSize = True}
            Controls.Add(chkIsActive)

            btnSave = New Button() With {.Text = "Save User", .Location = New Point(200, 385), .Width = 120, .Height = 35}
            UITheme.StylePrimaryButton(btnSave)
            AddHandler btnSave.Click, AddressOf BtnSave_Click
            Controls.Add(btnSave)

            btnCancel = New Button() With {.Text = "Cancel", .Location = New Point(330, 385), .Width = 90, .Height = 35, .DialogResult = DialogResult.Cancel}
            UITheme.StyleSecondaryButton(btnCancel)
            Controls.Add(btnCancel)

            AcceptButton = btnSave
            CancelButton = btnCancel
        End Sub

        Private Async Sub LoadRoles()
            Try
                Dim roles = Await _userService.GetAllRolesAsync()
                cboRole.Items.Clear()
                For Each r In roles
                    cboRole.Items.Add(New With {.Key = r.RoleId, .Value = $"{r.RoleName} - {r.Description}"})
                Next
                cboRole.DisplayMember = "Value"
                cboRole.ValueMember = "Key"
                If cboRole.Items.Count > 0 Then cboRole.SelectedIndex = 0

                If _user.RoleId > 0 Then
                    For i As Integer = 0 To cboRole.Items.Count - 1
                        Dim itm = CType(cboRole.Items(i), Object)
                        If CInt(itm.GetType().GetProperty("Key").GetValue(itm, Nothing)) = _user.RoleId Then
                            cboRole.SelectedIndex = i
                            Exit For
                        End If
                    Next
                End If
            Catch ex As Exception
                MessageBox.Show($"Error loading roles: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End Try
        End Sub

        Private Async Sub BtnSave_Click(sender As Object, e As EventArgs)
            If String.IsNullOrWhiteSpace(txtFullName.Text) Then
                MessageBox.Show("Full Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtFullName.Focus()
                Return
            End If

            If cboRole.SelectedItem Is Nothing Then
                MessageBox.Show("Please select a role.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim roleItem = CType(cboRole.SelectedItem, Object)
            Dim roleId = CInt(roleItem.GetType().GetProperty("Key").GetValue(roleItem, Nothing))

            _user.FullName = txtFullName.Text.Trim()
            _user.Email = txtEmail.Text.Trim()
            _user.Phone = txtPhone.Text.Trim()
            _user.RoleId = roleId
            _user.IsActive = chkIsActive.Checked

            btnSave.Enabled = False
            Try
                If _user.UserId = 0 Then
                    If String.IsNullOrWhiteSpace(txtUsername.Text) Then
                        MessageBox.Show("Username is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        txtUsername.Focus()
                        Return
                    End If
                    If String.IsNullOrWhiteSpace(txtPassword.Text) OrElse txtPassword.Text.Length < 6 Then
                        MessageBox.Show("Password must be at least 6 characters.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        txtPassword.Focus()
                        Return
                    End If
                    _user.Username = txtUsername.Text.Trim()

                    Dim res = Await _userService.CreateUserAsync(_user, txtPassword.Text)
                    If res.Success Then
                        DialogResult = DialogResult.OK
                        Close()
                    Else
                        MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If
                Else
                    Dim res = Await _userService.UpdateUserAsync(_user)
                    If res.Success Then
                        If Not String.IsNullOrWhiteSpace(txtPassword.Text) Then
                            Await _userService.ResetPasswordAsync(_user.UserId, txtPassword.Text)
                        End If
                        DialogResult = DialogResult.OK
                        Close()
                    Else
                        MessageBox.Show(res.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    End If
                End If
            Finally
                btnSave.Enabled = True
            End Try
        End Sub
    End Class

    Public Class DatabaseConfigDialog
        Inherits Form

        Private txtHost As TextBox
        Private txtPort As TextBox
        Private txtDatabase As TextBox
        Private txtUsername As TextBox
        Private txtPassword As TextBox
        Private chkSsl As CheckBox
        Private txtFullConnString As TextBox
        Private btnTest As Button
        Private btnInitSchema As Button
        Private btnSave As Button
        Private btnCancel As Button
        Private lblStatus As Label
        Private cboPreset As ComboBox

        Public Sub New()
            InitializeComponent()
            LoadCurrentConfiguration()
        End Sub

        Private Sub InitializeComponent()
            Text = "Database Connection & Supabase Setup"
            Size = New Size(620, 560)
            StartPosition = FormStartPosition.CenterParent
            FormBorderStyle = FormBorderStyle.FixedDialog
            MaximizeBox = False
            MinimizeBox = False
            BackColor = Color.White
            Font = UITheme.FontBody

            Dim lblTitle As New Label() With {
                .Text = "Database Connection Settings",
                .Font = UITheme.FontTitle,
                .ForeColor = UITheme.PrimaryDark,
                .Location = New Point(24, 18),
                .AutoSize = True
            }
            Controls.Add(lblTitle)

            Dim lblPreset As New Label() With {.Text = "Quick Preset Configuration:", .Location = New Point(24, 55), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblPreset)

            cboPreset = New ComboBox() With {.Location = New Point(24, 75), .Width = 556, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = UITheme.FontBody}
            cboPreset.Items.Add("Supabase Direct Database (db.your-ref.supabase.co / Port 5432)")
            cboPreset.Items.Add("Supabase Connection Pooler (Port 6543 / Universal IPv4 & IPv6)")
            cboPreset.Items.Add("Custom PostgreSQL Server")
            cboPreset.SelectedIndex = 0
            AddHandler cboPreset.SelectedIndexChanged, AddressOf CboPreset_SelectedIndexChanged
            Controls.Add(cboPreset)

            Dim top = 115
            Dim lblHost As New Label() With {.Text = "Host / Server Address *", .Location = New Point(24, top), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblHost)
            txtHost = New TextBox() With {.Location = New Point(24, top + 20), .Width = 380, .Font = UITheme.FontBody}
            AddHandler txtHost.TextChanged, AddressOf FormFieldChanged
            Controls.Add(txtHost)

            Dim lblPort As New Label() With {.Text = "Port *", .Location = New Point(420, top), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblPort)
            txtPort = New TextBox() With {.Location = New Point(420, top + 20), .Width = 160, .Font = UITheme.FontBody}
            AddHandler txtPort.TextChanged, AddressOf FormFieldChanged
            Controls.Add(txtPort)

            top += 55
            Dim lblDb As New Label() With {.Text = "Database Name *", .Location = New Point(24, top), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblDb)
            txtDatabase = New TextBox() With {.Location = New Point(24, top + 20), .Width = 260, .Text = "postgres", .Font = UITheme.FontBody}
            AddHandler txtDatabase.TextChanged, AddressOf FormFieldChanged
            Controls.Add(txtDatabase)

            Dim lblUser As New Label() With {.Text = "Database Username *", .Location = New Point(300, top), .AutoSize = True, .Font = UITheme.FontBodyBold}
            Controls.Add(lblUser)
            txtUsername = New TextBox() With {.Location = New Point(300, top + 20), .Width = 280, .Font = UITheme.FontBody}
            AddHandler txtUsername.TextChanged, AddressOf FormFieldChanged
            Controls.Add(txtUsername)

            top += 55
            Dim lblPass As New Label() With {.Text = "Database Password * (Enter your Supabase password)", .Location = New Point(24, top), .AutoSize = True, .Font = UITheme.FontBodyBold, .ForeColor = UITheme.PrimaryDark}
            Controls.Add(lblPass)
            txtPassword = New TextBox() With {.Location = New Point(24, top + 20), .Width = 556, .UseSystemPasswordChar = True, .Font = UITheme.FontBody}
            AddHandler txtPassword.TextChanged, AddressOf FormFieldChanged
            Controls.Add(txtPassword)

            top += 55
            Dim lblFull As New Label() With {.Text = "Generated Connection String (Npgsql):", .Location = New Point(24, top), .AutoSize = True, .Font = UITheme.FontSmall, .ForeColor = UITheme.TextSecondary}
            Controls.Add(lblFull)
            txtFullConnString = New TextBox() With {.Location = New Point(24, top + 18), .Width = 556, .ReadOnly = True, .BackColor = Color.FromArgb(248, 250, 252), .Font = UITheme.FontSmall}
            Controls.Add(txtFullConnString)

            top += 50
            btnTest = New Button() With {.Text = "Test Connection", .Location = New Point(24, top), .Width = 140, .Height = 35}
            UITheme.StyleSecondaryButton(btnTest)
            AddHandler btnTest.Click, AddressOf BtnTest_Click
            Controls.Add(btnTest)

            btnInitSchema = New Button() With {.Text = "Provision Tables & Seed", .Location = New Point(175, top), .Width = 180, .Height = 35}
            UITheme.StyleSecondaryButton(btnInitSchema)
            AddHandler btnInitSchema.Click, AddressOf BtnInitSchema_Click
            Controls.Add(btnInitSchema)

            btnSave = New Button() With {.Text = "Save & Apply", .Location = New Point(365, top), .Width = 120, .Height = 35}
            UITheme.StylePrimaryButton(btnSave)
            AddHandler btnSave.Click, AddressOf BtnSave_Click
            Controls.Add(btnSave)

            btnCancel = New Button() With {.Text = "Close", .Location = New Point(495, top), .Width = 85, .Height = 35, .DialogResult = DialogResult.Cancel}
            UITheme.StyleSecondaryButton(btnCancel)
            Controls.Add(btnCancel)

            lblStatus = New Label() With {
                .Text = "Please enter your Supabase database password, then click 'Test Connection'.",
                .Location = New Point(24, top + 45),
                .Size = New Size(556, 38),
                .ForeColor = UITheme.TextSecondary,
                .Font = UITheme.FontSmall
            }
            Controls.Add(lblStatus)

            CancelButton = btnCancel
        End Sub

        Private Sub LoadCurrentConfiguration()
            Dim config = AppConfiguration.Instance
            txtHost.Text = $"db.{config.SupabaseProjectRef}.supabase.co"
            txtPort.Text = "5432"
            txtUsername.Text = "postgres"
            txtDatabase.Text = "postgres"
            txtPassword.Text = "cd0g7JUJDuRjHqrR"

            Dim curConn = config.ConnectionString
            If Not String.IsNullOrWhiteSpace(curConn) AndAlso curConn.Contains("Password=") Then
                Dim parts() As String = curConn.Split(";"c)
                For Each part As String In parts
                    If part.Trim().StartsWith("Password=", StringComparison.OrdinalIgnoreCase) Then
                        Dim p = part.Trim().Substring("Password=".Length)
                        If Not p.Equals("YOUR_DATABASE_PASSWORD", StringComparison.OrdinalIgnoreCase) Then
                            txtPassword.Text = p
                        End If
                    ElseIf part.Trim().StartsWith("Host=", StringComparison.OrdinalIgnoreCase) Then
                        txtHost.Text = part.Trim().Substring("Host=".Length)
                    ElseIf part.Trim().StartsWith("Port=", StringComparison.OrdinalIgnoreCase) Then
                        txtPort.Text = part.Trim().Substring("Port=".Length)
                    ElseIf part.Trim().StartsWith("Username=", StringComparison.OrdinalIgnoreCase) Then
                        txtUsername.Text = part.Trim().Substring("Username=".Length)
                    End If
                Next
            End If

            UpdateConnectionString()
        End Sub

        Private Sub CboPreset_SelectedIndexChanged(sender As Object, e As EventArgs)
            Dim config = AppConfiguration.Instance
            Select Case cboPreset.SelectedIndex
                Case 0
                    txtHost.Text = $"db.{config.SupabaseProjectRef}.supabase.co"
                    txtPort.Text = "5432"
                    txtUsername.Text = "postgres"
                    txtDatabase.Text = "postgres"
                Case 1
                    txtHost.Text = If(String.IsNullOrWhiteSpace(config.PoolerHost), "aws-0-eu-central-1.pooler.supabase.com", config.PoolerHost)
                    txtPort.Text = "6543"
                    txtUsername.Text = If(String.IsNullOrWhiteSpace(config.PoolerUsername), $"postgres.{config.SupabaseProjectRef}", config.PoolerUsername)
                    txtDatabase.Text = "postgres"
            End Select
            UpdateConnectionString()
        End Sub

        Private Sub FormFieldChanged(sender As Object, e As EventArgs)
            UpdateConnectionString()
        End Sub

        Private Sub UpdateConnectionString()
            Dim host = txtHost.Text.Trim()
            Dim port = txtPort.Text.Trim()
            Dim db = txtDatabase.Text.Trim()
            Dim user = txtUsername.Text.Trim()
            Dim pass = txtPassword.Text

            txtFullConnString.Text = $"Host={host};Port={port};Database={db};Username={user};Password={pass};Pooling=true;SSL Mode=Require;Trust Server Certificate=true"
        End Sub

        Private Async Sub BtnTest_Click(sender As Object, e As EventArgs)
            btnTest.Enabled = False
            lblStatus.Text = "Connecting to database server..."
            lblStatus.ForeColor = UITheme.Primary

            Dim connStr = txtFullConnString.Text
            Dim res = Await DbConnectionFactory.TestConnectionAsync(connStr)
            If res.Success Then
                lblStatus.Text = res.Message
                lblStatus.ForeColor = UITheme.Success
            Else
                lblStatus.Text = res.Message
                lblStatus.ForeColor = UITheme.Danger
            End If
            btnTest.Enabled = True
        End Sub

        Private Async Sub BtnInitSchema_Click(sender As Object, e As EventArgs)
            Dim connStr = txtFullConnString.Text
            AppConfiguration.Instance.SaveConfiguration(connStr, AppConfiguration.Instance.CompanyName, AppConfiguration.Instance.CurrencySymbol)

            btnInitSchema.Enabled = False
            lblStatus.Text = "Executing DDL schema and initial seed..."
            lblStatus.ForeColor = UITheme.Primary

            Dim res = Await DatabaseInitializer.ResetAndSeedDatabaseAsync()
            If res.Success Then
                lblStatus.Text = res.Message
                lblStatus.ForeColor = UITheme.Success
                MessageBox.Show(res.Message, "Database Initialized", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Else
                lblStatus.Text = res.Message
                lblStatus.ForeColor = UITheme.Danger
                MessageBox.Show(res.Message, "Initialization Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
            btnInitSchema.Enabled = True
        End Sub

        Private Sub BtnSave_Click(sender As Object, e As EventArgs)
            Dim connStr = txtFullConnString.Text
            AppConfiguration.Instance.SaveConfiguration(connStr, AppConfiguration.Instance.CompanyName, AppConfiguration.Instance.CurrencySymbol)
            MessageBox.Show("Database connection saved and applied successfully!", "Configuration Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)
            DialogResult = DialogResult.OK
            Close()
        End Sub
    End Class

End Namespace
