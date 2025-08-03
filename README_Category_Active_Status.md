# Chức năng Active/Inactive cho Category

## Tổng quan
Đã hoàn thiện chức năng quản lý trạng thái active/inactive cho ProductCategory trong hệ thống Village Manager.

## Các thay đổi đã thực hiện

### 1. Model và Database
- ✅ `ProductCategory` model đã có thuộc tính `Active` (bool)
- ✅ Database đã có cột `active` với giá trị mặc định là 1 (true)

### 2. Controller Updates
- ✅ **CategoryController**: Thêm action `ToggleActive` để toggle trạng thái
- ✅ **CategoryController**: Cập nhật các action hiện có để xử lý trạng thái active
- ✅ **AdminWarehouseController**: Chỉ hiển thị category active trong dropdown
- ✅ **ShopController**: Chỉ hiển thị category active cho khách hàng
- ✅ **HomeController**: Chỉ hiển thị category active ở trang chủ
- ✅ **FamerController**: Chỉ hiển thị category active cho farmer
- ✅ **MediaController**: Chỉ hiển thị category active trong media management
- ✅ **CategoryMenuViewComponent**: Chỉ hiển thị category active trong menu

### 3. View Updates
- ✅ **Listcate.cshtml**: 
  - Thêm cột "Trạng thái" hiển thị badge active/inactive
  - Thêm nút toggle active/inactive với icon phù hợp
  - Row có background khác nhau cho category inactive
  - JavaScript để xử lý toggle với confirmation
- ✅ **EditCategory.cshtml**: 
  - Thêm switch toggle để chỉnh sửa trạng thái
  - Badge hiển thị trạng thái hiện tại
  - JavaScript để cập nhật badge khi toggle
- ✅ **addCategory.cshtml**: 
  - Thêm switch toggle với mặc định là active
  - Badge hiển thị trạng thái
  - JavaScript để cập nhật badge khi toggle

### 4. ViewModel Updates
- ✅ **CategoryStatsViewModel**: Thêm thuộc tính `Active`

## Tính năng chính

### 1. Hiển thị trạng thái
- Category active: Badge xanh "Hoạt động" với icon check-circle
- Category inactive: Badge đỏ "Dừng hoạt động" với icon x-circle
- Row có background màu xám cho category inactive

### 2. Toggle trạng thái
- Nút toggle với icon phù hợp (pause/play)
- Confirmation dialog trước khi thực hiện
- Kiểm tra ràng buộc: Không thể dừng hoạt động category có sản phẩm đang active
- AJAX call để toggle không reload trang
- Thông báo kết quả cho user

### 3. Ràng buộc nghiệp vụ
- Category inactive sẽ không hiển thị cho khách hàng
- Category inactive sẽ không hiển thị trong menu
- Category inactive sẽ không hiển thị trong dropdown khi tạo/sửa sản phẩm
- Không thể dừng hoạt động category có sản phẩm đang active

### 4. Mặc định
- Category mới tạo sẽ có trạng thái active = true
- Database có giá trị mặc định active = 1

## Cách sử dụng

### 1. Xem danh sách category
- Truy cập: `/category/listcate`
- Xem trạng thái trong cột "Trạng thái"
- Category inactive có background màu xám

### 2. Toggle trạng thái
- Click nút toggle (pause/play) trong cột "Hành động"
- Xác nhận trong dialog
- Trạng thái sẽ được cập nhật ngay lập tức

### 3. Chỉnh sửa category
- Click nút edit (icon edit)
- Toggle switch trong form để thay đổi trạng thái
- Lưu để áp dụng thay đổi

### 4. Tạo category mới
- Truy cập: `/category/addCategory`
- Switch mặc định là ON (active)
- Có thể toggle để set inactive nếu cần

## API Endpoints

### Toggle Active Status
```
POST /category/toggleActive/{id}
```
Response:
```json
{
  "success": true,
  "active": true,
  "message": "Đã kích hoạt danh mục"
}
```

## Lưu ý
- Category inactive sẽ không hiển thị ở bất kỳ đâu cho khách hàng
- Chỉ admin mới có thể quản lý trạng thái active/inactive
- Có validation để đảm bảo không dừng hoạt động category có sản phẩm active
- Tất cả các controller đã được cập nhật để chỉ hiển thị category active 