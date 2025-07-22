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

            if (userId == null)
            {
                return RedirectToAction("Login", "Home");
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
            var existing = await _context.FarmerRegistrationRequests
                .AnyAsync(r => r.UserId == userId && r.Status == "pending");

            if (existing)
            {
                TempData["Error"] = "Bạn đã gửi yêu cầu rồi. Vui lòng chờ xét duyệt.";
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
        public IActionResult CancelProduct(int productId, string reason)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return NotFound();

            product.ApprovalStatus = "rejected";
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

            return Ok(new { success = true });
        }

        [HttpPost]
        [Route("famer/resellproduct")]
        public IActionResult ResellProduct(int productId)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null) return NotFound();

            if (product.ApprovalStatus != "rejected")
                return BadRequest("Chỉ có thể bán lại sản phẩm đã bị hủy hoặc từ chối.");

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

            return Ok(new { success = true });
        }
    }
}
