using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Controllers
{
    public class RetailOrderController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RetailOrderController(AppDbContext context)
        {
            _context = context;
        }
        
    }
}
