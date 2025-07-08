-- Tạo tất cả bảng SQL cho hệ thống quản lý nông sản - SQL Server Version
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
    HasAcceptedGeolocation BIT NOT NULL DEFAULT 0,
    Phone Nvarchar(10) UNIQUE NOT NULL,
    created_at DATETIME DEFAULT GETDATE(),
    is_active BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (role_id) REFERENCES Roles(id)
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
	status NVARCHAR(50) 
    CONSTRAINT DF_status DEFAULT 'pending',
    CONSTRAINT CHK_status CHECK (status IN ('pending', 'approved', 'rejected', 'inactive')),
    FOREIGN KEY (user_id) REFERENCES Users(id)
);

-- 5. ProductCategory
CREATE TABLE ProductCategory (
    id INT PRIMARY KEY IDENTITY(1,1),
    name NVarchar(100) UNIQUE, -- không đc trùng danh mục
	imageUrl NVarchar(100)
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
	-- thêm thuộc tính này
	approval_status NVARCHAR(20) DEFAULT 'pending' CHECK (approval_status IN ('pending', 'accepted', 'rejected')),
    FOREIGN KEY (category_id) REFERENCES ProductCategory(id),
    FOREIGN KEY (farmer_id) REFERENCES Farmer(id)
);

-- 1 hàng có nhiều ảnh
CREATE TABLE ProductImage (
    id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT,
    image_url NVarchar(255),
    description NVarchar,
    uploaded_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (product_id) REFERENCES Product(id)
);


-- 8. Stock kho hàng
CREATE TABLE Stock (
    id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT,
    quantity INT,
    last_updated DATETIME,
    FOREIGN KEY (product_id) REFERENCES Product(id)
);

