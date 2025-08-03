using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using MimeKit;
using Village_Manager.Data;
using Village_Manager.Models;
using Village_Manager.Models.Dto;
using Village_Manager.ViewModel;

namespace Village_Manager.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailSettings _emailSettings;
        private static Dictionary<string, (string Otp, DateTime Expire)> otpStore = new();
        private readonly IWebHostEnvironment _env;
        public CustomerController( AppDbContext context, IConfiguration configuration, IOptions<EmailSettings> emailSettings, IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _emailSettings = emailSettings.Value;
            _env = env;
        }

        [HttpGet]
        [Route("customer")]
        public async Task<IActionResult> DashBoard()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Home");
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var totalOrders = await _context.RetailOrders.CountAsync(o => o.UserId == userId);
            var pendingOrders = await _context.RetailOrders.CountAsync(o => o.UserId == userId && o.Status == "pending");
            if (user != null)
            {
                ViewBag.UserId = userId;
                ViewBag.HasAcceptedGeo = user?.HasAcceptedGeolocation ?? false;
                ViewBag.Email = user.Email;
                ViewBag.Username = user.Username;
                ViewBag.TotalOrders = totalOrders;
                ViewBag.PendingOrders = pendingOrders;
                ViewBag.Phone = user.Phone ?? "Chưa có số điện thoại";

                // --- THÊM PHẦN LẤY LỊCH SỬ ĐƠN HÀNG ---
                var orderHistory = await _context.RetailOrders
                                    .Where(o => o.UserId == userId)
                                    .Include(o => o.RetailOrderItems)
                                    .ThenInclude(oi => oi.Product)
                                    .ThenInclude(p => p.ProductImages)
                                    .OrderByDescending(o => o.OrderDate)
                                    .ToListAsync();
                ViewBag.OrderHistory = orderHistory;
                var location = await _context.UserLocations
                                    .Where(ul => ul.UserId == userId)
                                    .ToListAsync();
                ViewBag.Location = location;
            }
            else
            {
                Response.StatusCode = 404;
                return View("404");
            }
            return View();
        }

        [HttpGet("customer/order-detail/{id}")]
        public async Task<IActionResult> OrderDetail(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
                return RedirectToAction("Login", "Home");

            var order = await _context.RetailOrders
                .Where(o => o.Id == id && o.UserId == userId)
                .Include(o => o.RetailOrderItems)
                    .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p.ProductImages)
                .Include(o => o.User)
                .Include(o => o.DiscountCode) // 🔹 Load DiscountCode từ DB
                .FirstOrDefaultAsync();

            if (order == null)
                return NotFound();

            // Lấy thông tin địa chỉ giao hàng từ bảng Delivery
            var delivery = await _context.Deliveries
                .FirstOrDefaultAsync(d => d.OrderId == order.Id && d.OrderType == "retail");

            ViewBag.DeliveryAddress = delivery?.CustomerAddress ?? "Không có thông tin địa chỉ";
            ViewBag.CustomerName = delivery?.CustomerName ?? order.User?.Username ?? "Không xác định";
            ViewBag.CustomerPhone = delivery?.CustomerPhone ?? order.User?.Phone ?? "Không xác định";

            // 🔹 Tính tổng tiền gốc
            decimal totalAmount = (decimal)order.RetailOrderItems.Sum(i => i.Quantity * (i.UnitPrice ?? 0));

            // 🔹 Tính giảm giá từ DiscountCode trong DB
            decimal discountAmount = 0;
            string discountCode = null;
            if (order.DiscountCode != null)
            {
                discountCode = order.DiscountCode.Code;
                discountAmount = totalAmount * order.DiscountCode.DiscountPercent / 100m;
            }

            // 🔹 Gán ViewBag
            ViewBag.DiscountCode = discountCode;
            ViewBag.DiscountAmount = discountAmount;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.FinalAmount = totalAmount - discountAmount;
            if (ViewBag.FinalAmount < 0) ViewBag.FinalAmount = 0;

            return PartialView("OrderDetail", order);
        }



        [HttpGet("/otp")]
        public async Task<IActionResult> Otp(string email, string phone)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Home");
            }
            var otp = new Random().Next(100000, 999999).ToString();
            otpStore[email] = (otp, DateTime.UtcNow.AddMinutes(15));

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Mã xác minh OTP thay đổi thông tin cá nhân";
            message.Body = new TextPart("plain") { Text = $"Mã OTP của bạn là: {otp} (hiệu lực trong 15 phút)." };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.AppPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            ViewBag.Email = email;
            ViewBag.Phone = phone;

            return View();
        }

        [HttpPost("/otp/confirm")]
        public async Task<IActionResult> ConfirmOtp(string Email, string Phone,
            string Otp1, string Otp2, string Otp3, string Otp4, string Otp5, string Otp6)
        {
            string otp = $"{Otp1}{Otp2}{Otp3}{Otp4}{Otp5}{Otp6}";

            if (otpStore.TryGetValue(Email, out var record))
            {
                if (record.Otp == otp && DateTime.UtcNow <= record.Expire)
                {
                    otpStore.Remove(Email);

                    var userId = HttpContext.Session.GetInt32("UserId");
                    if (userId == null) return RedirectToAction("Login", "Home");

                    var user = await _context.Users.FindAsync(userId);
                    if (user == null) return RedirectToAction("Login", "Home");

                    user.Email = Email;
                    user.Phone = Phone;

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Dashboard");
                }
            }

            TempData["Error"] = "Mã OTP không đúng hoặc đã hết hạn.";

            string safeEmail = Uri.EscapeDataString(Email);
            string safePhone = Uri.EscapeDataString(Phone);

            return Redirect($"/otp?email={safeEmail}&phone={safePhone}");
        }
                
    

        // hoàn hàng
        [HttpGet]
        public IActionResult CancelOrder(int orderId, string type)
        {
            ViewBag.OrderId = orderId;
            ViewBag.OrderType = type;
            return View("CancelOrder");
        }

        [HttpPost]
        public async Task<IActionResult> CancelOrder(int orderId, string type, string reason)
        {
            var userId = GetCurrentUserId(); // Tự viết logic lấy user ID

            // Tìm đơn hàng
            var order = await _context.RetailOrders
                .Include(o => o.RetailOrderItems) // Load cả danh sách item
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order != null && order.Status == "pending")
            {
                // Cập nhật trạng thái đơn hàng
                order.Status = "cancelled";

                // Cộng lại số lượng hàng vào kho
                foreach (var item in order.RetailOrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.Quantity += (int)item.Quantity;
                    }
                }

                // Ghi log hủy đơn hàng
                _context.ReturnOrders.Add(new ReturnOrder
                {
                    OrderId = orderId,
                    OrderType = type,
                    UserId = userId,
                    Quantity = (int)order.RetailOrderItems.Sum(i => i.Quantity),
                    Reason = reason,
                    CreatedAt = DateTime.Now,
                    ImageUrl = null
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Customer");
        }


        [HttpGet]
        public IActionResult ReturnOrder(int orderId, string type)
        {
            ViewBag.OrderId = orderId;
            ViewBag.OrderType = type;
            return View("ReturnOrder");
        }

        [HttpPost]
        public async Task<IActionResult> ReturnOrder(int orderId, string type, string reason, IFormFile image)
        {
            var userId = GetCurrentUserId();
            string imageUrl = null;

            if (image != null)
            {
                var folderName = "images/Reasonreturn"; // Đường dẫn thư mục cần lưu
                var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
                var relativePath = Path.Combine(folderName, fileName); // images/Reasonreturn/abc.jpg
                var savePath = Path.Combine(_env.WebRootPath, relativePath); // wwwroot/images/Reasonreturn/abc.jpg

                using var stream = new FileStream(savePath, FileMode.Create);
                await image.CopyToAsync(stream);

                imageUrl = "/" + relativePath.Replace("\\", "/"); // Lưu URL phục vụ truy cập
            }


            var order = await _context.RetailOrders.FindAsync(orderId);
            if (order != null && order.Status == "delivered")
            {
                order.Status = "inprocess";

                _context.ReturnOrders.Add(new ReturnOrder
                {
                    OrderId = orderId,
                    OrderType = type,
                    UserId = userId,
                    Quantity = (int)order.RetailOrderItems.Sum(i => i.Quantity),
                    Reason = reason,
                    CreatedAt = DateTime.Now,
                    ImageUrl = imageUrl
                });

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Customer");
        }
        
        private int GetCurrentUserId()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                throw new Exception("Người dùng chưa đăng nhập hoặc Session đã hết hạn.");
            }
            return userId.Value;
        }

        // Thêm địa chỉ mới
        [HttpPost]
        [Route("customer/add-address")]
        public async Task<IActionResult> AddAddress(int id, string label, string address, double latitude, double longitude)
        {
            try
            {
                var userId = GetCurrentUserId();

                var newAddress = new UserLocation
                {
                    UserId = userId,
                    Label = label,
                    Address = address,
                    Latitude = latitude,
                    Longitude = longitude
                };

                _context.UserLocations.Add(newAddress);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Thêm địa chỉ thành công!";
                return RedirectToAction("DashBoard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi thêm địa chỉ: " + ex.Message;
                return RedirectToAction("DashBoard");
            }
        }

        // Sửa địa chỉ
        [HttpPost]
        [Route("customer/edit-address")]
        public async Task<IActionResult> EditAddress(int id, string label, string address)
        {
            try
            {
                var userId = GetCurrentUserId();

                var existingAddress = await _context.UserLocations
                    .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

                if (existingAddress == null)
                {
                    TempData["Error"] = "Không tìm thấy địa chỉ.";
                    return RedirectToAction("DashBoard");
                }

                existingAddress.Label = label;
                existingAddress.Address = address;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Cập nhật địa chỉ thành công!";
                return RedirectToAction("DashBoard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi cập nhật địa chỉ: " + ex.Message;
                return RedirectToAction("DashBoard");
            }
        }

        // Xóa địa chỉ
        [HttpPost]
        [Route("customer/delete-address")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            try
            {
                var userId = GetCurrentUserId();

                var address = await _context.UserLocations
                    .FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);

                if (address == null)
                {
                    TempData["Error"] = "Không tìm thấy địa chỉ.";
                    return RedirectToAction("DashBoard");
                }

                _context.UserLocations.Remove(address);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Xóa địa chỉ thành công!";
                return RedirectToAction("DashBoard");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi khi xóa địa chỉ: " + ex.Message;
                return RedirectToAction("DashBoard");
            }
        }
    }
}

