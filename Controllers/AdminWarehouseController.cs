using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Village_Manager.Data;

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
                }).ToList<dynamic>();
            ViewBag.Categories = categories;

            // Tổng doanh thu delivered
            decimal totalRevenue = 0;
            // RetailOrder
            var retailRevenue = (from ro in _context.RetailOrders
                                 where ro.Status == "delivered"
                                 join ri in _context.RetailOrderItems on ro.Id equals ri.OrderId
                                 select ri.Quantity * ri.UnitPrice).Sum();
            // WholesaleOrder
            var wholesaleRevenue = (from wo in _context.WholesaleOrders
                                    where wo.Status == "delivered"
                                    join wi in _context.WholesaleOrderItems on wo.Id equals wi.OrderId
                                    select wi.Quantity * wi.UnitPrice).Sum();

            totalRevenue = (retailRevenue ?? 0) + (wholesaleRevenue ?? 0);
            ViewBag.TotalRevenue = totalRevenue;

            return View();
        }

        // Show all users in the data
        [HttpGet]
        [Route("alluser")]
        public IActionResult AllUser()
        {

            var users = _context.Users.ToList();
            return View(users); // Truyền danh sách User sang View
        }

        // delete user by id
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("AdminWarehouse/Delete/{id}")]
        public IActionResult Delete(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound(); // return 404 if user not found
            }

            _context.Users.Remove(user);
            _context.SaveChanges();

            return RedirectToAction("AllUser"); // Chuyển hướng về danh sách người dùng
        }
    }
}
