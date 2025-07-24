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

        //[HttpGet]
        //[Route("dashboard")]
        //public IActionResult DashBoard()
        //{
        //    var userLocations = _context.UserLocations
        //                        .Include(u1 => u1.User)
        //                        .ToList();
        //    return View(userLocations);
        //}
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
            var address = await _context.Addresses
                .Where(a => a.UserId == userId)
                .Select(a => a.AddressLine)
                .FirstOrDefaultAsync();
            if (user != null)
            {
                ViewBag.UserId = userId;
                ViewBag.HasAcceptedGeo = user?.HasAcceptedGeolocation ?? false;
                ViewBag.Email = user.Email;
                ViewBag.Username = user.Username;
                ViewBag.TotalOrders = totalOrders;
                ViewBag.PendingOrders = pendingOrders;
                ViewBag.Address = address ?? "Chưa có địa chỉ";
                ViewBag.Phone = user.Phone ?? "Chưa có số điện thoại";

                var addressParts = address?.Split(",") ?? new string[0];
                ViewBag.DetailAddress = addressParts.Length > 0 ? addressParts[0].Trim() : "";
                ViewBag.Ward = addressParts.Length > 1 ? addressParts[1].Trim() : "";
                ViewBag.District = addressParts.Length > 2 ? addressParts[2].Trim() : "";
                ViewBag.Province = addressParts.Length > 3 ? addressParts[3].Trim() : "";

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
                    .ThenInclude(u => u.Addresses)
                .FirstOrDefaultAsync();

            if (order == null)
                return NotFound();

            // Lấy mã giảm giá từ session nếu có
            var discountCode = HttpContext.Session.GetString("DiscountCode");
            var discountAmountStr = HttpContext.Session.GetString("DiscountAmount");
            decimal discountAmount = 0;
            if (decimal.TryParse(discountAmountStr, out var value))
                discountAmount = value;

            ViewBag.DiscountCode = discountCode;
            ViewBag.DiscountAmount = discountAmount;
            ViewBag.TotalAmount = order.RetailOrderItems.Sum(i => i.Quantity * (i.UnitPrice ?? 0));
            ViewBag.FinalAmount = ViewBag.TotalAmount - discountAmount;

            return PartialView("OrderDetail", order);
        }


        [HttpGet("/otp")]
        public async Task<IActionResult> Otp(string email, string phone, string address)
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
            ViewBag.Address = address;

            return View();
        }

        [HttpPost("/otp/confirm")]
        public async Task<IActionResult> ConfirmOtp(string Email, string Phone, string Address,
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

                    var address = await _context.Addresses.FirstOrDefaultAsync(a => a.UserId == userId);
                    if (address == null)
                    {
                        _context.Addresses.Add(new Address
                        {
                            UserId = userId.Value,
                            AddressLine = Address
                        });
                    }
                    else
                    {
                        address.AddressLine = Address;
                    }

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("Dashboard");
                }
            }

            TempData["Error"] = "Mã OTP không đúng hoặc đã hết hạn.";

            string safeEmail = Uri.EscapeDataString(Email);
            string safePhone = Uri.EscapeDataString(Phone);
            string safeAddress = Uri.EscapeDataString(Address);

            return Redirect($"/otp?email={safeEmail}&phone={safePhone}&address={safeAddress}");
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

            // Update trạng thái order
            var order = await _context.RetailOrders.FindAsync(orderId);
            if (order != null && order.Status == "pending")
            {
                order.Status = "cancelled";

                _context.ReturnOrders.Add(new ReturnOrder
                {
                    OrderId = orderId,
                    OrderType = type,
                    UserId = userId,
                    Quantity = (int)order.RetailOrderItems.Sum(i => i.Quantity),
                    Reason = reason,
                    CreatedAt = DateTime.Now
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

            return (int)userId;

            throw new Exception("Người dùng chưa đăng nhập hoặc Session đã hết hạn.");
        }



        public async Task<IActionResult> CancelOrder()
        {
            return View();
        }
        public async Task<IActionResult> ReturnOrder()
        {
            return View();
        }
    }
}

