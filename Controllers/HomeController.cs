using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using Village_Manager.Models;

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

            var user = _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
            string connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

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

                // session
                HttpContext.Session.SetInt32("UserId", user.Id);
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetInt32("RoleId", user.RoleId);
                HttpContext.Session.SetString("RoleName", roleName ?? "");

                // role admin
                if (user.RoleId == 1 || user.RoleId == 3)
                {
                    return RedirectToAction("Index", "Home");
                }

            }
            ViewBag.Error = "Email hoặc mật khẩu không đúng!";
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
