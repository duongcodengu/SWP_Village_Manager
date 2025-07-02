using Microsoft.AspNetCore.Mvc;
using Village_Manager.Data;

namespace Village_Manager.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class FarmerSearchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FarmerSearchController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Get(string term)
        {
            var farmers = _context.Farmers
                .Where(f => f.FullName.Contains(term))
                .Select(f => new
                {
                    label = f.FullName,
                    value = f.FullName,
                    id = f.Id
                })
                .Take(10)
                .ToList();

            return Ok(farmers);
        }
    }
}
