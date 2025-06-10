using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Village_Manager.Data;

namespace Village_Manager.Controllers
{
    public class AdminWarehouseController : Controller
    {
        private readonly DBContext _context;
        private readonly IConfiguration _configuration;


        public AdminWarehouseController(DBContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // kiem tra quyen truy cap
        [HttpGet]
        [Route("adminwarehouse")]
        public IActionResult Dashboard()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            var username = HttpContext.Session.GetString("Username");
            var roleId = HttpContext.Session.GetInt32("RoleId");
            

            if (string.IsNullOrEmpty(username) || roleId != 1)
            {
                Response.StatusCode = 404;
                return View("404");
            }

            // Lấy thông tin tổng số khách hàng từ cơ sở dữ liệu
            int totalCustomers = _context.Users.Count();
            ViewBag.TotalCustomers = totalCustomers;
            // lấy thông tin tổng số sản phẩm từ cơ sở dữ liệu
            int totalProducts = _context.Products.Count();
            ViewBag.TotalProducts = totalProducts;

            // Lấy thông tin tổng số đơn hàng bán lẻ từ cơ sở dữ liệu
            int totalRetailOrders = _context.RetailOrders.Count();
            int totalWholesaleOrders = _context.WholesaleOrders.Count();
            int totalOrders = totalRetailOrders + totalWholesaleOrders;
            ViewBag.TotalOrders = totalOrders;

            // lấy category
            var categories = new List<dynamic>();
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT name, image_url FROM ProductCategory", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Sử dụng anonymous object
                        categories.Add(new
                        {
                            Name = reader.GetString(0),
                            ImageUrl = reader.GetString(1)
                        });
                    }
                }
            }
            ViewBag.Categories = categories;

            // lấy total revenue
            decimal totalRevenue = 0;
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand(@"
                    SELECT 
                        (ISNULL(
                            (SELECT SUM(ri.quantity * ri.unit_price)
                             FROM RetailOrder ro
                             JOIN RetailOrderItem ri ON ro.id = ri.order_id
                             WHERE ro.status = 'delivered'), 0)
                         +
                         ISNULL(
                            (SELECT SUM(wi.quantity * wi.unit_price)
                             FROM WholesaleOrder wo
                             JOIN WholesaleOrderItem wi ON wo.id = wi.order_id
                             WHERE wo.status = 'delivered'), 0)
                        ) AS TotalRevenue", conn);
                var result = cmd.ExecuteScalar();
                if (result != DBNull.Value)
                {
                    totalRevenue = Convert.ToDecimal(result);
                }
            }
            ViewBag.TotalRevenue = totalRevenue;
            return View();
        }

        [HttpGet]
        [Route("products")]
        public IActionResult Products() => View();

        [HttpGet]
        [Route("alluser")]
        public IActionResult AllUser() => View();
    }
}
