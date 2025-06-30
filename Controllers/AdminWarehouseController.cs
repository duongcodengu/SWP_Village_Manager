using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Controllers
{
    [Route("adminwarehouse")]
    public class AdminWarehouseController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminWarehouseController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // kiểm tra quyền truy cập
        [HttpGet("")]
     
        public IActionResult Dashboard()
        {
            var username = HttpContext.Session.GetString("Username");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (string.IsNullOrEmpty(username) || roleId != 1)
            {
                Response.StatusCode = 404;
                return View("404");
            }

            // Tổng số khách hàng
            int totalCustomers = _context.Users.Count();
            ViewBag.TotalCustomers = totalCustomers;

            // Tổng số sản phẩm
            int totalProducts = _context.Products.Count();
            ViewBag.TotalProducts = totalProducts;

            // Tổng số đơn hàng
            int totalRetailOrders = _context.RetailOrders.Count();
            int totalOrders = totalRetailOrders;
            ViewBag.TotalOrders = totalOrders;

            // Lấy category (name, image_url)
            var categories = _context.ProductCategory
                .Select(c => new
                {
                    Name = c.Name,
                    ImageUrl = c.ImageUrl
                }).ToList<dynamic>();
            ViewBag.Categories = categories;

            // Tổng doanh thu confirmed
            decimal currentYear = DateTime.Now.Year;

            // Bán lẻ (Retail)
            var retailRevenue = _context.RetailOrders
                .Where(ro => ro.Status == "confirmed"
                    && ro.ConfirmedAt.HasValue
                    && ro.ConfirmedAt.Value.Year == currentYear)
                .Join(_context.RetailOrderItems,
                      ro => ro.Id,
                      ri => ri.OrderId,
                      (ro, ri) => ri.Quantity * ri.UnitPrice)
                .Sum();

            decimal totalRevenue = (retailRevenue ?? 0);
            ViewBag.TotalRevenue = totalRevenue;

            // last 4 đơn hàng
            var retailOrders = (from ro in _context.RetailOrders
                                join u in _context.Users on ro.UserId equals u.Id
                                join roi in _context.RetailOrderItems on ro.Id equals roi.OrderId into roItems
                                from roiGroup in roItems.DefaultIfEmpty()
                                group roiGroup by new { ro.Id, u.Username, ro.OrderDate, ro.Status } into g
                                select new
                                {
                                    OrderId = g.Key.Id,
                                    Username = g.Key.Username,
                                    DatePlaced = g.Key.OrderDate,
                                    OrderStatus = g.Key.Status,
                                    TotalPrice = g.Sum(x => (x != null) ? x.Quantity * x.UnitPrice : 0),
                                    PaymentStatus = _context.Payments.Any(p =>
                                        p.OrderId == g.Key.Id &&
                                        p.OrderType == "retail" &&
                                        p.PaymentType == "receive") ? "Paid" : "Unpaid"
                                });

            var latestOrders = retailOrders
                                .OrderByDescending(x => x.DatePlaced)
                                .Take(4)
                                .ToList();

            ViewBag.LatestOrders = latestOrders.ToList();

            return View();
        }
      
        
       
    
        
        [HttpGet]
        [Route("products")]
        public IActionResult Products() => View();

        [HttpGet]
        [Route("alluser")]
        public IActionResult AllUser() => View();


        // Lấy danh sách farmer
        [HttpGet]
        [Route("famer")]
        public IActionResult Famer()
        {
            var farmers = (from f in _context.Farmers
                           join u in _context.Users on f.UserId equals u.Id
                           join r in _context.Roles on u.RoleId equals r.Id
                           where r.Name == "farmer"
                           select new
                           {
                               FarmerId = f.Id,
                               FarmerName = f.FullName,
                               FarmerPhone = f.Phone,
                               FarmerAddress = f.Address,
                               Username = u.Username,
                               Email = u.Email,
                               CreatedAt = u.CreatedAt,
                               UserId = u.Id
                           }).ToList();

            ViewBag.Farmers = farmers.ToList();

            return View();
        }

        //update farmer thông tin
        [HttpPost]
        [Route("famer/update")]
        public IActionResult UpdateFarmer(int FarmerId, string FarmerName, string FarmerPhone, string FarmerAddress)
        {
            var farmer = _context.Farmers.FirstOrDefault(f => f.Id == FarmerId);
            if (farmer != null)
            {
                farmer.FullName = FarmerName;
                farmer.Phone = FarmerPhone;
                farmer.Address = FarmerAddress;

                _context.SaveChanges();
            }

            return RedirectToAction("Famer");
        }

        // xóa role farmer
        [HttpPost]
        [Route("famer/change-role")]
        public IActionResult ChangeRole(int UserId)
        {
            // retail_customer = role_id 5
            int newRoleId = 5;

            var user = _context.Users.FirstOrDefault(u => u.Id == UserId);
            if (user != null)
            {
                user.RoleId = newRoleId;
                _context.SaveChanges();
            }

            return RedirectToAction("Famer");
        }
        [HttpGet]
        [Route("addfamer")]
        public IActionResult AddFamer()
        {
            var pending = _context.FarmerRegistrationRequest
                .Where(r => r.status == "pending")
                .OrderByDescending(r => r.requested_at)
                .ToList();

            return View(pending);
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var request = await _context.FarmerRegistrationRequest.FindAsync(id);

            if (request == null || request.status != "pending")
                return NotFound();

            request.status = "approved";
            request.reviewed_at = DateTime.Now;
            request.reviewed_by = HttpContext.Session.GetInt32("UserId");

            // Tạo bản ghi mới trong bảng Farmers
            _context.Farmers.Add(new Farmer
            {
                UserId = request.user_id,
                FullName = request.full_name,
                Phone = request.phone,
                Address = request.address
            });

            var user = await _context.Users.FindAsync(request.user_id);
            if (user != null)
            {
                user.RoleId = 5;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("AddFamer");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var request = await _context.FarmerRegistrationRequest.FindAsync(id);

            if (request == null || request.status != "pending")
                return NotFound();

            request.status = "rejected";
            request.reviewed_at = DateTime.Now;
            request.reviewed_by = HttpContext.Session.GetInt32("UserId");

            await _context.SaveChangesAsync();
            return RedirectToAction("AddFamer");
        }
    }
}
