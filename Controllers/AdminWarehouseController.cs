using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Village_Manager.Data;

namespace Village_Manager.Controllers
{
    public class AdminWarehouseController : Controller
    {
        private readonly DBContext _context;

        public AdminWarehouseController(DBContext context)
        {
            _context = context;
        }

        // kiem tra quyen truy cap
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

            // Lấy thông tin tổng số khách hàng từ cơ sở dữ liệu
            int totalCustomers = _context.Users.Count();
            ViewBag.TotalCustomers = totalCustomers;
            // lấy thông tin tổng số sản phẩm từ cơ sở dữ liệu
            int totalProducts = _context.Products.Count();
            ViewBag.TotalProducts = totalProducts;

            return View();
        }
    }
}
