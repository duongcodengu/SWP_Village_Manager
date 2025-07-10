using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;

namespace Village_Manager.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashBoardRetailController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DashBoardRetailController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var totalRevenue = await _context.RetailOrderItems
                .SumAsync(x => (decimal?)(x.Quantity * x.UnitPrice)) ?? 0;

            var totalOrders = await _context.RetailOrders.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var totalCustomers = await _context.Users.CountAsync(u => u.RoleId == 3);

            return Ok(new
            {
                totalRevenue,
                totalOrders,
                totalProducts,
                totalCustomers
            });
        }


        [HttpGet("recent-sold")]
        
        public async Task<IActionResult> GetRecentOrders()
        {
            var orders = await _context.RetailOrders
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new
                {
                    OrderId = o.Id,
                    CustomerEmail = o.User.Email,
                    DatePlaced = o.OrderDate != null ? o.OrderDate.Value.ToString("yyyy-MM-dd") : "N/A",
                    Status = o.Status,
                    TotalAmount = _context.RetailOrderItems
                        .Where(i => i.OrderId == o.Id)
                        .Sum(i => i.Quantity * i.UnitPrice)
                })
                .ToListAsync();

            return Ok(orders);
        }


        [HttpGet("order-status-summary")]
        public async Task<IActionResult> GetOrderStatusSummary()
        {
            var result = await _context.RetailOrders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            return Ok(result);
        }
        [HttpGet("low-stock-products")]
        public async Task<IActionResult> GetLowStockProducts()
        {
            var result = await _context.Products
                .OrderBy(p => p.Quantity)
                .Select(p => new
                {
                    p.Name,
                    p.Quantity,
                    IsLow = p.Quantity < 10
                })
                .Take(10) // chỉ hiển thị 10 sản phẩm ít nhất
                .ToListAsync();

            return Ok(result);
        }

    }
}
