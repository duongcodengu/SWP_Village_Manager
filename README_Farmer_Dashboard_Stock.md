# 🚜 Farmer Dashboard - Stock Management Improvements

## 📋 Tổng quan

Đã cập nhật dashboard của farmer để hiển thị dữ liệu kho từ bảng **Product** thay vì bảng **Stock** (không dùng được), cung cấp thông tin chi tiết về số lượng tồn kho và thống kê bán hàng.

## ✨ Các cải tiến đã thực hiện

### 1. 🔄 **Cập nhật Controller (FamerController.cs)**

#### **Thay đổi cách tính tổng số lượng trong kho:**
```csharp
// Trước (sử dụng bảng Stock - không dùng được)
ViewBag.TotalQuantityInStock = _context.Stocks
    .Where(s => productIds.Contains(s.Id))
    .Sum(s => (int?)s.Quantity) ?? 0;

// Sau (sử dụng bảng Product)
ViewBag.TotalQuantityInStock = productList.Sum(p => p.Quantity);
```

#### **Cập nhật ProductWithSalesViewModel:**
```csharp
var productWithSalesList = productList.Select(p => new ProductWithSalesViewModel
{
    Product = p,
    SoldQuantity = _context.RetailOrderItems
        .Where(roi => roi.ProductId == p.Id)
        .Sum(roi => (int?)roi.Quantity) ?? 0,
    StockQuantity = p.Quantity // Lấy số lượng từ bảng Product
}).ToList();
```

### 2. 📊 **Cập nhật ViewModel (ProductWithSalesViewModel.cs)**

Thêm property mới để lưu trữ số lượng tồn kho:
```csharp
public class ProductWithSalesViewModel
{
    public Product Product { get; set; }
    public int SoldQuantity { get; set; }
    public int StockQuantity { get; set; } // Số lượng tồn kho từ bảng Product
}
```

### 3. 🎨 **Cập nhật View (DashboardFamer.cshtml)**

#### **A. Cập nhật bảng sản phẩm chính:**
- Thêm cột "Tồn kho" và "Đã bán"
- Hiển thị số lượng tồn kho từ trường `Quantity` của Product
- Format giá tiền với đơn vị VNĐ

#### **B. Thêm section "Thống kê kho hàng chi tiết":**
- Hiển thị từng sản phẩm với thông tin chi tiết
- Trạng thái kho hàng (Còn hàng/Sắp hết/Hết hàng)
- Progress bar hiển thị tỷ lệ tồn kho
- Màu sắc phân biệt trạng thái:
  - 🟢 **Xanh lá**: Còn hàng (>5)
  - 🟡 **Vàng**: Sắp hết (≤5)
  - 🔴 **Đỏ**: Hết hàng (=0)

#### **C. Cập nhật bảng sản phẩm trong tab "Sản phẩm":**
- Thêm cột "Tồn kho" với trạng thái màu sắc
- Hiển thị số lượng và trạng thái kho hàng

#### **D. CSS Enhancements:**
```css
/* CSS cho thống kê kho hàng */
.stock-item {
    background: #fff;
    transition: all 0.3s ease;
    border: 1px solid #e9ecef !important;
}
.stock-item:hover {
    transform: translateY(-2px);
    box-shadow: 0 4px 15px rgba(0,0,0,0.1);
    border-color: #0da487 !important;
}
```

## 📈 **Dữ liệu hiển thị**

### 1. **Thống kê tổng quan:**
- **Tổng loại sản phẩm**: Số lượng sản phẩm khác nhau
- **Tổng đã bán**: Tổng số lượng đã bán từ tất cả sản phẩm
- **Tổng trong kho**: Tổng số lượng tồn kho từ bảng Product

### 2. **Thông tin chi tiết từng sản phẩm:**
- **Tên sản phẩm** và **hình ảnh**
- **Giá bán** (format VNĐ)
- **Số lượng tồn kho** (từ Product.Quantity)
- **Số lượng đã bán** (từ RetailOrderItems)
- **Trạng thái kho hàng** với màu sắc phân biệt

### 3. **Trạng thái kho hàng:**
```csharp
var stockStatus = item.StockQuantity == 0 ? "Hết hàng" : 
                 item.StockQuantity <= 5 ? "Sắp hết" : "Còn hàng";
var statusClass = item.StockQuantity == 0 ? "text-danger" : 
                 item.StockQuantity <= 5 ? "text-warning" : "text-success";
```

## 🔧 **Cách hoạt động**

### 1. **Lấy dữ liệu từ bảng Product:**
```csharp
var productList = _context.Products
    .Include(p => p.ProductImages)
    .Where(p => p.FarmerId == farmer.Id)
    .ToList();
```

### 2. **Tính toán số lượng đã bán:**
```csharp
SoldQuantity = _context.RetailOrderItems
    .Where(roi => roi.ProductId == p.Id)
    .Sum(roi => (int?)roi.Quantity) ?? 0
```

### 3. **Lấy số lượng tồn kho:**
```csharp
StockQuantity = p.Quantity // Trực tiếp từ trường Quantity của Product
```

## 📊 **Lợi ích**

### 1. **Chính xác hơn:**
- Dữ liệu kho được lấy trực tiếp từ bảng Product
- Không phụ thuộc vào bảng Stock (không dùng được)
- Đảm bảo tính nhất quán của dữ liệu

### 2. **Trực quan hơn:**
- Hiển thị trạng thái kho hàng với màu sắc
- Progress bar cho tỷ lệ tồn kho
- Thống kê chi tiết từng sản phẩm

### 3. **Dễ quản lý:**
- Farmer có thể dễ dàng theo dõi kho hàng
- Cảnh báo khi sản phẩm sắp hết
- Thống kê bán hàng chi tiết

## 🎯 **Sử dụng**

### 1. **Xem thống kê tổng quan:**
- Dashboard hiển thị tổng số lượng trong kho
- Tổng số lượng đã bán
- Tổng loại sản phẩm

### 2. **Xem chi tiết từng sản phẩm:**
- Bảng sản phẩm với thông tin tồn kho
- Section thống kê kho hàng chi tiết
- Trạng thái và cảnh báo kho hàng

### 3. **Theo dõi trạng thái:**
- 🟢 **Còn hàng**: Sản phẩm có đủ số lượng
- 🟡 **Sắp hết**: Cần bổ sung hàng (≤5)
- 🔴 **Hết hàng**: Cần nhập hàng gấp

## 🔮 **Tính năng tương lai**

### 1. **Cảnh báo tự động:**
- Email/SMS khi sản phẩm sắp hết
- Thông báo khi hết hàng

### 2. **Báo cáo chi tiết:**
- Biểu đồ xu hướng bán hàng
- Dự báo nhu cầu
- Phân tích hiệu suất sản phẩm

### 3. **Quản lý kho nâng cao:**
- Lịch sử nhập/xuất kho
- Quản lý hạn sử dụng
- Tối ưu hóa tồn kho

## 📝 **Lưu ý**

1. **Đảm bảo trường Quantity trong bảng Product được cập nhật chính xác**
2. **Kiểm tra dữ liệu RetailOrderItems để tính toán số lượng đã bán**
3. **Backup database trước khi deploy thay đổi**

---

**Kết quả**: Dashboard của farmer giờ đây hiển thị đầy đủ thông tin kho hàng từ bảng Product, giúp farmer quản lý sản phẩm hiệu quả hơn! 🚜✨ 