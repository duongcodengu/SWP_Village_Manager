using MailKit.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using MimeKit;
using Village_Manager.Data;
using Village_Manager.Models;
using Village_Manager.Models.Dto;

namespace Village_Manager.Controllers
{
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly EmailSettings _emailSettings;
        private static Dictionary<string, (string Otp, DateTime Expire)> otpStore = new();

        public CustomerController( AppDbContext context, IConfiguration configuration, IOptions<EmailSettings> emailSettings)
        {
            _context = context;
            _configuration = configuration;
            _emailSettings = emailSettings.Value;
        }

        [HttpGet]
        [Route("dashboard")]
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
            }
            else
            {
                Response.StatusCode = 404;
                return View("404");
            }
            return View();
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

    }
}
