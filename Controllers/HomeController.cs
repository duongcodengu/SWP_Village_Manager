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
            string inputEmail = email?.Trim().ToLower() ?? string.Empty;
            string inputPassword = password?.Trim() ?? string.Empty;

            // Tìm user theo email
            var user = _context.Users.FirstOrDefault(u => u.Email.ToLower() == inputEmail);

            if (user == null)
            {
                ViewBag.Error = "Email không tồn tại!";
                return View();
            }

            if (!user.IsActive)
            {
                ViewBag.Error = "Tài khoản đã bị khóa!";
                return View();
            }
            // Nếu là shipper thì bỏ qua kiểm tra password (chỉ để test đăng nhập)
            if (user.RoleId != 4)
            {
                if (user.Password != inputPassword)
                {
                    ViewBag.Error = "Mật khẩu không đúng!";
                    return View();
                }
            }

            // lấy tên role name
            int roleId = user.RoleId;
            string roleName = "";
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new SqlCommand("SELECT name FROM Roles WHERE id = @roleId", conn);
                cmd.Parameters.AddWithValue("@roleId", roleId);
                var result = cmd.ExecuteScalar();
                roleName = result?.ToString() ?? "";
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
                return RedirectToAction("DashboardShipper", "Shipper");
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
                return RedirectToAction("Index", "Home");
            }
            // role admin
            if (user.RoleId == 1)
            {
                return RedirectToAction("Index", "Home");
            }
            else if (user.RoleId == 3)
            {
                return RedirectToAction("IndexCustomer", "Customer");
            }
            // Nếu không khớp role nào thì về trang chủ
            return RedirectToAction("Index", "Home");
        }

        //Contact us
        [HttpGet]
        [Route("contact-us")]
        public IActionResult ContactUs() => View();

        [HttpPost]
        [Route("contact-us")]
        public async Task<IActionResult> ContactUs(string FirstName, string LastName, string Email, string PhoneNumber, string Message, DateTime CreatedAt)
        {
            var request = new ContactMessages
            {
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                PhoneNumber = PhoneNumber,
                Message = Message,
                CreatedAt = DateTime.Now
            };

            _context.ContactMessages.Add(request);

            var admins = await _context.Users
                .Where(u => u.RoleId == 1)
                .ToListAsync();

            string message = $"Có ticket cần sử lý từ {Email}";

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

            TempData["Success"] = "Yêu cầu đã được gửi. Chúng tôi sẽ liên hệ với bạn sớm nhất";
            return RedirectToAction("ContactUs");
        }

        // đăng ký
        [HttpGet]
        [Route("signup")]
        public IActionResult SignUp() => View();


        // Đăng xuất
        [Route("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
