using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using Village_Manager.Data;

namespace Village_Manager.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class BestSellingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BestSellingController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult GetBestSellingProducts([FromQuery] int range = 7)
        {
            if (range <= 0)
                return BadRequest("Range must be a positive number of days.");

            DateTime fromDate = DateTime.Today.AddDays(-range);

            // Step 1: Tính tổng số lượng đã bán theo ProductId
            var retailSales = from ro in _context.RetailOrders
                              join roi in _context.RetailOrderItems on ro.Id equals roi.OrderId
                              where ro.ConfirmedAt >= fromDate  
                              group roi by roi.ProductId into g
                              select new { ProductId = g.Key, Qty = g.Sum(x => x.Quantity) };

            // Step 2: bán lẻ 
            var totalSales = retailSales
                .GroupBy(x => x.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    Qty = g.Sum(x => x.Qty)
                });

            // Step 3: Join với Product để lấy thông tin
            var result = (from sale in totalSales
                          join p in _context.Products.Include(p => p.ProductImages)
                              on sale.ProductId equals p.Id
                          where sale.Qty > 0
                          orderby sale.Qty descending
                          select new
                          {
                              ProductName = p.Name,
                              p.Price,
                              ImageUrl = p.ProductImages.Select(i => i.ImageUrl).FirstOrDefault(),
                              TotalOrders = sale.Qty
                          })
                         .Take(10)
                         .ToList();

            return Ok(result);
        }
    }
}
