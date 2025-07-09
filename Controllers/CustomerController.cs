using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Village_Manager.Data;
using Village_Manager.Models;
using Village_Manager.ViewModel;

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
            var userLocations = _context.UserLocations
                                .Include(u1 => u1.User)
                                .ToList();
            return View(userLocations);
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
