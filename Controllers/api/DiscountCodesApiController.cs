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

        [HttpGet("apply")]
        public async Task<IActionResult> ApplyDiscount(string code, decimal totalAmount)
        {
            var now = DateTime.Now;

            var discount = await _context.DiscountCodes
                .FirstOrDefaultAsync(c =>
                    c.Code == code &&
                    c.Status == "active" &&
                    (!c.ExpiredAt.HasValue || c.ExpiredAt > now) &&
                    c.UsageLimit > 0);

            if (discount == null)
                return NotFound(new { message = "Mã không hợp lệ hoặc đã hết hạn." });

            // Tính phần trăm và tiền giảm
            decimal percent = discount.DiscountPercent;
            decimal discountAmount = (percent / 100m) * totalAmount;
            decimal finalAmount = totalAmount - discountAmount;

            // ➕ Lưu thông tin vào Session để sang Checkout đọc
            HttpContext.Session.SetString("DiscountCode", discount.Code);
            HttpContext.Session.SetString("DiscountAmount", Math.Round(discountAmount, 0).ToString());

            // Trừ lượt dùng
            discount.UsageLimit -= 1;
            if (discount.UsageLimit == 0)
            {
                discount.Status = "used";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                code = discount.Code,
                discountPercent = percent,
                discountAmount = Math.Round(discountAmount, 0),
                finalAmount = Math.Round(finalAmount, 0),
                message = "Áp dụng mã thành công."
            });
        }
    }
}
