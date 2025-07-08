using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using System.Linq;

namespace Village_Manager.Controllers.api
{
    [Route("api/famer")]
    [ApiController]
    public class FamerApiController : ControllerBase
    {
        private readonly AppDbContext _context;
        public FamerApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("products")] // GET: api/famer/products
        public IActionResult GetMyProducts()
        {
            int? farmerId = HttpContext.Session.GetInt32("FarmerId");
            if (farmerId == null)
                return Unauthorized();

            var products = _context.Products
                .Where(p => p.FarmerId == farmerId)
                .Include(p => p.ProductImages)
                .Select(p => new {
                    p.Id,
                    p.Name,
                    p.Price,
                    p.Quantity,
                    p.ApprovalStatus,
                    ImageUrl = p.ProductImages.FirstOrDefault().ImageUrl,
                    ImageDescription = p.ProductImages.FirstOrDefault().Description
                })
                .ToList();

            return Ok(products);
        }
    }
}
