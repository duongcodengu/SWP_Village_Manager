-- Tạo tất cả bảng SQL cho hệ thống quản lý nông sản - SQL Server Version
--Create database vllage_manager_database

-- 1. Roles
CREATE TABLE Roles (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVarchar(50) NOT NULL UNIQUE -- chỉ tồn tại 1 name role
);

-- 2. Users
CREATE TABLE Users (
    id INT PRIMARY KEY IDENTITY(1,1),
    username NVarchar(100) UNIQUE NOT NULL, -- check trùng username
    password NVarchar(255) NOT NULL,
    email NVarchar(100) UNIQUE NOT NULL, -- check trùng email
    role_id INT NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (role_id) REFERENCES Roles(id)
);
-- dành cho bán lẻ
CREATE TABLE RetailCustomer (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    full_name NVarchar(100),
    phone NVarchar(20) UNIQUE NOT NULL, -- check trùng phone
	CONSTRAINT CK_RetailCustomer_Phone_OnlyDigits CHECK (phone NOT LIKE '%[^0-9]%'),
    FOREIGN KEY (user_id) REFERENCES Users(id)
);
-- dành cho bán buôn cần xác minh thêm các thông tin doanh nghiệp
CREATE TABLE WholesaleCustomer (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    company_name NVarchar(100) UNIQUE NOT NULL, -- check trùng
    contact_person NVarchar(100) NOT NULL, -- bắt buộc phải có
    phone NVarchar(20) UNIQUE NOT NULL, -- check trùng phone
	CONSTRAINT CK_WholesaleCustomer_Phone_OnlyDigits CHECK (phone NOT LIKE '%[^0-9]%'),
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- 3. Farmer
CREATE TABLE Farmer (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    full_name NVarchar(100),
    phone NVarchar(20) UNIQUE NOT NULL, -- check trùng
	CONSTRAINT CK_Farmer_Phone_OnlyDigits CHECK (phone NOT LIKE '%[^0-9]%'),
    address TEXT NOT NULL, -- bắt buộc phải có
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- 4. Shipper
CREATE TABLE Shipper (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    full_name NVarchar(100),
    phone NVarchar(20) UNIQUE NOT NULL, -- check trùng
	CONSTRAINT CK_Shipper_Phone_OnlyDigits CHECK (phone NOT LIKE '%[^0-9]%'),
    vehicle_info TEXT, -- bắt buộc phải có
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- 5. ProductCategory
CREATE TABLE ProductCategory (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVarchar(100) UNIQUE, -- không đc trùng danh mục
	image_url NVARCHAR(255)
);

-- 6. Product
CREATE TABLE Product (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVarchar(100) UNIQUE NOT NULL, -- không trùng tên hàng hóa
    category_id INT NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    expiration_date DATE,
    product_type NVarchar(20) CHECK (product_type IN ('processed', 'raw')),
    quantity INT NOT NULL,
    processing_time DATE,
    farmer_id INT,
    FOREIGN KEY (category_id) REFERENCES ProductCategory(id),
    FOREIGN KEY (farmer_id) REFERENCES Farmer(id)
);

-- 1 hàng có nhiều ảnh
CREATE TABLE ProductImage (
    id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT,
    image_url NVarchar(255),
    description TEXT,
    uploaded_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (product_id) REFERENCES Product(id)
);

-- 7. Warehouse
CREATE TABLE Warehouse (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVarchar(100) UNIQUE NOT NULL,
    location TEXT
);

-- 8. Stock kho hàng
CREATE TABLE Stock (
    id INT PRIMARY KEY IDENTITY(1,1),
    warehouse_id INT,
    product_id INT,
    quantity INT,
    last_updated DATETIME,
    FOREIGN KEY (warehouse_id) REFERENCES Warehouse(id),
    FOREIGN KEY (product_id) REFERENCES Product(id)
);

-- 9. hóa đơn nhập khẩu
CREATE TABLE ImportInvoice (
    id INT PRIMARY KEY IDENTITY(1,1),
    warehouse_id INT,
    supplier_name NVarchar(100),
    total_amount DECIMAL(12,2),
    created_at DATETIME,
	purchase_time DATETIME,
    FOREIGN KEY (warehouse_id) REFERENCES Warehouse(id)
);

-- 10. chi tiết hóa đơn nhập khảu
CREATE TABLE ImportInvoiceDetail (
    id INT PRIMARY KEY IDENTITY(1,1),
    import_invoice_id INT,
    product_id INT,
    quantity INT,
    unit_price DECIMAL(10,2),
    FOREIGN KEY (import_invoice_id) REFERENCES ImportInvoice(id),
    FOREIGN KEY (product_id) REFERENCES Product(id)
);

-- 11. đơn bán buôn
CREATE TABLE WholesaleOrder (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    order_date DATETIME,
    status NVarchar(50) CHECK (status IN ('pending', 'confirmed', 'shipped', 'delivered', 'cancelled', 'returned')),
	confirmed_at DATETIME, -- xác nhận đơn hàng
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- 12. WholesaleOrderItem
CREATE TABLE WholesaleOrderItem (
    id INT PRIMARY KEY IDENTITY(1,1),
    order_id INT,
    product_id INT,
    quantity INT,
    unit_price DECIMAL(10,2),
    FOREIGN KEY (order_id) REFERENCES WholesaleOrder(id),
    FOREIGN KEY (product_id) REFERENCES Product(id)
);

-- 13. đơn bán lẻ
CREATE TABLE RetailOrder (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    order_date DATETIME,
    status NVarchar(50) CHECK (status IN ('pending', 'confirmed', 'shipped', 'delivered', 'cancelled', 'returned')),
	confirmed_at DATETIME, -- xác nhận đơn hàng
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- 14. RetailOrderItem
CREATE TABLE RetailOrderItem (
    id INT PRIMARY KEY IDENTITY(1,1),
    order_id INT,
    product_id INT,
    quantity INT,
    unit_price DECIMAL(10,2),
    FOREIGN KEY (order_id) REFERENCES RetailOrder(id),
    FOREIGN KEY (product_id) REFERENCES Product(id)
);

-- 15. Cart
CREATE TABLE Cart (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT UNIQUE,
    created_at DATETIME,
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- 16. CartItem
CREATE TABLE CartItem (
    id INT PRIMARY KEY IDENTITY(1,1),
    cart_id INT,
    product_id INT,
    quantity INT,
    FOREIGN KEY (cart_id) REFERENCES Cart(id),
    FOREIGN KEY (product_id) REFERENCES Product(id)
);

-- 17. Delivery -- bỏ Delivery_status bảng này chỉ những đơn deli r mới lưu vào đây ko cần status
CREATE TABLE Delivery (
    id INT PRIMARY KEY IDENTITY(1,1),
    order_type NVarchar(10) CHECK (order_type IN ('retail', 'wholesale', 'import')),
    order_id INT,
    shipper_id INT,
    shipping_fee DECIMAL(10,2),
    start_time DATETIME,
    end_time DATETIME,
    FOREIGN KEY (shipper_id) REFERENCES Shipper(id)
);

-- 18. ProcessingOrder
CREATE TABLE ProcessingOrder (
    id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT,
    quantity INT,
    send_date DATE,
    expected_return_date DATE,
    FOREIGN KEY (product_id) REFERENCES Product(id)
);

-- 19. ProcessingReceipt
CREATE TABLE ProcessingReceipt (
    id INT PRIMARY KEY IDENTITY(1,1),
    processing_order_id INT,
    received_date DATE,
    actual_quantity INT,
    FOREIGN KEY (processing_order_id) REFERENCES ProcessingOrder(id)
);

-- tách ra vì có thể 1 sản phẩm chế biến đc nhiều lần
-- dùng để lưu lịch sử của đơn chế biến
CREATE TABLE ProductProcessingHistory (
    id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT,
    sent_date DATE,
    return_date DATE,
    quantity INT,
    FOREIGN KEY (product_id) REFERENCES Product(id)
);


-- 20. ProcessedProduct - dùng để lưu tất cả kết quả 
CREATE TABLE ProcessedProduct (
    id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT,
	history_id INT,
    processed_date DATE,
    return_date DATE,
    processing_note TEXT,
	total_weight DECIMAL(10,2),
	image_url NVarchar(255),
	description TEXT,
    FOREIGN KEY (history_id) REFERENCES ProductProcessingHistory(id)
);

-- 21. Feedback chưa tất cả các đánh giá kể cả nhập kho hay xuất kho
CREATE TABLE Feedback (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    order_id INT,
    order_type NVarchar(10) CHECK (order_type IN ('retail', 'wholesale', 'import')),
    content TEXT,
    rating INT,
    created_at DATETIME,
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- 22. Address
CREATE TABLE Address (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    address_line TEXT,
    city NVarchar(100),
    province NVarchar(100),
    postal_code NVarchar(20),
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- 23. Payment cả thanh toán cho nhập kho hay xuất kho hoàn hàng
CREATE TABLE Payment (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    order_id INT,
    order_type NVarchar(10) CHECK (order_type IN ('retail', 'wholesale', 'import')),
    amount DECIMAL(10,2),
    paid_at DATETIME,
    method NVarchar(50) CHECK (method IN ('cash', 'card', 'bank_transfer')),
	payment_type NVarchar(10) CHECK (payment_type IN ('receive', 'refund'))
	-- chỉ nhận tiền mặt chuyển khoản hoặc qua visa
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- 24. TransportRoute
CREATE TABLE TransportRoute (
    id INT PRIMARY KEY IDENTITY(1,1),
    start_location TEXT,
    end_location TEXT,
    distance_km DECIMAL(10,2),
    estimated_time NVarchar(50)
);

-- 25. Notification
CREATE TABLE Notification (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    content TEXT,
    created_at DATETIME,
    is_read BIT DEFAULT 0,
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- 26. Log
CREATE TABLE Log (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    action TEXT,
    created_at DATETIME,
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- 28. Report
CREATE TABLE Report (
    id INT PRIMARY KEY IDENTITY(1,1),
    title NVarchar(100),
    content TEXT,
    created_at DATETIME
);

-- 29. Staff
CREATE TABLE Staff (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    role NVarchar(100),
    assigned_warehouse_id INT,
    FOREIGN KEY (user_id) REFERENCES Users(id),
    FOREIGN KEY (assigned_warehouse_id) REFERENCES Warehouse(id)
);

-- 30. Supplier
CREATE TABLE Supplier (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVarchar(100) UNIQUE NOT NULL, -- check trùng
    contact_info TEXT NOT NULL
);

-- hoàn hàng 
CREATE TABLE ReturnOrder (
    id INT PRIMARY KEY IDENTITY(1,1),
    order_type NVarchar(10) CHECK (order_type IN ('retail', 'wholesale', 'import')),
    order_id INT, -- bắt buộc phải kiểm tra trong code order_type 
    user_id INT,
    quantity INT,
    reason TEXT,
    created_at DATETIME,
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

------------------------------------INSERT--------------------------------------------------------------

INSERT INTO Roles (name) VALUES
('admin'),
('wholesale_staff'),
('wholesale_customer'),
('retail_staff'),
('retail_customer'),
('shipper'),
('farmer');

INSERT INTO Users (username, password, email, role_id) VALUES
('admin_user', 'admin123', 'admin@example.com', 1),
('wholesale_staff_user', '1', 'wholesale.staff@example.com', 2),
('wholesale_customer_user', '1', 'wholesale.customer@example.com', 3),
('retail_staff_user', '1', 'retail.staff@example.com', 4),
('retail_customer_user', '1', 'retail.customer@example.com', 5),
('shipper_user', '1', 'shipper@example.com', 6),
('farmer_user', '1', 'farmer@example.com', 7);

INSERT INTO ProductCategory (name, image_url) VALUES
(N'Vegetables & Fruit', N'/back-end/svg/vegetable.svg'),
(N'Beverages', N'/back-end/svg/cup.svg'),
(N'Meats & Seafood', N'/back-end/svg/meats.svg'),
(N'Breakfast', N'/back-end/svg/breakfast.svg'),
(N'Frozen Foods', N'/back-end/svg/frozen.svg'),
(N'Milk & Dairies', N'/back-end/svg/milk.svg'),
(N'Pet Food', N'/back-end/svg/pet.svg'),
(N'Vegetables & Fruit', N'/back-end/svg/vegetable.svg');

INSERT INTO RetailCustomer (user_id, full_name, phone)
VALUES
(5, N'Nguyễn Văn An', '0912345678');

INSERT INTO WholesaleCustomer (user_id, company_name, contact_person, phone)
VALUES
(3, N'Công ty TNHH Xuất Nhập Khẩu Tiến Phát', N'Phạm Thị Hồng', '0987654321');

INSERT INTO Farmer (user_id, full_name, phone, address)
VALUES
(7, N'Ngô Văn B', '0909998888', N'Ấp 1, Xã Tân Phú, Huyện Châu Thành, Tỉnh Đồng Tháp');

INSERT INTO Shipper (user_id, full_name, phone, vehicle_info)
VALUES
(6, N'Lê Văn C', '0911222333', N'Xe tải 1.5 tấn - 61A-12345');

INSERT INTO Warehouse (name, location)
VALUES
(N'Kho Miền Nam', N'KCN Tân Tạo, Bình Tân, TP.HCM'),
(N'Kho Miền Bắc', N'KCN Quang Minh, Mê Linh, Hà Nội');

INSERT INTO Product (name, category_id, price, expiration_date, product_type, quantity, processing_time, farmer_id)
VALUES
(N'Rau muống', 1, 15000, '2025-07-01', N'raw', 100, NULL, 1),
(N'Sữa tươi Vinamilk', 6, 25000, '2025-09-10', N'processed', 200, '2025-05-01', 1),
(N'Thịt heo ba chỉ', 3, 130000, '2025-06-20', N'raw', 50, NULL, 1),
(N'Cá basa phi lê', 3, 90000, '2025-06-15', N'raw', 70, NULL, 1);

INSERT INTO Stock (warehouse_id, product_id, quantity, last_updated)
VALUES
(1, 1, 40, GETDATE()),
(1, 2, 80, GETDATE()),
(2, 3, 30, GETDATE()),
(2, 4, 20, GETDATE());

INSERT INTO ImportInvoice (warehouse_id, supplier_name, total_amount, created_at, purchase_time)
VALUES
(1, N'Công ty Rau Xanh', 2000000, GETDATE(), '2025-06-01 09:30:00'),
(2, N'Công ty Thịt Sạch', 4000000, GETDATE(), '2025-06-05 15:20:00');

INSERT INTO ImportInvoiceDetail (import_invoice_id, product_id, quantity, unit_price)
VALUES
(1, 1, 50, 15000),
(1, 2, 60, 25000),
(2, 3, 20, 130000),
(2, 4, 30, 90000);

INSERT INTO WholesaleOrder (user_id, order_date, status, confirmed_at)
VALUES
(3, GETDATE(), N'pending', NULL);

INSERT INTO WholesaleOrderItem (order_id, product_id, quantity, unit_price)
VALUES
(1, 2, 30, 24000),
(1, 4, 15, 89000);

INSERT INTO RetailOrder (user_id, order_date, status, confirmed_at)
VALUES
(5, GETDATE(), N'confirmed', GETDATE());

INSERT INTO RetailOrderItem (order_id, product_id, quantity, unit_price)
VALUES
(1, 1, 3, 15000),
(1, 2, 1, 25000);

INSERT INTO Cart (user_id, created_at)
VALUES
(5, GETDATE());

INSERT INTO CartItem (cart_id, product_id, quantity)
VALUES
(1, 1, 2),
(1, 2, 1);

INSERT INTO Delivery (order_type, order_id, shipper_id, shipping_fee, start_time, end_time)
VALUES
(N'retail', 1, 1, 25000, GETDATE(), NULL);

INSERT INTO ProcessingOrder (product_id, quantity, send_date, expected_return_date)
VALUES
(1, 20, '2025-06-01', '2025-06-05');

INSERT INTO ProcessingReceipt (processing_order_id, received_date, actual_quantity)
VALUES
(1, '2025-06-05', 19);

INSERT INTO ProductProcessingHistory (product_id, sent_date, return_date, quantity)
VALUES
(1, '2025-06-01', '2025-06-05', 19);

INSERT INTO ProcessedProduct (product_id, history_id, processed_date, return_date, processing_note, total_weight, image_url, description)
VALUES
(1, 1, '2025-06-05', '2025-06-05', N'Đã xử lý đạt chuẩn an toàn', 18.5, N'/images/processed_rau.jpg', N'Rau muống đã rửa sạch và đóng gói');

INSERT INTO Feedback (user_id, order_id, order_type, content, rating, created_at)
VALUES
(5, 1, N'retail', N'Rau tươi, giao nhanh', 5, GETDATE());

INSERT INTO Feedback (user_id, order_id, order_type, content, rating, created_at)
VALUES
(5, 1, N'retail', N'Rau tươi, giao nhanh', 5, GETDATE());

INSERT INTO Address (user_id, address_line, city, province, postal_code)
VALUES
(5, N'456 Lê Lợi', N'TP.HCM', N'Hồ Chí Minh', N'700000');

INSERT INTO Payment (user_id, order_id, order_type, amount, paid_at, method, payment_type)
VALUES
(5, 1, N'retail', 55000, GETDATE(), N'cash', N'receive');

INSERT INTO TransportRoute (start_location, end_location, distance_km, estimated_time)
VALUES
(N'Kho Miền Nam', N'456 Lê Lợi, TP.HCM', 15.0, N'30 phút');

INSERT INTO Notification (user_id, content, created_at, is_read)
VALUES
(5, N'Đơn hàng của bạn đã được xác nhận!', GETDATE(), 0);

INSERT INTO Log (user_id, action, created_at)
VALUES
(5, N'Thêm đơn hàng bán lẻ', GETDATE());

INSERT INTO Report (title, content, created_at)
VALUES
(N'Thống kê bán hàng tháng 6', N'Số liệu bán hàng tăng trưởng 12% so với tháng trước', GETDATE());

INSERT INTO Staff (user_id, role, assigned_warehouse_id)
VALUES
(2, N'Kho Miền Nam Manager', 1);

INSERT INTO Supplier (name, contact_info)
VALUES
(N'Công ty Rau Xanh', N'Liên hệ: 028 12345678'),
(N'Công ty Thịt Sạch', N'Liên hệ: 024 98765432');

INSERT INTO ReturnOrder (order_type, order_id, user_id, quantity, reason, created_at)
VALUES
(N'retail', 1, 5, 1, N'Rau không tươi', GETDATE());

-- chua lay anh
INSERT INTO ProductImage (product_id, image_url, description)
VALUES
(1, N'/images/rau-muong.jpg', N'Rau muống tươi mới cắt sáng nay'),
(2, N'/images/sua-tuoi-vinamilk.jpg', N'Sữa tươi hộp 1L'),
(3, N'/images/thit-heo-ba-chi.jpg', N'Thịt ba chỉ tươi sạch'),
(4, N'/images/ca-basa.jpg', N'Cá basa phi lê tươi');



select * from ProductImage



