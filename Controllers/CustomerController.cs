using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;

namespace Village_Manager.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public CustomerController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // Hiển thị thông tin cá nhân của khách hàng mua lẻ
        [HttpGet]
        [Route("customerinfo")]
        public IActionResult CustomerInformation()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var customer = _context.RetailCustomers
                                   .Include(rc => rc.User)
                                   .FirstOrDefault(rc => rc.UserId == userId);

            if (customer == null)
            {
                return NotFound("Không tìm thấy thông tin khách hàng.");
            }

            return View("CustomerInformation", customer);
        }
        [HttpGet]
        [Route("order")]
        public IActionResult Orders()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy danh sách đơn hàng của user đó
            var orders = _context.RetailOrders
                                 .Include(o => o.RetailOrderItems)
                                    .ThenInclude(oi => oi.Product)
                                 .Where(o => o.UserId == userId)
                                 .OrderByDescending(o => o.OrderDate)
                                 .ToList();

            return View("Orders", orders);
        }
    }
}