-- 9. hóa đơn nhập khẩu
CREATE TABLE ImportInvoice (
    id INT PRIMARY KEY IDENTITY(1,1),
    supplier_name NVarchar(100),
    total_amount DECIMAL(12,2),
    created_at DATETIME,
	purchase_time DATETIME,
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
    order_type NVarchar(10) CHECK (order_type IN ('retail', 'import')),
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
    order_type NVarchar(10) CHECK (order_type IN ('retail', 'import')),
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

-- 27. Session
CREATE TABLE Session (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT,
    session_token TEXT,
    created_at DATETIME,
    expires_at DATETIME,
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
    FOREIGN KEY (user_id) REFERENCES Users(id),
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
    order_type NVarchar(10) CHECK (order_type IN ('retail', 'import')),
    order_id INT, -- bắt buộc phải kiểm tra trong code order_type 
    user_id INT,
    quantity INT,
    reason TEXT,
    created_at DATETIME,
    FOREIGN KEY (user_id) REFERENCES Users(id)
);
------------------------------------ADD Table--------------------------------------------------------------

CREATE TABLE SupplyRequest (
    id INT PRIMARY KEY IDENTITY(1,1),   
    requester_type NVARCHAR(10) CHECK (requester_type IN ('admin', 'farmer')) NOT NULL,
    requester_id INT NOT NULL,   
    receiver_id INT NOT NULL,   
    farmer_id INT NOT NULL,     
    product_name NVARCHAR(100) NOT NULL,
    quantity INT NOT NULL,
    price DECIMAL(10,2), -- giá đề xuất
    status NVARCHAR(20) CHECK (status IN ('pending', 'accepted', 'rejected')) DEFAULT 'pending',
    requested_at DATETIME DEFAULT GETDATE(),
    responded_at DATETIME NULL,
    FOREIGN KEY (requester_id) REFERENCES Users(id),
    FOREIGN KEY (receiver_id) REFERENCES Users(id),
    FOREIGN KEY (farmer_id) REFERENCES Farmer(id),
);

CREATE TABLE FarmerRegistrationRequest (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT NOT NULL,	
    full_name NVARCHAR(100) NOT NULL,
    phone NVARCHAR(20) UNIQUE NOT NULL,
    address TEXT NOT NULL,
    status NVARCHAR(20) CHECK (status IN ('pending', 'approved', 'rejected')) DEFAULT 'pending',
    requested_at DATETIME DEFAULT GETDATE(),
    reviewed_at DATETIME NULL,
    reviewed_by INT NULL, -- admin user_id
    FOREIGN KEY (user_id) REFERENCES Users(id),
    FOREIGN KEY (reviewed_by) REFERENCES Users(id)
);
CREATE TABLE ShipperRegistrationRequest (
    id INT PRIMARY KEY IDENTITY(1,1),
    user_id INT NOT NULL,	
    full_name NVARCHAR(100) NOT NULL,
    phone NVARCHAR(20) UNIQUE NOT NULL,
    address TEXT NOT NULL,
    status NVARCHAR(20) CHECK (status IN ('pending', 'approved', 'rejected')) DEFAULT 'pending',
    requested_at DATETIME DEFAULT GETDATE(),
	vehicle_info TEXT, -- bắt buộc phải có
    reviewed_at DATETIME NULL,
    reviewed_by INT NULL, -- admin user_id
    FOREIGN KEY (user_id) REFERENCES Users(id),
    FOREIGN KEY (reviewed_by) REFERENCES Users(id)
);

CREATE TABLE UserLocations (
    Id INT PRIMARY KEY IDENTITY,
    UserId int NOT NULL,
    Label NVARCHAR(100),             
    Address NVARCHAR(255),           
    Latitude FLOAT NOT NULL,
    Longitude FLOAT NOT NULL,
    CreatedAt DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

CREATE TABLE Delivery (
    id INT PRIMARY KEY IDENTITY(1,1),
    order_type NVARCHAR(10) CHECK (order_type IN ('retail', 'import')),
    order_id INT,
    shipper_id INT,
    shipping_fee DECIMAL(10,2),
    start_time DATETIME,
    end_time DATETIME,
    status NVARCHAR(50) CHECK (status IN ('assigned', 'in_transit', 'delivered', 'failed')), -- trạng thái đơn giao
    customer_name NVARCHAR(100),      -- tên khách hàng nhận
    customer_address NVARCHAR(255),   -- địa chỉ giao hàng
    customer_phone NVARCHAR(20),      -- số điện thoại khách
    FOREIGN KEY (shipper_id) REFERENCES Shipper(id)
);

CREATE TABLE DeliveryIssue (
    id INT PRIMARY KEY IDENTITY(1,1),
    delivery_id INT,
    shipper_id INT,
    issue_type NVARCHAR(50),
    description NVARCHAR(255),
    reported_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (delivery_id) REFERENCES Delivery(id),
    FOREIGN KEY (shipper_id) REFERENCES Shipper(id)
);

CREATE TABLE DeliveryProof (
    id INT PRIMARY KEY IDENTITY(1,1),
    delivery_id INT,
    shipper_id INT,
    image_path NVARCHAR(255),
    note NVARCHAR(255),
    created_at DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (delivery_id) REFERENCES Delivery(id),
    FOREIGN KEY (shipper_id) REFERENCES Shipper(id)
);

CREATE TABLE ContactMessages (
    Id INT PRIMARY KEY IDENTITY,
    FirstName NVARCHAR(100),
    LastName NVARCHAR(100),
    Email NVARCHAR(150),
    PhoneNumber NVARCHAR(20),
    Message NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE HiddenProduct (
    id INT PRIMARY KEY IDENTITY(1,1),
    product_id INT NOT NULL,
    
    reason TEXT NULL, -- tùy chọn: lý do ẩn sản phẩm
    hidden_at DATETIME DEFAULT GETDATE(),
    
    FOREIGN KEY (product_id) REFERENCES Product(id),
    
);
------------------------------------INSERT--------------------------------------------------------------

INSERT INTO Roles (name) VALUES
('admin'),
('staff'),
('customer'),
('shipper'),
('farmer');

INSERT INTO Users (username, password, email, role_id, HasAcceptedGeolocation, Phone)
VALUES (N'admin', N'admin123', N'admin@example.com', 1, 0, 0123456789);

INSERT INTO Users (username, password, email, role_id, HasAcceptedGeolocation, Phone)
VALUES (N'Staff', N'123', N'admin@gmail.com', 2, 0, 0123456788);

INSERT INTO ProductCategory (name, imageUrl) VALUES
(N'Vegetables & Fruit', N'back-end/svg/vegetable.svg'),
(N'Beverages', N'back-end/svg/cup.svg'),
(N'Meats & Seafood', N'back-end/svg/meats.svg'),
(N'Breakfast', N'back-end/svg/breakfast.svg'),
(N'Frozen Foods', N'back-end/svg/frozen.svg'),
(N'Milk & Dairies', N'back-end/svg/milk.svg'),
(N'Pet Food', N'back-end/svg/pet.svg');



INSERT INTO Product (name, category_id, price, expiration_date, product_type, quantity, processing_time, farmer_id)
VALUES
(N'Carrot Fresh', 1, 15000, '2025-12-31', 'raw', 200, NULL, 2),

(N'Carrot Juice', 1, 35000, NULL, 'processed', 100, '2025-06-01', 2),

(N'Organic Tomato', 1, 18000, '2025-11-30', 'raw', 150, NULL, 2),
(N'Organic Cucumber', 1, 16000, '2025-12-01', 'raw', 9, NULL, 2),
(N'Spinach Fresh', 1, 14000, '2025-10-01', 'raw', 50, NULL, 2),
(N'Beef Jerky', 3, 90000, NULL, 'processed', 3, '2025-06-15', 2),
(N'Avocado Smoothie', 1, 45000, NULL, 'processed', 5, '2025-07-01', 2);
select * from product
INSERT INTO Farmer (user_id, full_name, phone, address)
VALUES (2, N'Trần Văn Nông', '0123456789', N'An Giang');

INSERT INTO ProductImage (product_id, image_url, description)
VALUES
(1, '/images/product/carrot1.png', N'Ảnh cà rốt tươi'),
(2, '/images/product/carrot-juice1.png', N'Ảnh nước ép cà rốt'),
(3, '/images/product/tomato1.png', N'Ảnh cà chua hữu cơ'),
(4, '/images/product/cucumber.png', N'Dưa leo hữu cơ'),
(5, '/images/product/spinach.png', N'Cải bó xôi tươi'),
(6, '/images/product/beefjerky.png', N'Thịt bò khô'),
(7, '/images/product/avocadosmoothie.png', N'Sinh tố bơ');
INSERT INTO RetailOrder (user_id, order_date, status, confirmed_at) VALUES
(2, '2024-07-01', 'pending', NULL),
(3, '2024-06-15', 'confirmed', '2024-06-16'),
(4, '2024-06-20', 'shipped', '2024-06-21'),
(5, '2024-06-10', 'delivered', '2024-06-11'),
(6, '2024-06-05', 'cancelled', NULL),
(2, '2024-06-25', 'confirmed', '2024-06-25'),
(3, '2024-06-28', 'confirmed', '2024-06-28');
select * from RetailOrderItem
INSERT INTO RetailOrderItem (order_id, product_id, quantity, unit_price) VALUES
(1, 3, 3, 15000), (1, 2, 2, 35000),
(2, 2, 1, 35000), (2, 3, 4, 18000),
(3, 3, 5, 15000),
(4, 3, 2, 18000), (4, 2, 1, 35000),
(5, 7, 10, 15000),
(6, 4, 5, 16000), (6, 5, 3, 14000),
(7, 6, 2, 90000), (7, 7, 4, 45000);

