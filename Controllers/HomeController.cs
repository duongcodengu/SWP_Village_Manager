using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using Village_Manager.Models;
//using Village_Manager.Extensions;

namespace Village_Manager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public HomeController(ILogger<HomeController> logger, AppDbContext context, IConfiguration configuration)
        {
            _logger = logger;
            _context = context;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            var categories = _context.ProductCategories.Select(c => new
            {
                c.Id,
                c.Name,
                c.ImageUrl

            })
                .ToList();
            ViewBag.ProductCategories = categories;
            return View();
           
        }

        //login
        [HttpGet]
        [Route("login")]
        public IActionResult Login() => View();

        // Xử lý đăng nhập
        [HttpPost]
        [Route("login")]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password && u.IsActive);
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            Console.WriteLine(connectionString);
            if (user != null)
            {
                // lấy tên role name
                int roleId = user.RoleId;
                string roleName = "";
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    var cmd = new SqlCommand("SELECT name FROM Roles WHERE id = @roleId", conn);
                    cmd.Parameters.AddWithValue("@roleId", roleId);

                    var result = cmd.ExecuteScalar();
                    roleName = result.ToString() ?? "";
                }

                // Xóa session cũ trước khi set mới
                HttpContext.Session.Clear();
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetInt32("RoleId", user.RoleId);
                HttpContext.Session.SetString("RoleName", roleName ?? "");

                // Set thêm ShipperId nếu là shipper
                if (user.RoleId == 4)
                {
                    var shipper = _context.Shippers.FirstOrDefault(s => s.UserId == user.Id);
                    if (shipper != null)
                    {
                        HttpContext.Session.SetInt32("ShipperId", shipper.Id);
                        HttpContext.Session.SetString("ShipperName", shipper.FullName ?? "");
                    }
                }
                // Set thêm FarmerId nếu là farmer
                if (user.RoleId == 5)
                {
                    var farmer = _context.Farmers.FirstOrDefault(f => f.UserId == user.Id);
                    if (farmer != null)
                    {
                        HttpContext.Session.SetInt32("FarmerId", farmer.Id);
                        HttpContext.Session.SetString("FarmerName", farmer.FullName ?? "");
                    }
                }

                // role admin
                if (user.RoleId == 1 || user.RoleId == 3 || user.RoleId == 5)
                // check role
                if (user.RoleId == 4)
                {
                    return RedirectToAction("DashboardShipper", "Shipper");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
                else if (user.RoleId == 3)
                {
                    return RedirectToAction("IndexCustomer", "Customer");
                }

            }
            ViewBag.Error = user == null ? "Tài khoản bị khóa hoặc thông tin không đúng!" : "Email hoặc mật khẩu không đúng!";

            return View();
        }





        // Đăng xuất
        [Route("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
