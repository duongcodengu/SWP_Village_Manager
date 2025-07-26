using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Linq;
using Village_Manager.Data;
using Village_Manager.Models;
using Village_Manager.ViewModel;

namespace Village_Manager.Controllers
{
    public class FamerController : Controller
    {
        private readonly AppDbContext _context;
        public FamerController(AppDbContext context)
        {
            _context = context;
        }
 
        [HttpGet]
        [Route("becomefamer")]
        public IActionResult FamerBecome()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var RoleId = HttpContext.Session.GetInt32("RoleId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Home");
            }
            if (RoleId == 5)
            {
                return RedirectToAction("DashboardFamer");
            }
            return View();
        }

        [HttpPost]
        [Route("becomefamer")]
        public async Task<IActionResult> FamerBecome(string FullName, string Phone, string AddressDetail, string Address)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Home");
            }

            // Kiểm tra nếu đã gửi yêu cầu rồi
            var existingRequest = await _context.FarmerRegistrationRequests
                .AnyAsync(r => r.UserId == userId && r.Status == "pending");

            if (existingRequest)
            {
                TempData["Error"] = "Bạn đã gửi yêu cầu rồi. Vui lòng chờ xét duyệt.";
                return RedirectToAction("FamerBecome");
            }

            // Kiểm tra nếu số điện thoại đã tồn tại trong hệ thống
            var phoneExists = await _context.FarmerRegistrationRequests
                .AnyAsync(r => r.Phone == Phone);

            if (phoneExists)
            {
                TempData["Error"] = "Số điện thoại này đã được sử dụng để đăng ký.";
                return RedirectToAction("FamerBecome");
            }

            // Kiểm tra địa chỉ
            if (string.IsNullOrWhiteSpace(Address))
            {
                TempData["Error"] = $"Vui lòng nhập đầy đủ địa chỉ. Address nhận được: '{Address}'";
                return RedirectToAction("FamerBecome");
            }

            // Lưu yêu cầu
            var request = new FarmerRegistrationRequest
            {
                UserId = userId.Value,
                FullName = FullName,
                Phone = Phone,
                Address = Address,
                Status = "pending",
                RequestedAt = DateTime.Now
            };

            _context.FarmerRegistrationRequests.Add(request);

            // Gửi thông báo đến tất cả admin
            var admins = await _context.Users
                .Where(u => u.RoleId == 1)
                .ToListAsync();

            string message = $"Tài khoản ID {userId} đã gửi yêu cầu đăng ký làm nông dân.";

            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Content = message,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Yêu cầu đã được gửi. Vui lòng chờ xét duyệt.";
            return RedirectToAction("FamerBecome");
        }


        [HttpGet]
        [Route("dashboardfamer")]
        public IActionResult DashboardFamer()
        {
            var categories = _context.ProductCategories
                .Select(c => new { Id = c.Id, Name = c.Name })
                .ToList();

            int? userId = HttpContext.Session.GetInt32("UserId");
            int? farmerId = HttpContext.Session.GetInt32("FarmerId");

            if (userId == null || farmerId == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            var farmer = _context.Farmers.FirstOrDefault(f => f.Id == farmerId && f.UserId == userId);

            if (user == null || farmer == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var productList = _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.FarmerId == farmer.Id)
                .ToList();

            var productWithSalesList = productList.Select(p => new ProductWithSalesViewModel
            {
                Product = p,
                SoldQuantity = _context.RetailOrderItems
                    .Where(roi => roi.ProductId == p.Id)
                    .Sum(roi => (int?)roi.Quantity) ?? 0
            }).ToList();

            var productIds = productList.Select(p => p.Id).ToList();
            var ongoingStatuses = new[] { "pending", "confirmed", "shipped" };

            var ongoingOrders = _context.RetailOrders
                .Include(o => o.RetailOrderItems)
                    .ThenInclude(roi => roi.Product)
                .Where(o => o.RetailOrderItems.Any(roi => productIds.Contains((int)roi.ProductId))
                        && ongoingStatuses.Contains(o.Status))
                .ToList();

            var model = new FamerDashboardViewModel
            {
                User = user,
                Famer = farmer,
                ProductList = productList,
                ProductWithSalesList = productWithSalesList,
                OngoingOrders = ongoingOrders
            };

            ViewBag.Categories = categories;
            ViewBag.UserId = userId;
            ViewBag.FarmerId = farmerId;
            ViewBag.FarmerName = farmer.FullName;
            ViewBag.TotalProductTypes = productList.Count;
            ViewBag.TotalSold = productWithSalesList.Sum(p => p.SoldQuantity);
            ViewBag.TotalQuantityInStock = _context.Stocks
                .Where(s => productIds.Contains(s.Id))
                .Sum(s => (int?)s.Quantity) ?? 0;

            // Load danh sách yêu cầu cung cấp
            var supplyRequests = _context.SupplyRequests
                .Where(sr => sr.FarmerId == farmer.Id)
                .OrderByDescending(sr => sr.RequestedAt)
                .ToList();
            ViewBag.SupplyRequests = supplyRequests;

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(IFormCollection form, List<IFormFile> images)
        {
            int? farmerId = HttpContext.Session.GetInt32("FarmerId");
            if (farmerId == null)
                return RedirectToAction("Login", "Home");

            var product = new Product
            {
                Name = form["name"],
                ProductType = form["product_type"],
                CategoryId = int.Parse(form["category_id"]),
                Quantity = int.Parse(form["quantity"]),
                Price = decimal.Parse(form["price"]),
                ExpirationDate = string.IsNullOrWhiteSpace(form["expiration_date"]) ? null : DateTime.Parse(form["expiration_date"]),
                ProcessingTime = string.IsNullOrWhiteSpace(form["processing_time"]) ? null : DateTime.Parse(form["processing_time"]),
                FarmerId = farmerId.Value,
                ApprovalStatus = "pending"
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Xử lý upload ảnh
            if (images != null && images.Count > 0)
            {
                foreach (var file in images)
                {
                    if (file.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        var productImage = new ProductImage
                        {
                            ProductId = product.Id,
                            ImageUrl = "/uploads/" + uniqueFileName,
                            Description = form["image_description"],
                            UploadedAt = DateTime.Now
                        };

                        _context.ProductImages.Add(productImage);
                    }
                }
                await _context.SaveChangesAsync();
            }

            // Gửi thông báo cho admin
            var admins = _context.Users.Where(u => u.RoleId == 1).ToList();
            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Content = $"Sản phẩm mới '{product.Name}' của farmer ID {farmerId} cần duyệt.",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }
            await _context.SaveChangesAsync();

            TempData["Success"] = "Sản phẩm đã gửi chờ duyệt!";
            return RedirectToAction("DashboardFamer");
        }

        [HttpPost]
        [Route("famer/cancelproduct")]
        [ValidateAntiForgeryToken]
        public IActionResult CancelProduct(int productId, string reason)
        {
            int? farmerId = HttpContext.Session.GetInt32("FarmerId");
            if (farmerId == null)
                return RedirectToAction("Login", "Home");

            var product = _context.Products.FirstOrDefault(p => p.Id == productId && p.FarmerId == farmerId.Value);
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm hoặc bạn không có quyền hủy bán sản phẩm này.";
                return RedirectToAction("MyProducts");
            }

            if (product.ApprovalStatus != "accepted")
            {
                TempData["Error"] = "Chỉ có thể hủy bán sản phẩm đã được duyệt.";
                return RedirectToAction("MyProducts");
            }

            product.ApprovalStatus = "cancelled";
            _context.SaveChanges();

            // Gửi thông báo cho admin
            var admins = _context.Users.Where(u => u.RoleId == 1).ToList();
            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Content = $"Farmer đã hủy bán sản phẩm '{product.Name}'. Lý do: {reason}",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }
            _context.SaveChanges();

            TempData["Success"] = "Đã hủy bán sản phẩm thành công!";
            return RedirectToAction("MyProducts");
        }

        [HttpPost]
        [Route("famer/resellproduct")]
        [ValidateAntiForgeryToken]
        public IActionResult ResellProduct(int productId)
        {
            int? farmerId = HttpContext.Session.GetInt32("FarmerId");
            if (farmerId == null)
                return RedirectToAction("Login", "Home");

            var product = _context.Products.FirstOrDefault(p => p.Id == productId && p.FarmerId == farmerId.Value);
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm hoặc bạn không có quyền bán lại sản phẩm này.";
                return RedirectToAction("MyProducts");
            }

            if (product.ApprovalStatus != "rejected" && product.ApprovalStatus != "cancelled")
            {
                TempData["Error"] = "Chỉ có thể bán lại sản phẩm đã bị từ chối hoặc hủy bán.";
                return RedirectToAction("MyProducts");
            }

            product.ApprovalStatus = "pending";
            _context.SaveChanges();

            // Gửi thông báo cho admin
            var admins = _context.Users.Where(u => u.RoleId == 1).ToList();
            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Content = $"Farmer đã yêu cầu bán lại sản phẩm '{product.Name}'.",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }
            _context.SaveChanges();

            TempData["Success"] = "Đã gửi yêu cầu bán lại sản phẩm thành công!";
            return RedirectToAction("MyProducts");
        }

        // Yêu cầu cung cấp sản phẩm từ farmer đến admin
        [HttpPost]
        [Route("famer/requestsupply")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestSupplyAsync(int productId, int quantity, decimal? price, string? note)
        {
            int? farmerId = HttpContext.Session.GetInt32("FarmerId");
            int? userId = HttpContext.Session.GetInt32("UserId");
            
            if (farmerId == null || userId == null)
                return RedirectToAction("Login", "Home");

            // Kiểm tra sản phẩm có thuộc về farmer này không
            var product = _context.Products.FirstOrDefault(p => p.Id == productId && p.FarmerId == farmerId.Value);
            if (product == null)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm hoặc bạn không có quyền yêu cầu cung cấp sản phẩm này.";
                return RedirectToAction("DashboardFamer");
            }

            // Validation cho giá mới
            if (price.HasValue)
            {
                if (price.Value < 1000 || price.Value % 1000 != 0)
                {
                    TempData["Error"] = "Giá phải lớn hơn 1000 và là bội số của 1000.";
                    return RedirectToAction("DashboardFamer");
                }
            }

            // Tạo yêu cầu cung cấp
            var supplyRequest = new SupplyRequest
            {
                RequesterType = "farmer",
                RequesterId = userId.Value,
                ReceiverId = 1, // Admin ID
                FarmerId = farmerId.Value,
                ProductName = product.Name,
                Quantity = quantity,
                Price = price,
                Status = "pending",
                RequestedAt = DateTime.Now
            };

            _context.SupplyRequests.Add(supplyRequest);

            // Gửi thông báo cho admin
            var admins = _context.Users.Where(u => u.RoleId == 1).ToList();
            foreach (var admin in admins)
            {
                var content = price.HasValue 
                    ? $"Farmer yêu cầu cung cấp {quantity} {product.Name} với giá mới {price.Value:N0} VNĐ. Ghi chú: {note ?? "Không có"}"
                    : $"Farmer yêu cầu cung cấp {quantity} {product.Name}. Ghi chú: {note ?? "Không có"}";
                
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Content = content,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã gửi yêu cầu cung cấp sản phẩm thành công!";
            return RedirectToAction("DashboardFamer");
        }

        // Xem danh sách yêu cầu cung cấp của farmer
        [HttpGet]
        [Route("famer/supplyrequests")]
        public IActionResult SupplyRequests()
        {
            int? farmerId = HttpContext.Session.GetInt32("FarmerId");
            if (farmerId == null)
                return RedirectToAction("Login", "Home");

            var requests = _context.SupplyRequests
                .Where(sr => sr.FarmerId == farmerId.Value)
                .OrderByDescending(sr => sr.RequestedAt)
                .ToList();

            return View(requests);
        }

        // Farmer phản hồi yêu cầu cung cấp từ admin
        [HttpPost]
        [Route("famer/respondtosupply")]
        [ValidateAntiForgeryToken]
        public IActionResult RespondToSupply(int requestId, string response, string? note)
        {
            int? farmerId = HttpContext.Session.GetInt32("FarmerId");
            if (farmerId == null)
                return RedirectToAction("Login", "Home");

            var request = _context.SupplyRequests
                .FirstOrDefault(sr => sr.Id == requestId && sr.FarmerId == farmerId.Value);

            if (request == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu cung cấp.";
                return RedirectToAction("SupplyRequests");
            }

            if (request.Status != "pending")
            {
                TempData["Error"] = "Yêu cầu này đã được xử lý.";
                return RedirectToAction("SupplyRequests");
            }

            request.Status = response; // "accepted" hoặc "rejected"
            request.RespondedAt = DateTime.Now;

            // Nếu farmer chấp nhận, cập nhật sản phẩm
            if (response == "accepted")
            {
                var product = _context.Products
                    .FirstOrDefault(p => p.Name == request.ProductName && p.FarmerId == farmerId.Value);
                
                if (product != null)
                {
                    // Cộng thêm số lượng
                    product.Quantity += request.Quantity;
                    
                    // Cập nhật giá nếu có giá mới
                    if (request.Price.HasValue)
                    {
                        product.Price = request.Price.Value;
                    }
                }
            }

            // Gửi thông báo cho admin
            var admin = _context.Users.FirstOrDefault(u => u.Id == request.RequesterId);
            if (admin != null)
            {
                var content = response == "accepted" 
                    ? $"Farmer đã chấp nhận yêu cầu cung cấp {request.Quantity} {request.ProductName}. " +
                      (request.Price.HasValue ? $"Giá mới: {request.Price.Value:N0} VNĐ. " : "") +
                      $"Ghi chú: {note ?? "Không có"}"
                    : $"Farmer đã từ chối yêu cầu cung cấp {request.ProductName}. Ghi chú: {note ?? "Không có"}";
                
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Content = content,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }

            _context.SaveChanges();

            TempData["Success"] = $"Đã {response} yêu cầu cung cấp thành công!";
            return RedirectToAction("SupplyRequests");
        }
    }
}
