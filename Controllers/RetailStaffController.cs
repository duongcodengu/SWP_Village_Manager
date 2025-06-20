using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;

namespace Village_Manager.Controllers
{
    public class retailStaffController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public retailStaffController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        // Hiển thị danh sách khách mua lẻ
        [HttpGet]
        [Route("allretailcustomers")]
        public IActionResult AllRetailCustomers()
        {
            var model = _context.RetailCustomers
                                .Include(rc => rc.User) 
                                .ToList();

            return View(model);
        }
    }
}
