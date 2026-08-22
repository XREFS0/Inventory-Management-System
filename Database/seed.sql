INSERT INTO roles (role_id, role_name, description, is_system) VALUES
(1, 'Administrator', 'Full system access and administrative privileges', TRUE),
(2, 'Manager', 'Operational management, approving orders and generating reports', TRUE),
(3, 'WarehouseStaff', 'Inventory management, stock receiving, and transfers', TRUE),
(4, 'SalesStaff', 'Customer management and sales order creation', TRUE)
ON CONFLICT (role_name) DO NOTHING;

SELECT setval('roles_role_id_seq', (SELECT MAX(role_id) FROM roles));

INSERT INTO permissions (permission_code, description, module) VALUES
('dashboard.view', 'View Dashboard KPIs and Analytics', 'Dashboard'),
('products.view', 'View Products List and Details', 'Products'),
('products.create', 'Create New Products', 'Products'),
('products.edit', 'Modify Existing Products', 'Products'),
('products.delete', 'Archive or Delete Products', 'Products'),
('categories.manage', 'Manage Product Categories', 'Categories'),
('warehouses.manage', 'Manage Warehouses and Facilities', 'Warehouses'),
('inventory.view', 'View Current Inventory Levels', 'Inventory'),
('inventory.adjust', 'Perform Stock Adjustments', 'Inventory'),
('inventory.transfer', 'Execute Stock Transfers between Warehouses', 'Inventory'),
('suppliers.manage', 'Manage Suppliers and Vendor info', 'Suppliers'),
('customers.manage', 'Manage Customers and Client records', 'Customers'),
('purchases.view', 'View Purchase Orders', 'Purchases'),
('purchases.create', 'Create Purchase Orders', 'Purchases'),
('purchases.approve', 'Approve and Receive Purchase Orders', 'Purchases'),
('sales.view', 'View Sales Orders', 'Sales'),
('sales.create', 'Create Sales Orders', 'Sales'),
('sales.complete', 'Complete and Dispatch Sales Orders', 'Sales'),
('reports.view', 'Generate and Export Business Reports', 'Reports'),
('users.manage', 'Manage Users and Roles', 'Users'),
('audit.view', 'View System Audit Logs', 'Audit'),
('settings.manage', 'Manage Application Configuration', 'Settings')
ON CONFLICT (permission_code) DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT 1, permission_id FROM permissions
ON CONFLICT DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT 2, permission_id FROM permissions 
WHERE permission_code NOT IN ('users.manage', 'settings.manage')
ON CONFLICT DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT 3, permission_id FROM permissions 
WHERE permission_code IN (
    'dashboard.view', 'products.view', 'inventory.view', 'inventory.adjust', 
    'inventory.transfer', 'warehouses.manage', 'purchases.view', 'purchases.approve'
)
ON CONFLICT DO NOTHING;

INSERT INTO role_permissions (role_id, permission_id)
SELECT 4, permission_id FROM permissions 
WHERE permission_code IN (
    'dashboard.view', 'products.view', 'inventory.view', 'customers.manage', 
    'sales.view', 'sales.create', 'sales.complete'
)
ON CONFLICT DO NOTHING;

INSERT INTO users (user_id, username, password_hash, full_name, email, phone, role_id, is_active)
VALUES (
    1, 
    'admin', 
    '$2a$11$98kGg3X46B6y2p7m8ZtCxejWnFvKupK7D1V6.2uG9g5jE.GfG6K0C', 
    'Mohamed El-Sayed (Admin)', 
    'admin@masainventory.com.eg', 
    '+20 100 234 5678', 
    1, 
    TRUE
)
ON CONFLICT (username) DO NOTHING;

SELECT setval('users_user_id_seq', (SELECT MAX(user_id) FROM users));

