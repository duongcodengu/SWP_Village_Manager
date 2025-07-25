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

        public IActionResult GetBestSellingProducts([FromQuery] int range = 1)
        {
            if (range != 1 && range != 7 && range != 30)
                return BadRequest("Invalid range. Must be 1, 7, or 30.");

            var fromDate = DateTime.Today.AddDays(-range + 1); // ví dụ: 7 -> từ 7 ngày trước tới hôm nay

            var bestSelling = (from order in _context.RetailOrders
                               where order.Status == "delivered"
                                     && order.ConfirmedAt.HasValue
                                     && order.ConfirmedAt.Value.Date >= fromDate
                               join item in _context.RetailOrderItems on order.Id equals item.OrderId
                               group item by item.ProductId into g
                               orderby g.Sum(x => x.Quantity) descending
                               select new
                               {
                                   ProductId = g.Key,
                                   TotalSold = g.Sum(x => x.Quantity)
                               })
                               .Take(5)
                               .Join(_context.Products.Include(p => p.ProductImages),
                                     sale => sale.ProductId,
                                     p => p.Id,
                                     (sale, p) => new
                                     {
                                         ProductId = p.Id,
                                         ProductName = p.Name,
                                         Price = p.Price,
                                         ImageUrl = p.ProductImages.Select(i => i.ImageUrl).FirstOrDefault(),
                                         Qty = sale.TotalSold
                                     })
                               .ToList();
            return Ok(bestSelling);
        }
    }
}
