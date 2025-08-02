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
        public IActionResult GetPendingOrders(int page = 1, int pageSize = 5)
        {
            var query = _context.RetailOrders
                .Where(o => o.Status == "pending")
                .OrderByDescending(o => o.OrderDate);

            var totalOrders = query.Count();
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            var orders = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new {
                    o.Id,
                    o.OrderDate,
                    o.Status,
                    TotalAmount = _context.RetailOrderItems
                        .Where(i => i.OrderId == o.Id)
                        .Sum(i => i.Quantity * i.UnitPrice),
                    CustomerEmail = o.User.Email
                }).ToList();

            return Ok(new
            {
                orders,
                currentPage = page,
                totalPages
            });
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

        [HttpPost("update-status/{id}")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var order = await _context.RetailOrders.FindAsync(id);
            if (order == null) return NotFound();

            // Cập nhật trạng thái đơn hàng
            order.Status = request.Status;
            
            // Nếu trạng thái là cancelled, có thể thêm logic bổ sung
            if (request.Status == "cancelled")
            {
                // Có thể thêm logic hoàn trả số lượng sản phẩm vào kho
                var orderItems = await _context.RetailOrderItems
                    .Where(oi => oi.OrderId == id)
                    .Include(oi => oi.Product)
                    .ToListAsync();

                foreach (var item in orderItems)
                {
                    if (item.Product != null)
                    {
                        item.Product.Quantity += (int)item.Quantity;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true, message = $"Đã cập nhật trạng thái đơn hàng #{id} thành {request.Status}" });
        }

        public class UpdateStatusRequest
        {
            public string Status { get; set; }
        }
        [HttpGet("processed-orders")]
        public async Task<IActionResult> GetProcessedOrders(int page = 1, int pageSize = 10, string searchTerm = "")
        {
            var query = _context.RetailOrders
                .Include(o => o.User)
                .Where(o => o.Status != "pending");

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(o =>
                    o.User.Email.Contains(searchTerm) ||
                    o.Status.Contains(searchTerm) ||
                    o.Id.ToString().Contains(searchTerm));
            }

            var totalRecords = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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

            return Ok(new
            {
                TotalRecords = totalRecords,
                Page = page,
                PageSize = pageSize,
                Orders = orders
            });
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