INSERT INTO categories (category_id, name, code, description) VALUES
(1, 'Electronics & Smart Hardware', 'CAT-ELEC', 'Microcontrollers, IoT modules, sensors, and electronic devices'),
(2, 'Raw Materials & Metals', 'CAT-RAW', 'Aluminum extrusions, steel alloys, copper wires, and polymers'),
(3, 'Packaging & Cartons', 'CAT-PKG', 'Corrugated boxes, protective wrap, strapping bands, and pallets'),
(4, 'Finished Commercial Goods', 'CAT-FIN', 'Assembled consumer-ready products, power supplies, and gateway units'),
(5, 'Warehouse Tools & Equipment', 'CAT-TOOL', 'Optical barcode scanners, digital scales, and calibration tools')
ON CONFLICT (name) DO NOTHING;

SELECT setval('categories_category_id_seq', (SELECT MAX(category_id) FROM categories));

INSERT INTO warehouses (warehouse_id, name, code, location, manager_name, contact_phone) VALUES
(1, 'Cairo Central Hub - 10th of Ramadan', 'WH-CAIRO-10R', 'Plot 45, Heavy Industrial Zone B3, 10th of Ramadan City, Sharqia', 'Tarek Abdel-Rahman', '+20 101 456 7890'),
(2, 'Giza & 6th of October Distribution Center', 'WH-GIZA-6OCT', 'Block 12, 3rd Industrial Zone, 6th of October City, Giza', 'Ahmed Mansour', '+20 112 345 6789'),
(3, 'Alexandria & Borg El-Arab Logistics Center', 'WH-ALEX-BORG', 'Zone 2, Logistics Park, Borg El-Arab New City, Alexandria', 'Mahmoud El-Shazly', '+20 122 789 0123')
ON CONFLICT (code) DO NOTHING;

SELECT setval('warehouses_warehouse_id_seq', (SELECT MAX(warehouse_id) FROM warehouses));

INSERT INTO suppliers (supplier_id, name, company_name, contact_person, email, phone, address, tax_number) VALUES
(1, 'Al-Ahram Electronics Supply', 'Al-Ahram Components & Tech SAE', 'Hassan El-Ghandour', 'sales@ahram-electronics.com.eg', '+20 2 2794 1122', 'Building 14, El-Nasr St, Nasr City, Cairo', 'EG-TRN-100-245-890'),
(2, 'El-Sewedy Industrial Materials', 'El-Sewedy Raw Materials Trading LLC', 'Youssef El-Bahr', 'supply@elsewedy-raw.com.eg', '+20 100 889 4433', 'Industrial Sector 5, 10th of Ramadan City', 'EG-TRN-200-567-123'),
(3, 'Nile Valley Packaging Solutions', 'Nile Valley Paper & Packaging SAE', 'Karim Abdel-Fattah', 'orders@nilevalleypack.com.eg', '+20 111 667 8899', 'Plot 88, Badr Industrial City, Cairo', 'EG-TRN-300-987-654')
ON CONFLICT DO NOTHING;

SELECT setval('suppliers_supplier_id_seq', (SELECT MAX(supplier_id) FROM suppliers));

INSERT INTO customers (customer_id, full_name, company_name, email, phone, address, tax_number) VALUES
(1, 'Eng. Mostafa Kamel', 'Misr Technology & Robotics Systems', 'procurement@misrtech-eg.com', '+20 100 334 5566', '5th Settlement, New Cairo, Cairo', 'EG-CUST-401-100'),
(2, 'Dr. Sarah Farouk', 'Alexandria Advanced Automation Co.', 'sfarouk@alex-automation.com', '+20 120 778 9900', 'Smouha Business Center, Alexandria', 'EG-CUST-402-200'),
(3, 'Hany El-Maghraby', 'Delta Manufacturing & Trading LLC', 'hany@deltamfg-eg.com', '+20 155 223 4455', 'Mansoura Industrial Park, Dakahlia', 'EG-CUST-403-300')
ON CONFLICT DO NOTHING;

SELECT setval('customers_customer_id_seq', (SELECT MAX(customer_id) FROM customers));

