using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Controllers
{
    public class ShipperController : Controller
    {
        private readonly AppDbContext _context;
        public ShipperController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        [Route("shipperbecome")]
        public IActionResult ShipperBecome() => View();

        [HttpPost]
        [Route("ShipperBecome")]
        public async Task<IActionResult> ShipperBecome(string FullName, string Phone, string AddressDetail, string Address, string vehicleInfor)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra nếu đã gửi yêu cầu rồi
            var existing = await _context.ShipperRegistrationRequests
                .AnyAsync(r => r.UserId == userId && r.Status == "pending");

            if (existing)
            {
                TempData["Error"] = "Bạn đã gửi yêu cầu rồi. Vui lòng chờ xét duyệt.";
                return RedirectToAction("shipperbecome");
            }

            // Lưu yêu cầu
            var request = new ShipperRegistrationRequest
            {
                UserId = userId.Value,
                FullName = FullName,
                Phone = Phone,
                Address = Address,
                Status = "pending",
                VehicleInfo = vehicleInfor,
                RequestedAt = DateTime.Now
            };

            _context.ShipperRegistrationRequests.Add(request);

            // Gửi thông báo đến tất cả admin
            var admins = await _context.Users
                .Where(u => u.RoleId == 1)
                .ToListAsync();

            string message = $"Tài khoản ID {userId} đã gửi yêu cầu đăng ký làm shipper.";

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
            return RedirectToAction("shipperbecome");
        }
        [HttpGet]
        [Route("dashboardshipper")]
        public IActionResult ShipperDashboard()
        {
            return View();
        }
        //udpateShipper
        [HttpPost]
        [Route("admin/shipper/update")]
        public async Task<IActionResult> UpdateShipper(int Id, string FullName, string Phone, string Address, string Email)
        {
            var shipper = await _context.Shippers.Include(s => s.User).FirstOrDefaultAsync(s => s.Id == Id);
            if (shipper != null)
            {
                shipper.FullName = FullName;
                shipper.Phone = Phone;
                shipper.VehicleInfo = shipper.VehicleInfo; // giữ nguyên nếu không cập nhật
                shipper.User.Email = Email;

                // Nếu có địa chỉ riêng thì cập nhật bảng khác (tùy design)
                var request = await _context.ShipperRegistrationRequests.FirstOrDefaultAsync(r => r.UserId == shipper.UserId);
                if (request != null)
                {
                    request.Address = Address;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Cập nhật shipper thành công.";
            }
            return Redirect("/shipper");
        }
        //DeleteShipper
        [HttpPost]
        [Route("admin/shipper/delete")]
        public async Task<IActionResult> DeleteShipper(int UserId)
        {
            try
            {
                var shipper = await _context.Shippers.FirstOrDefaultAsync(s => s.UserId == UserId);

                if (shipper == null)
                {
                    TempData["Error"] = "Không tìm thấy shipper.";
                    return Redirect("/shipper");
                }

                // Soft delete thay vì Remove
                shipper.Status = "pending"; 
                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã gỡ quyền shipper.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi xoá: {ex.Message}";
            }

            return Redirect("/shipper");
        }







    }
}

