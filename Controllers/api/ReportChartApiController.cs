using Microsoft.AspNetCore.Mvc;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Controllers.api
{
    [ApiController]
    [Route("api/[controller]")]
    public class report_chart : ControllerBase
    {
        private readonly AppDbContext _context;
        public report_chart(AppDbContext context)
        {
            _context = context;
        }

        // RetailOrder, RetailOrderItem và
        [HttpGet("total-revenue-by-month")]
        public IActionResult TotalRevenueByMonth([FromQuery] int year = 2024)
        {
            // BÁN LẺ: Lấy doanh thu theo tháng từ bảng Payment
            var retailQuery = from payment in _context.Payments
                              where payment.OrderType == "retail"
                                    && payment.PaymentType == "receive"
                                    && payment.PaidAt.HasValue
                                    && payment.PaidAt.Value.Year == year
                              group payment by payment.PaidAt.Value.Month into g
                              select new
                              {
                                  Month = g.Key,
                                  TotalRevenue = g.Sum(x => x.Amount)
                              };

            // Tạo danh sách 12 tháng, nếu tháng nào không có thì gán doanh thu = 0
            var months = Enumerable.Range(1, 12).ToList();

            var chartData = months.Select(m =>
                retailQuery.FirstOrDefault(x => x.Month == m)?.TotalRevenue ?? 0
            ).ToList();

            return Ok(new
            {
                categories = months.Select(m => new DateTime(year, m, 1).ToString("MMM")).ToList(),
                series = new[]
                {
            new { name = "Tổng doanh thu", data = chartData }
        }
            });
        }
    }
}
