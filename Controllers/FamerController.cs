using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using System.Linq;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Controllers
{
    public class FamerController : Controller
    {
        private readonly AppDbContext _context;
        public FamerController(AppDbContext context)
        {
            _context = context;
        }
        // class custom model  để lưu trữ thông tin sản phẩm và số lượng sản phẩm đã bán
        public class ProductWithSales
        {
            public Product Product { get; set; }
            public int SoldQuantity { get; set; }
        }

            
        public class FamerDashboardView
        {
            public User User { get; set; }
            public Farmer Famer { get; set; }
            public List<Product> ProductList { get; set; } // Danh sách sản phẩm gốc
            public List<ProductWithSales> ProductWithSalesList { get; set; } // Danh sách sản phẩm kèm số lượng đã bán
            public List<RetailOrder> OngoingOrders { get; set; }

        }
        [HttpGet]
        [Route("dashboardfamer")]
        public IActionResult DashboardFamer()
        {
            // lấy thông tin từ session nếu không có quay lại đăng nhập
            var userId = HttpContext.Session.GetInt32("UserId");
            if(userId == null)
            {
                return RedirectToAction("Login", "Home");
            }
            // truy vấn thông tin user từ bảng User
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            // truy vấn thông tin famer từ bảng famer
            var farmer = _context.Farmers.FirstOrDefault(f => f.UserId == userId);
            // Truy vấn Prodcut trong bảng product và ảnh trong bảng prodcutImages của famer qua id famer
            var productList = _context.Products.Include(p => p.ProductImages).Where(p => p.FarmerId == farmer.Id).ToList();
            //Duyệt qua từng sản phẩm trong prodcutList
            var productWithSalesList = productList.Select(p => new ProductWithSales
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

            if (user == null || farmer == null)
            {
                return Content("Khong thay thong tin");
            }
            var model = new FamerDashboardView
            {
                User = user,
                Famer = farmer,
                ProductList = productList,
                ProductWithSalesList = productWithSalesList,
                OngoingOrders = ongoingOrders
            };
            return View(model);


        }

        [HttpGet]
        [Route("becomefamer")]
        public IActionResult FamerBecome() => View();

        [HttpPost]
        [Route("becomefamer")]
        public async Task<IActionResult> FamerBecome(string FullName, string Phone, string AddressDetail, string Address)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra nếu đã gửi yêu cầu rồi
            var existing = await _context.FarmerRegistrationRequest
                .AnyAsync(r => r.user_id == userId && r.status == "pending");

            if (existing)
            {
                TempData["Error"] = "Bạn đã gửi yêu cầu rồi. Vui lòng chờ xét duyệt.";
                return RedirectToAction("FamerBecome");
            }

            // Lưu yêu cầu
            var request = new FarmerRegistrationRequest
            {
                user_id = userId.Value,
                full_name = FullName,
                phone = Phone,
                address = Address,
                status = "pending",
                requested_at = DateTime.Now
            };

            _context.FarmerRegistrationRequest.Add(request);

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

        //[HttpGet]
        //[Route("dashboardfamer")]
        //public IActionResult DashboardFamer() => View();
    }
}
