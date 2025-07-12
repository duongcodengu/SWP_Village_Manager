using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;

namespace Village_Manager.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderRetailApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public OrderRetailApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("pending-orders")]
        public async Task<IActionResult> GetPendingOrders()
        {
            var orders = await _context.RetailOrders
                .Where(o => o.Status == "pending")
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.Id,
                    o.OrderDate,
                    o.Status,
                    CustomerEmail = o.User.Email,
                    TotalAmount = _context.RetailOrderItems
                        .Where(i => i.OrderId == o.Id)
                        .Sum(i => i.Quantity * i.UnitPrice)
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpPost("accept/{id}")]
        public async Task<IActionResult> AcceptOrder(int id)
        {
            var order = await _context.RetailOrders.FindAsync(id);
            if (order == null || order.Status != "pending") return NotFound();

            order.Status = "confirmed";
            order.ConfirmedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost("cancel/{id}")]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var order = await _context.RetailOrders.FindAsync(id);
            if (order == null || order.Status != "pending") return NotFound();

            order.Status = "cancelled";
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }
        [HttpGet("processed-orders")]
        public async Task<IActionResult> GetProcessedOrders()
        {
            var orders = await _context.RetailOrders
                .Include(o => o.User)
                .Where(o => o.Status != "pending")
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.Id,
                    o.Status,
                    Date = o.OrderDate,
                    CustomerEmail = o.User.Email,
                    TotalAmount = _context.RetailOrderItems
                        .Where(i => i.OrderId == o.Id)
                        .Sum(i => i.Quantity * i.UnitPrice)
                })
                .ToListAsync();

            return Ok(orders);
        }

        // search
        [HttpGet("searchbynamestatuspending")]
        public async Task<IActionResult> SearchPendingOrdersByName(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return BadRequest("Keyword is required.");

            var orders = await _context.RetailOrders
                .Include(o => o.User)
                .Where(o => o.Status == "pending" &&
                            (o.User.Username.Contains(keyword) || o.User.Email.Contains(keyword)))
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    o.Id,
                    o.OrderDate,
                    o.Status,
                    CustomerEmail = o.User.Email,
                    TotalAmount = _context.RetailOrderItems
                        .Where(i => i.OrderId == o.Id)
                        .Sum(i => i.Quantity * i.UnitPrice)
                })
                .ToListAsync();

            return Ok(orders);
        }
    }
}
