using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Controllers
{
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
        [HttpGet]
        [Route("adminwarehouse")]
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
            int totalWholesaleOrders = _context.WholesaleOrders.Count();
            int totalOrders = totalRetailOrders + totalWholesaleOrders;
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

            // Bán buôn (Wholesale)
            var wholesaleRevenue = _context.WholesaleOrders
                .Where(wo => wo.Status == "confirmed"
                    && wo.ConfirmedAt.HasValue
                    && wo.ConfirmedAt.Value.Year == currentYear)
                .Join(_context.WholesaleOrderItems,
                      wo => wo.Id,
                      wi => wi.OrderId,
                      (wo, wi) => wi.Quantity * wi.UnitPrice)
                .Sum();

            decimal totalRevenue = (retailRevenue ?? 0) + (wholesaleRevenue ?? 0);
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

            var wholesaleOrders = (from wo in _context.WholesaleOrders
                                   join u in _context.Users on wo.UserId equals u.Id
                                   join woi in _context.WholesaleOrderItems on wo.Id equals woi.OrderId into woItems
                                   from woiGroup in woItems.DefaultIfEmpty()
                                   group woiGroup by new { wo.Id, u.Username, wo.OrderDate, wo.Status } into g
                                   select new
                                   {
                                       OrderId = g.Key.Id,
                                       Username = g.Key.Username,
                                       DatePlaced = g.Key.OrderDate,
                                       OrderStatus = g.Key.Status,
                                       TotalPrice = g.Sum(x => (x != null) ? x.Quantity * x.UnitPrice : 0),
                                       PaymentStatus = _context.Payments.Any(p =>
                                           p.OrderId == g.Key.Id &&
                                           p.OrderType == "wholesale" &&
                                           p.PaymentType == "receive") ? "Paid" : "Unpaid"
                                   });

            var latestOrders = retailOrders
                                .Concat(wholesaleOrders)
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
    }
}