INSERT INTO products (product_id, sku, barcode, name, description, category_id, supplier_id, cost_price, selling_price, min_stock_level, unit_of_measure) VALUES
(1, 'SKU-ELEC-001', '6221234567890', 'ARM Cortex-M4 Industrial MCU Chip', '32-bit high-performance embedded microcontroller with 512KB Flash', 1, 1, 220.00, 395.00, 50, 'Pieces'),
(2, 'SKU-ELEC-002', '6221234567891', 'Wi-Fi / BLE 5.0 Dual Wireless Module', 'High-range IoT transceiver module with ceramic antenna', 1, 1, 155.00, 290.00, 40, 'Pieces'),
(3, 'SKU-RAW-001', '6221234567892', '6061 Structural Aluminum Profile (2m)', 'T-Slot structural anodized aluminum extrusion 20x20mm', 2, 2, 580.00, 890.00, 20, 'Bars'),
(4, 'SKU-RAW-002', '6221234567893', 'Precision Steel Bearings (Pack of 10)', 'ABEC-7 deep groove high-speed industrial bearings', 2, 2, 420.00, 680.00, 30, 'Packs'),
(5, 'SKU-PKG-001', '6221234567894', 'Heavy-Duty Corrugated Shipping Boxes (L)', 'Double-wall carton box 45x35x35cm for export and logistics', 3, 3, 45.00, 85.00, 100, 'Pieces'),
(6, 'SKU-FIN-001', '6221234567895', 'MASA 4G/LTE Industrial Smart Gateway Hub', 'Edge telemetry gateway with Modbus RTU, 4G LTE, and Ethernet', 4, 1, 6800.00, 11500.00, 10, 'Units'),
(7, 'SKU-TOOL-001', '6221234567896', 'Wireless 2D Optical Barcode & QR Scanner', 'Rugged IP54 industrial handheld scanner with USB cradle', 5, 1, 1650.00, 2750.00, 15, 'Units')
ON CONFLICT (sku) DO NOTHING;

SELECT setval('products_product_id_seq', (SELECT MAX(product_id) FROM products));

INSERT INTO warehouse_stock (warehouse_id, product_id, quantity) VALUES
(1, 1, 350),
(1, 2, 240),
(1, 3, 110),
(1, 4, 160),
(1, 5, 800),
(1, 6, 35),
(1, 7, 45),
(2, 1, 90),
(2, 3, 18),
(2, 6, 6),
(3, 5, 300),
(3, 7, 20)
ON CONFLICT (warehouse_id, product_id) DO UPDATE SET quantity = EXCLUDED.quantity;

INSERT INTO stock_transactions (product_id, warehouse_id, transaction_type, quantity, previous_qty, new_qty, reference_type, reference_id, notes, created_by) VALUES
(1, 1, 'Stock In', 350, 0, 350, 'INITIAL_SEED', 'INIT-EGY-001', 'Initial inventory stock initialization', 'admin'),
(2, 1, 'Stock In', 240, 0, 240, 'INITIAL_SEED', 'INIT-EGY-002', 'Initial inventory stock initialization', 'admin'),
(3, 1, 'Stock In', 110, 0, 110, 'INITIAL_SEED', 'INIT-EGY-003', 'Initial inventory stock initialization', 'admin'),
(4, 1, 'Stock In', 160, 0, 160, 'INITIAL_SEED', 'INIT-EGY-004', 'Initial inventory stock initialization', 'admin'),
(5, 1, 'Stock In', 800, 0, 800, 'INITIAL_SEED', 'INIT-EGY-005', 'Initial inventory stock initialization', 'admin'),
(6, 1, 'Stock In', 35, 0, 35, 'INITIAL_SEED', 'INIT-EGY-006', 'Initial inventory stock initialization', 'admin'),
(7, 1, 'Stock In', 45, 0, 45, 'INITIAL_SEED', 'INIT-EGY-007', 'Initial inventory stock initialization', 'admin');

INSERT INTO application_settings (setting_key, setting_value, description) VALUES
('Company.Name', 'MASA Enterprises Egypt SAE', 'Corporate registered entity name'),
('Company.Currency', 'EGP', 'Default accounting currency ISO code'),
('Company.CurrencySymbol', 'EGP', 'Default currency display symbol'),
('Company.LowStockThreshold', '10', 'Default minimum threshold for low stock alert trigger'),
('Company.TaxRatePercent', '14.00', 'Standard Egyptian VAT value added tax rate percentage')
ON CONFLICT (setting_key) DO NOTHING;
