    using Microsoft.AspNetCore.Mvc;
    using Village_Manager.Data;
    using System;
    using System.Linq;

    namespace Village_Manager.Controllers.api
    {
        [ApiController]
        [Route("api")]
        public class GetMonthlyOrderChartController : ControllerBase
        {
            private readonly AppDbContext _context;
            public GetMonthlyOrderChartController(AppDbContext context)
            {
                _context = context;
            }

            [HttpGet("monthly-orders")]
            public IActionResult GetMonthlyOrderChart()
            {
                var year = DateTime.Now.Year;
                var months = Enumerable.Range(1, 12).ToList();

                // Đếm đơn bán buôn/tháng (kiểm tra HasValue trước khi lấy Value.Year/Month)
                var wholesaleData = months
                    .Select(m => _context.WholesaleOrders
                        .Count(o =>
                            o.OrderDate.HasValue &&
                            o.OrderDate.Value.Year == year &&
                            o.OrderDate.Value.Month == m))
                    .ToList();

                // Đếm đơn bán lẻ/tháng
                var retailData = months
                    .Select(m => _context.RetailOrders
                        .Count(o =>
                            o.OrderDate.HasValue &&
                            o.OrderDate.Value.Year == year &&
                            o.OrderDate.Value.Month == m))
                    .ToList();

                // Tháng hiển thị
                var categories = new[]
                {
                    "Jan","Feb","Mar","Apr","May","Jun",
                    "Jul","Aug","Sep","Oct","Nov","Dec"
                };

                return Ok(new
                {
                    wholesaleData,
                    retailData,
                    categories
                });
            }
        }
    }
