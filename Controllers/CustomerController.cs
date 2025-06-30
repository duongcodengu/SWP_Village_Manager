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
            return View();
        }
        [HttpGet]
        [Route("customer")]
        public IActionResult IndexCustomer()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return Redirect("/login");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            ViewBag.UserId = userId;
            ViewBag.HasAcceptedGeo = user?.HasAcceptedGeolocation ?? false;

            return View();
        }
    }
}
