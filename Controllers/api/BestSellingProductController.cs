using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;

namespace Village_Manager.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class BestSellingProductController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BestSellingProductController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("top-selling")]
        public async Task<IActionResult> GetTopSellingProducts(int top = 5)
        {
            var result = await _context.RetailOrderItems
                .Include(i => i.Product)
                .GroupBy(i => new { i.ProductId, i.Product.Name, i.Product.Price, i.Product.Quantity })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    Name = g.Key.Name,
                    Price = g.Key.Price,
                    Stock = g.Key.Quantity,
                    TotalSold = g.Sum(x => x.Quantity),
                    TotalRevenue = g.Sum(x => x.Quantity * x.UnitPrice),
                    Image = _context.ProductImages
                        .Where(p => p.ProductId == g.Key.ProductId)
                        .Select(p => p.ImageUrl)
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.TotalSold)
                .Take(top)
                .ToListAsync();

            return Ok(result);
        }
    }
}
