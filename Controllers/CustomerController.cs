using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Village_Manager.Data;

namespace Village_Manager.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public CustomerController( AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("dashboard")]
        public IActionResult DashBoard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            var totalOrders = _context.RetailOrders.Count(o => o.UserId == userId);
            var pendingOrders = _context.RetailOrders.Count(o => o.UserId == userId && o.Status == "pending");
            var address = _context.Addresses
                .Where(a => a.UserId == userId)
                .Select(a => a.AddressLine)
                .FirstOrDefault();
            if (user != null)
            {
                ViewBag.Email = user.Email;
                ViewBag.Username = user.Username;
                ViewBag.TotalOrders = totalOrders;
                ViewBag.PendingOrders = pendingOrders;
                ViewBag.Address = address ?? "Chưa có địa chỉ";
                ViewBag.Phone = user.Phone ?? "Chưa có số điện thoại";
            }
            return View();
        }
    }
}
