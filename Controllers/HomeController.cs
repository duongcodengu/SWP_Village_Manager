using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DBContext _context;

        public HomeController(ILogger<HomeController> logger, DBContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index() => View();

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

            if (user != null)
            {
                // session
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetInt32("RoleId", user.RoleId);

                // role admin
                if (user.RoleId == 1)
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
