using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using System.Linq;

namespace Village_Manager.Controllers.api
{
    [Route("api/discountcode")]
    [ApiController]
    public class DiscountCodesApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DiscountCodesApiController(AppDbContext context)
        {
            _context = context;
        }

        // API áp dụng mã giảm giá với tổng đơn hàng
        [HttpGet("apply")]
        public IActionResult ApplyDiscount(string code, decimal total)
        {
            var now = DateTime.Now;

            var discount = _context.DiscountCodes
                .Where(c => c.Code == code
                            && c.Status == "active"
                            && (!c.ExpiredAt.HasValue || c.ExpiredAt > now)
                            && c.UsageLimit > 0)
                .FirstOrDefault();

            if (discount == null)
            {
                return NotFound(new { message = "Mã không hợp lệ hoặc đã hết hạn." });
            }

            // Tính số tiền được giảm
            var discountAmount = Math.Round(total * discount.DiscountPercent / 100, 2);
            var totalAfterDiscount = total - discountAmount;

            return Ok(new
            {
                message = "Áp dụng mã thành công.",
                code = discount.Code,
                discountPercent = discount.DiscountPercent,
                discountAmount,
                totalBeforeDiscount = total,
                totalAfterDiscount
            });
        }
    }
}
