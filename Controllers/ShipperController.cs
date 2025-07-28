using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using Village_Manager.Models;
using BCrypt.Net;

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
        [Route("dashboardshipper")]
        public IActionResult DashboardShipper()
        {
            var shipperId = HttpContext.Session.GetInt32("ShipperId");
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (!shipperId.HasValue || roleId != 4)
                return RedirectToAction("Login", "Home");

            Console.WriteLine($"[DEBUG] Session ShipperId: {shipperId}");
            if (!shipperId.HasValue)
                return RedirectToAction("Login", "Home");

            // Đơn đang giao
            var deliveringDeliveries = _context.Deliveries.Count(d => d.ShipperId == shipperId && (d.Status == "shipped" || d.Status == "in_transit"));
            // Đơn chờ nhận
            var pendingDeliveries = _context.Deliveries.Count(d => d.ShipperId == shipperId && d.Status == "assigned");
            // Đơn hoàn thành
            var completedDeliveries = _context.Deliveries.Count(d => d.ShipperId == shipperId && d.Status == "delivered");
            // Đơn hàng gần nhất
            var recentDeliveries = _context.Deliveries
                .Where(d => d.ShipperId == shipperId)
                .OrderByDescending(d => d.StartTime)
                .Take(5)
                .ToList();
            ViewBag.DeliveringDeliveries = deliveringDeliveries;
            ViewBag.PendingDeliveries = pendingDeliveries;
            ViewBag.CompletedDeliveries = completedDeliveries;
            ViewBag.RecentDeliveries = recentDeliveries;

            return View();
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
            return Redirect("shipperbecome");
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
            Console.WriteLine("UserId nhận được: " + UserId);
            try
            {
                Console.WriteLine($"[DeleteShipper] Received UserId: {UserId}");

                var shipper = await _context.Shippers.FirstOrDefaultAsync(s => s.UserId == UserId);
                if (shipper == null)
                {
                    TempData["Error"] = $"Không tìm thấy shipper với UserId = {UserId}";
                    return Redirect("/shipper");
                }

                // Soft delete thay vì Remove
                shipper.Status = "pending";
                await _context.SaveChangesAsync();

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == UserId);
                if (user == null)
                {
                    TempData["Error"] = "Không tìm thấy user.";
                    return Redirect("/shipper");
                }

                if (user.RoleId == 4)
                {
                    user.RoleId = 3;
                    _context.Users.Update(user);
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã gỡ quyền shipper và chuyển về khách hàng.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi gỡ quyền: {ex.Message}";
            }

            return Redirect("/shipper");
        }
        
        public IActionResult OrdersShipper(string searchOrderId = "", string searchCustomer = "", string searchStatus = "", int page = 1, int pageSize = 10)
        {
            var query = _context.RetailOrders
                .Include(o => o.RetailOrderItems)
                    .ThenInclude(ri => ri.Product)
                .Include(o => o.User)
                .AsQueryable();

            // Tìm kiếm theo mã đơn hàng
            if (!string.IsNullOrWhiteSpace(searchOrderId))
            {
                if (int.TryParse(searchOrderId, out int orderId))
                {
                    query = query.Where(o => o.Id == orderId);
                }
            }

            // Tìm kiếm theo tên khách hàng
            if (!string.IsNullOrWhiteSpace(searchCustomer))
            {
                query = query.Where(o => o.User.Username.Contains(searchCustomer));
            }

            // Tìm kiếm theo trạng thái
            if (!string.IsNullOrWhiteSpace(searchStatus))
            {
                query = query.Where(o => o.Status == searchStatus);
            }
            else
            {
                // Mặc định chỉ hiển thị đơn pending
                query = query.Where(o => o.Status == "confirmed");
            }

            int totalItems = query.Count();
            var orders = query.OrderByDescending(o => o.Id).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchOrderId = searchOrderId;
            ViewBag.SearchCustomer = searchCustomer;
            ViewBag.SearchStatus = searchStatus;

            return View(orders);
        }

        [HttpGet]
        public IActionResult RetailOrderDetail(int id)
        {
            var order = _context.RetailOrders
                .Include(o => o.RetailOrderItems)
                    .ThenInclude(ri => ri.Product)
                .Include(o => o.User)
                .FirstOrDefault(o => o.Id == id);
            if (order == null)
                return NotFound();

            // Lấy thông tin địa chỉ giao hàng từ bảng Delivery
            var delivery = _context.Deliveries
                .FirstOrDefault(d => d.OrderId == order.Id && d.OrderType == "retail");
            
            ViewBag.DeliveryAddress = delivery?.CustomerAddress ?? "Không có thông tin địa chỉ";
            ViewBag.CustomerName = delivery?.CustomerName ?? order.User?.Username ?? "Không xác định";
            ViewBag.CustomerPhone = delivery?.CustomerPhone ?? order.User?.Phone ?? "Không xác định";

            return View(order);
        }

        [HttpPost]
        public IActionResult AcceptOrder(int retailOrderId)
        {
            int shipperId = HttpContext.Session.GetInt32("ShipperId") ?? 0;
            var order = _context.RetailOrders.Include(o => o.User)
                .Include(o => o.User)
                .Include(o => o.RetailOrderItems)
                .FirstOrDefault(o => o.Id == retailOrderId && o.Status == "confirmed");
            if (order != null && shipperId > 0)
            {

                // Tạo bản ghi giao hàng
                var delivery = new Delivery
                {
                    OrderId = order.Id,
                    OrderType = "retail",
                    Status = "assigned", // trạng thái của bảng Delivery
                    ShipperId = shipperId,
                    CustomerName = order.User?.Username,
                    StartTime = DateTime.Now
                };
                _context.Deliveries.Add(delivery);

                // Cập nhật trạng thái đơn hàng
                order.Status = "shipped"; // Đúng với CHECK constraint
                _context.SaveChanges();
                TempData["Success"] = "Nhận đơn thành công!";
            }
            else
            {
                TempData["Error"] = "Không thể nhận đơn. Đơn đã được nhận hoặc có lỗi xảy ra.";
            }
            return RedirectToAction("OrdersShipper");
        }
        public IActionResult DeliveriesShipper(string searchOrderId = "", string searchCustomer = "", string searchStatus = "", string searchDateRange = "", int page = 1, int pageSize = 10)
        {
            var shipperId = HttpContext.Session.GetInt32("ShipperId");
            if (!shipperId.HasValue)
                return RedirectToAction("Login", "Home");

            var query = _context.Deliveries
                .Where(d => d.ShipperId == shipperId)
                .AsQueryable();

            // Tìm kiếm theo mã đơn hàng
            if (!string.IsNullOrWhiteSpace(searchOrderId))
            {
                if (int.TryParse(searchOrderId, out int orderId))
                {
                    query = query.Where(d => d.OrderId == orderId);
                }
            }

            // Tìm kiếm theo tên khách hàng
            if (!string.IsNullOrWhiteSpace(searchCustomer))
            {
                query = query.Where(d => d.CustomerName.Contains(searchCustomer));
            }

            // Tìm kiếm theo trạng thái
            if (!string.IsNullOrWhiteSpace(searchStatus))
            {
                query = query.Where(d => d.Status == searchStatus);
            }
            else
            {
                // Mặc định chỉ hiển thị đơn assigned và in_transit
                query = query.Where(d => d.Status == "assigned" || d.Status == "in_transit");
            }

            // Tìm kiếm theo khoảng thời gian
            if (!string.IsNullOrWhiteSpace(searchDateRange))
            {
                var dates = searchDateRange.Split(" - ");
                if (dates.Length == 2 && DateTime.TryParse(dates[0], out DateTime startDate) && DateTime.TryParse(dates[1], out DateTime endDate))
                {
                    endDate = endDate.AddDays(1); // Bao gồm cả ngày cuối
                    query = query.Where(d => d.StartTime >= startDate && d.StartTime < endDate);
                }
            }

            int totalItems = query.Count();
            var deliveries = query.OrderByDescending(d => d.StartTime).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchOrderId = searchOrderId;
            ViewBag.SearchCustomer = searchCustomer;
            ViewBag.SearchStatus = searchStatus;
            ViewBag.SearchDateRange = searchDateRange;

            return View(deliveries);
        }
        public IActionResult ProfileShipper()
        {
            var shipperId = HttpContext.Session.GetInt32("ShipperId");
            if (!shipperId.HasValue)
                return RedirectToAction("Login", "Home");

            var shipper = _context.Shippers
                .Where(s => s.Id == shipperId)
                .Select(s => new
                {
                    s.Id,
                    s.FullName,
                    s.Phone,
                    s.VehicleInfo,
                    User = s.User
                })
                .FirstOrDefault();

            if (shipper == null) return RedirectToAction("Login", "Home");

            ViewBag.Profile = shipper;
            return View();
        }
        public IActionResult HistoryShipper(string searchOrderId = "", string searchCustomer = "", string searchStatus = "", string searchDateRange = "", int page = 1, int pageSize = 10)
        {
            var shipperId = HttpContext.Session.GetInt32("ShipperId");
            if (!shipperId.HasValue)
                return RedirectToAction("Login", "Home");

            var query = _context.Deliveries
                .Where(d => d.ShipperId == shipperId)
                .AsQueryable();

            // Tìm kiếm theo mã đơn hàng
            if (!string.IsNullOrWhiteSpace(searchOrderId))
            {
                if (int.TryParse(searchOrderId, out int orderId))
                {
                    query = query.Where(d => d.OrderId == orderId);
                }
            }

            // Tìm kiếm theo tên khách hàng
            if (!string.IsNullOrWhiteSpace(searchCustomer))
            {
                query = query.Where(d => d.CustomerName.Contains(searchCustomer));
            }

            // Tìm kiếm theo trạng thái
            if (!string.IsNullOrWhiteSpace(searchStatus))
            {
                query = query.Where(d => d.Status == searchStatus);
            }
            else
            {
                // Mặc định chỉ hiển thị đơn delivered và failed
                query = query.Where(d => d.Status == "delivered" || d.Status == "failed");
            }

            // Tìm kiếm theo khoảng thời gian
            if (!string.IsNullOrWhiteSpace(searchDateRange))
            {
                var dates = searchDateRange.Split(" - ");
                if (dates.Length == 2 && DateTime.TryParse(dates[0], out DateTime startDate) && DateTime.TryParse(dates[1], out DateTime endDate))
                {
                    endDate = endDate.AddDays(1); // Bao gồm cả ngày cuối
                    query = query.Where(d => d.EndTime >= startDate && d.EndTime < endDate);
                }
            }

            int totalItems = query.Count();
            var deliveries = query.OrderByDescending(d => d.EndTime).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            // Lấy proof cho từng đơn
            var deliveryIds = deliveries.Select(d => d.Id).ToList(); // Id là int
            var proofs = _context.DeliveryProofs.Where(p => p.DeliveryId.HasValue && deliveryIds.Contains(p.DeliveryId.Value)).ToList();
            ViewBag.DeliveryProofs = proofs;

            // Lấy issue cho từng đơn
            var issues = _context.DeliveryIssues.Where(i => deliveryIds.Contains(i.DeliveryId)).ToList();
            ViewBag.DeliveryIssues = issues;

            ViewBag.TotalItems = totalItems;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchOrderId = searchOrderId;
            ViewBag.SearchCustomer = searchCustomer;
            ViewBag.SearchStatus = searchStatus;
            ViewBag.SearchDateRange = searchDateRange;

            return View(deliveries);
        }
        public IActionResult NotificationsShipper()
        {
            return View();
        }
        public IActionResult LogsShipper()
        {
            return View();
        }

        [HttpPost]
        public IActionResult StartDelivery(int id)
        {
            var delivery = _context.Deliveries.FirstOrDefault(d => d.Id == id);
            if (delivery != null && delivery.Status == "assigned")
            {
                delivery.Status = "in_transit";
                _context.SaveChanges();
            }
            return RedirectToAction("DeliveriesShipper");
        }

        [HttpPost]
        public IActionResult ConfirmDelivery(int id)
        {
            var delivery = _context.Deliveries.FirstOrDefault(d => d.Id == id);
            if (delivery != null && delivery.Status == "in_transit")
            {
                delivery.Status = "delivered";
                delivery.EndTime = DateTime.Now;
                _context.SaveChanges();
            }
            return RedirectToAction("DeliveriesShipper");
        }


        [HttpPost]
        public IActionResult ConfirmDeliveryProof(int id, string note, IFormFile proofImage)
        {
            var delivery = _context.Deliveries.FirstOrDefault(d => d.Id == id);
            var shipperId = HttpContext.Session.GetInt32("ShipperId");

            if (delivery != null && delivery.Status == "in_transit" && shipperId.HasValue)
            {
                string imagePath = null;

                // Xử lý lưu ảnh nếu có
                if (proofImage != null && proofImage.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
                    var fileName = $"proof_{delivery.Id}_{DateTime.Now.Ticks}{Path.GetExtension(proofImage.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        proofImage.CopyTo(stream);
                    }
                    imagePath = "/uploads/" + fileName;
                }

                // Lưu thông tin DeliveryProof
                _context.DeliveryProofs.Add(new DeliveryProof
                {
                    DeliveryId = delivery.Id,
                    ShipperId = shipperId.Value,
                    ImagePath = imagePath,
                    Note = note,
                    CreatedAt = DateTime.Now
                });

                // Cập nhật trạng thái giao hàng
                delivery.Status = "delivered";
                delivery.EndTime = DateTime.Now;

                // Xử lý cập nhật RetailOrder và tạo payment nếu cần
                var order = _context.RetailOrders.FirstOrDefault(o => o.Id == delivery.OrderId);
                if (order != null && order.Status != "delivered")
                {
                    order.Status = "delivered";
                    order.ConfirmedAt = DateTime.Now;

                    // Tính tổng tiền đơn hàng
                    var totalAmount = _context.RetailOrderItems
                        .Where(i => i.OrderId == order.Id)
                        .Sum(i => i.Quantity * i.UnitPrice);

                    // Kiểm tra đã có payment chưa
                    bool alreadyPaid = _context.Payments.Any(p => p.OrderId == order.Id && p.OrderType == "retail");
                    if (!alreadyPaid)
                    {
                        var payment = new Payment
                        {
                            UserId = order.UserId,
                            OrderId = order.Id,
                            OrderType = "retail",
                            Amount = totalAmount,
                            PaidAt = DateTime.Now,
                            Method = "cash",
                            PaymentType = "receive"
                        };
                        _context.Payments.Add(payment);
                    }
                }

                _context.SaveChanges();
            }

            return RedirectToAction("DeliveriesShipper");
        }


        [HttpPost]
        public IActionResult ReportIssue(int id, string reason)
        {
            var delivery = _context.Deliveries.FirstOrDefault(d => d.Id == id);
            if (delivery != null)
            {
                delivery.Status = "failed";
                _context.SaveChanges();

                // Lưu lý do vào bảng DeliveryIssue
                var shipperId = HttpContext.Session.GetInt32("ShipperId");
                _context.DeliveryIssues.Add(new DeliveryIssue
                {
                    DeliveryId = id,
                    ShipperId = shipperId ?? 0,
                    IssueType = "other",
                    Description = reason,
                    ReportedAt = DateTime.Now
                });
                _context.SaveChanges();
            }
            return RedirectToAction("DeliveriesShipper");
        }

        [HttpPost]
        public IActionResult UpdateVehicleInfo(int id, string vehicleInfo)
        {
            var shipper = _context.Shippers.FirstOrDefault(s => s.Id == id);
            if (shipper != null)
            {
                shipper.VehicleInfo = vehicleInfo;
                _context.SaveChanges();
            }
            return RedirectToAction("ProfileShipper");
        }

        [HttpPost]
        public IActionResult UpdateProfile(int id, string fullName, string phone, string vehicleInfo)
        {
            var shipper = _context.Shippers.FirstOrDefault(s => s.Id == id);
            if (shipper == null)
                return RedirectToAction("ProfileShipper");

            // Kiểm tra phương tiện không được để trống
            if (string.IsNullOrWhiteSpace(vehicleInfo))
            {
                TempData["ProfileError"] = "Phương tiện không được để trống.";
                return RedirectToAction("ProfileShipper");
            }

            // Kiểm tra số điện thoại chỉ chứa số
            if (string.IsNullOrWhiteSpace(phone) || phone.Any(c => !char.IsDigit(c)))
            {
                TempData["ProfileError"] = "Số điện thoại chỉ được chứa ký tự số.";
                return RedirectToAction("ProfileShipper");
            }

            // Kiểm tra số điện thoại không trùng với shipper khác
            var phoneExists = _context.Shippers.Any(s => s.Phone == phone && s.Id != id);
            if (phoneExists)
            {
                TempData["ProfileError"] = "Số điện thoại đã tồn tại.";
                return RedirectToAction("ProfileShipper");
            }

            shipper.FullName = fullName;
            shipper.Phone = phone;
            shipper.VehicleInfo = vehicleInfo;
            _context.SaveChanges();
            TempData["ProfileSuccess"] = "Cập nhật thông tin thành công.";
            return RedirectToAction("ProfileShipper");
        }

        [HttpPost]
        public IActionResult ChangePassword(int id, string currentPassword, string newPassword, string confirmPassword)
        {
            var shipper = _context.Shippers.Include(s => s.User).FirstOrDefault(s => s.Id == id);
            if (shipper == null || shipper.User == null)
            {
                TempData["PasswordError"] = "Không tìm thấy tài khoản.";
                return RedirectToAction("ProfileShipper");
            }
            if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["PasswordError"] = "Vui lòng nhập đầy đủ thông tin.";
                return RedirectToAction("ProfileShipper");
            }
            if (!Village_Manager.Utils.PasswordHelper.VerifyPassword(currentPassword, shipper.User.Password))
            {
                TempData["PasswordError"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction("ProfileShipper");
            }
            if (newPassword != confirmPassword)
            {
                TempData["PasswordError"] = "Mật khẩu mới và xác nhận không khớp.";
                return RedirectToAction("ProfileShipper");
            }
            if (newPassword.Length < 6)
            {
                TempData["PasswordError"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return RedirectToAction("ProfileShipper");
            }
            shipper.User.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.SaveChanges();
            TempData["PasswordSuccess"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("ProfileShipper");
        }

        [HttpGet]
        public IActionResult DeliveryDetail(int id)
        {
            var delivery = _context.Deliveries
                .Include(d => d.Shipper)
                .Include(d => d.RetailOrder)
                    .ThenInclude(r => r.User)
                .FirstOrDefault(d => d.Id == id);

            if (delivery == null)
                return NotFound();

            return View(delivery);
        }

        // Hiển thị đơn hàng đã trả (chỉ hiển thị đơn có status returned)
        [HttpGet]
        public IActionResult ReturnedOrders()
        {
            var returnedOrders = _context.RetailOrders
                .Include(r => r.User)
                .Include(r => r.RetailOrderItems)
                    .ThenInclude(ri => ri.Product)
                .Where(r => r.Status == "returned")
                .OrderByDescending(r => r.OrderDate)
                .ToList();

            // Lấy lý do trả hàng từ ReturnOrder
            var orderIds = returnedOrders.Select(o => o.Id).ToList();
            var returnReasons = _context.ReturnOrders
                .Where(r => orderIds.Contains(r.OrderId))
                .ToDictionary(r => r.OrderId, r => r.Reason);
            ViewBag.ReturnReasons = returnReasons;

            return View(returnedOrders);
        }

        // Shipper nhận đơn trả hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptReturnOrder(int orderId)
        {
            var retailOrder = _context.RetailOrders.FirstOrDefault(o => o.Id == orderId && o.Status == "returned");
            if (retailOrder == null)
            {
                TempData["Error"] = "Không tìm thấy đơn trả hàng.";
                return RedirectToAction("ReturnedOrders");
            }

            // Cập nhật trạng thái để đơn biến mất khỏi danh sách
            retailOrder.Status = "delivered";
            _context.SaveChanges();

            TempData["ReturnSuccess"] = "Đã nhận đơn trả hàng thành công!";
            return RedirectToAction("ReturnedOrders");
        }
    }
}
