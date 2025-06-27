using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Controllers
{
    public class FamerController : Controller
    {
        private readonly AppDbContext _context;
        public FamerController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Route("becomefamer")]
        public IActionResult FamerBecome() => View();

        [HttpPost]
        [Route("becomefamer")]
        public async Task<IActionResult> FamerBecome(string FullName, string Phone, string AddressDetail, string Address)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra nếu đã gửi yêu cầu rồi
            var existing = await _context.FarmerRegistrationRequest
                .AnyAsync(r => r.user_id == userId && r.status == "pending");

            if (existing)
            {
                TempData["Error"] = "Bạn đã gửi yêu cầu rồi. Vui lòng chờ xét duyệt.";
                return RedirectToAction("FamerBecome");
            }

            // Lưu yêu cầu
            var request = new FarmerRegistrationRequest
            {
                user_id = userId.Value,
                full_name = FullName,
                phone = Phone,
                address = Address,
                status = "pending",
                requested_at = DateTime.Now
            };

            _context.FarmerRegistrationRequest.Add(request);

            // Gửi thông báo đến tất cả admin
            var admins = await _context.Users
                .Where(u => u.RoleId == 1)
                .ToListAsync();

            string message = $"Tài khoản ID {userId} đã gửi yêu cầu đăng ký làm nông dân.";

            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Content = message,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Yêu cầu đã được gửi. Vui lòng chờ xét duyệt.";
            return RedirectToAction("FamerBecome");
        }
    }
}
