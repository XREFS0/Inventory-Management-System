CREATE TABLE IF NOT EXISTS roles (
    role_id SERIAL PRIMARY KEY,
    role_name VARCHAR(50) NOT NULL UNIQUE,
    description VARCHAR(255),
    is_system BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS permissions (
    permission_id SERIAL PRIMARY KEY,
    permission_code VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(255),
    module VARCHAR(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS role_permissions (
    role_id INT NOT NULL REFERENCES roles(role_id) ON DELETE CASCADE,
    permission_id INT NOT NULL REFERENCES permissions(permission_id) ON DELETE CASCADE,
    PRIMARY KEY (role_id, permission_id)
);

CREATE TABLE IF NOT EXISTS users (
    user_id SERIAL PRIMARY KEY,
    username VARCHAR(50) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(100) NOT NULL,
    email VARCHAR(100),
    phone VARCHAR(30),
    role_id INT NOT NULL REFERENCES roles(role_id),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS categories (
    category_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    code VARCHAR(30) UNIQUE,
    description VARCHAR(255),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS suppliers (
    supplier_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    company_name VARCHAR(100),
    contact_person VARCHAR(100),
    email VARCHAR(100),
    phone VARCHAR(30),
    address TEXT,
    tax_number VARCHAR(50),
    notes TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS customers (
    customer_id SERIAL PRIMARY KEY,
    full_name VARCHAR(100) NOT NULL,
    company_name VARCHAR(100),
    email VARCHAR(100),
    phone VARCHAR(30),
    address TEXT,
    tax_number VARCHAR(50),
    notes TEXT,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS warehouses (
    warehouse_id SERIAL PRIMARY KEY,
    name VARCHAR(100) NOT NULL,
    code VARCHAR(30) NOT NULL UNIQUE,
    location VARCHAR(200),
    manager_name VARCHAR(100),
    contact_phone VARCHAR(30),
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS products (
    product_id SERIAL PRIMARY KEY,
    sku VARCHAR(50) NOT NULL UNIQUE,
    barcode VARCHAR(100),
    name VARCHAR(150) NOT NULL,
    description TEXT,
    category_id INT REFERENCES categories(category_id) ON DELETE SET NULL,
    supplier_id INT REFERENCES suppliers(supplier_id) ON DELETE SET NULL,
    cost_price NUMERIC(12, 2) NOT NULL DEFAULT 0.00 CHECK (cost_price >= 0),
    selling_price NUMERIC(12, 2) NOT NULL DEFAULT 0.00 CHECK (selling_price >= 0),
    min_stock_level INT NOT NULL DEFAULT 10 CHECK (min_stock_level >= 0),
    unit_of_measure VARCHAR(30) NOT NULL DEFAULT 'Pieces',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS warehouse_stock (
    warehouse_id INT NOT NULL REFERENCES warehouses(warehouse_id) ON DELETE CASCADE,
    product_id INT NOT NULL REFERENCES products(product_id) ON DELETE CASCADE,
    quantity INT NOT NULL DEFAULT 0 CHECK (quantity >= 0),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (warehouse_id, product_id)
);

CREATE TABLE IF NOT EXISTS stock_transactions (
    transaction_id SERIAL PRIMARY KEY,
    product_id INT NOT NULL REFERENCES products(product_id),
    warehouse_id INT NOT NULL REFERENCES warehouses(warehouse_id),
    transaction_type VARCHAR(30) NOT NULL,
    quantity INT NOT NULL,
    previous_qty INT NOT NULL,
    new_qty INT NOT NULL,
    reference_type VARCHAR(50),
    reference_id VARCHAR(100),
    notes TEXT,
    created_by VARCHAR(50),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS stock_transfers (
    transfer_id SERIAL PRIMARY KEY,
    transfer_number VARCHAR(50) NOT NULL UNIQUE,
    source_warehouse_id INT NOT NULL REFERENCES warehouses(warehouse_id),
    destination_warehouse_id INT NOT NULL REFERENCES warehouses(warehouse_id),
    status VARCHAR(30) NOT NULL DEFAULT 'Completed',
    notes TEXT,
    transferred_by VARCHAR(50),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS stock_transfer_items (
    transfer_item_id SERIAL PRIMARY KEY,
    transfer_id INT NOT NULL REFERENCES stock_transfers(transfer_id) ON DELETE CASCADE,
    product_id INT NOT NULL REFERENCES products(product_id),
    quantity INT NOT NULL CHECK (quantity > 0)
);

CREATE TABLE IF NOT EXISTS purchase_orders (
    order_id SERIAL PRIMARY KEY,
    order_number VARCHAR(50) NOT NULL UNIQUE,
    supplier_id INT NOT NULL REFERENCES suppliers(supplier_id),
    warehouse_id INT NOT NULL REFERENCES warehouses(warehouse_id),
    order_date DATE NOT NULL DEFAULT CURRENT_DATE,
    expected_date DATE,
    received_date DATE,
    status VARCHAR(30) NOT NULL DEFAULT 'Draft',
    subtotal NUMERIC(14, 2) NOT NULL DEFAULT 0.00,
    tax_amount NUMERIC(14, 2) NOT NULL DEFAULT 0.00,
    discount_amount NUMERIC(14, 2) NOT NULL DEFAULT 0.00,
    total_amount NUMERIC(14, 2) NOT NULL DEFAULT 0.00,
    notes TEXT,
    created_by VARCHAR(50),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS purchase_order_items (
    item_id SERIAL PRIMARY KEY,
    order_id INT NOT NULL REFERENCES purchase_orders(order_id) ON DELETE CASCADE,
    product_id INT NOT NULL REFERENCES products(product_id),
    quantity INT NOT NULL CHECK (quantity > 0),
    received_quantity INT NOT NULL DEFAULT 0,
    unit_cost NUMERIC(12, 2) NOT NULL CHECK (unit_cost >= 0),
    total_price NUMERIC(14, 2) NOT NULL CHECK (total_price >= 0)
);

CREATE TABLE IF NOT EXISTS sales_orders (
    order_id SERIAL PRIMARY KEY,
    order_number VARCHAR(50) NOT NULL UNIQUE,
    customer_id INT NOT NULL REFERENCES customers(customer_id),
    warehouse_id INT NOT NULL REFERENCES warehouses(warehouse_id),
    order_date DATE NOT NULL DEFAULT CURRENT_DATE,
    status VARCHAR(30) NOT NULL DEFAULT 'Draft',
    payment_status VARCHAR(30) NOT NULL DEFAULT 'Pending',
    subtotal NUMERIC(14, 2) NOT NULL DEFAULT 0.00,
    tax_amount NUMERIC(14, 2) NOT NULL DEFAULT 0.00,
    discount_amount NUMERIC(14, 2) NOT NULL DEFAULT 0.00,
    total_amount NUMERIC(14, 2) NOT NULL DEFAULT 0.00,
    notes TEXT,
    created_by VARCHAR(50),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS sales_order_items (
    item_id SERIAL PRIMARY KEY,
    order_id INT NOT NULL REFERENCES sales_orders(order_id) ON DELETE CASCADE,
    product_id INT NOT NULL REFERENCES products(product_id),
    quantity INT NOT NULL CHECK (quantity > 0),
    unit_price NUMERIC(12, 2) NOT NULL CHECK (unit_price >= 0),
    total_price NUMERIC(14, 2) NOT NULL CHECK (total_price >= 0)
);

CREATE TABLE IF NOT EXISTS audit_logs (
    log_id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(user_id) ON DELETE SET NULL,
    username VARCHAR(50) NOT NULL,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    entity_id VARCHAR(100),
    old_values JSONB,
    new_values JSONB,
    ip_address VARCHAR(45),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS application_settings (
    setting_key VARCHAR(100) PRIMARY KEY,
    setting_value TEXT NOT NULL,
    description VARCHAR(255),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_products_sku ON products(sku);
CREATE INDEX IF NOT EXISTS idx_products_name ON products(name);
CREATE INDEX IF NOT EXISTS idx_products_category ON products(category_id);
CREATE INDEX IF NOT EXISTS idx_products_supplier ON products(supplier_id);
CREATE INDEX IF NOT EXISTS idx_warehouse_stock_lookup ON warehouse_stock(warehouse_id, product_id);
CREATE INDEX IF NOT EXISTS idx_stock_tx_product ON stock_transactions(product_id);
CREATE INDEX IF NOT EXISTS idx_stock_tx_warehouse ON stock_transactions(warehouse_id);
CREATE INDEX IF NOT EXISTS idx_stock_tx_date ON stock_transactions(created_at);
CREATE INDEX IF NOT EXISTS idx_audit_created_at ON audit_logs(created_at);
