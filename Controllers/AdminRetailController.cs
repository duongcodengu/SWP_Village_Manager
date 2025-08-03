using BCrypt.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Text.RegularExpressions;
using Utils;
using Village_Manager.Data;
using Village_Manager.Extensions;
using Village_Manager.Models;

namespace Village_Manager.Controllers
{
    public class AdminRetailController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AdminRetailController> _logger;
        private readonly IWebHostEnvironment _env;

        public AdminRetailController(AppDbContext context, IConfiguration configuration, ILogger<AdminRetailController> logger, IWebHostEnvironment env)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _env = env;
        }

        //hiển thị tổng
        private async Task<decimal> GetTotalRevenue()
        {
            var total = await _context.RetailOrderItems
                .SumAsync(x => (decimal?)(x.Quantity * x.UnitPrice));
            return total ?? 0;
        }

        private async Task<int> GetTotalOrders()
        {
            var total = await _context.RetailOrders.CountAsync();
            return total;
        }

        private async Task<int> GetTotalProducts()
        {
            var total = await _context.Products.CountAsync();
            return total;
        }

        private async Task<int> GetTotalCustomers()
        {
            var total = await _context.Users.CountAsync(u => u.RoleId == 3);
            return total;
        }
        [HttpGet]
        [Route("AdminRetail")]
        // Trang tổng hợp để đẩy lên View
        public async Task<IActionResult> Dashboard()
        {
            ViewBag.TotalRevenue = await GetTotalRevenue();
            ViewBag.TotalOrders = await GetTotalOrders();
            ViewBag.TotalProducts = await GetTotalProducts();
            ViewBag.TotalCustomers = await GetTotalCustomers();
            ViewBag.Categories = await _context.ProductCategory.ToListAsync();

            return View();
        }






        //phần admin quản lý sản phẩm bắt đầu

        [HttpGet]
        [Route("productsretail")]
        public async Task<IActionResult> Products(string sortOrder, string keyword, int page = 1, int pageSize = 5)
        {
            var query = _context.Products
                .Include(p => p.ProductImages)
                .Where(p => !_context.HiddenProduct.Any(h => h.ProductId == p.Id))
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(p => p.Name.Contains(keyword) || p.ProductType.Contains(keyword));

            ViewBag.CurrentSort = sortOrder;

            switch (sortOrder)
            {
                case "expSoon":
                    query = query.OrderBy(p => p.ExpirationDate); break;
                case "name_desc": query = query.OrderByDescending(p => p.Name); break;
                case "price_asc": query = query.OrderBy(p => p.Price); break;
                case "price_desc": query = query.OrderByDescending(p => p.Price); break;
                case "expiry_asc": query = query.OrderBy(p => p.ExpirationDate); break;
                default: query = query.OrderBy(p => p.Name); break;
            }

            int total = await query.CountAsync();
            var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            DefaultImage.Ensure(products, _env);

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);

            return View(products);
        }

        [HttpGet]
        public async Task<IActionResult> EditProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Lấy danh sách category để hiển thị dropdown
            ViewBag.Categories = await _context.ProductCategory
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();

            return View(product);
        }


        // POST: AdminRetail/EditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct(Product model)
        {
            

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (product == null)
            {
                return NotFound();
            }

            product.Name = model.Name;
            product.ProductType = model.ProductType;
            product.Quantity = model.Quantity;
            product.Price = model.Price;
            product.CategoryId = model.CategoryId;

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction("Products");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Lỗi khi cập nhật: " + ex.Message);

                ViewBag.Categories = await _context.ProductCategory
                    .Select(c => new { c.Id, c.Name })
                    .ToListAsync();

                return View(model);
            }
        }

        // hien thi chi tiet san pham 
        [HttpGet]
        [Route("AdminRetail/ProductDetail/{id}")]
        public async Task<IActionResult> ProductDetail(int id)
        {
            var product = await _context.Products
                .Include(p => p.Farmer)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            // Truyền xuống View
            return View(product);
        }
        //search 
        [HttpGet]
        public IActionResult SearchProducts(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return RedirectToAction("Products");
            }

            var products = _context.Products
                .Include(p => p.ProductImages)
                .Where(p => p.Name.Contains(keyword) || p.ProductType.Contains(keyword))
                .ToList();

            return View("Products", products);
        }
        // hàm xóa mềm sản phẩm
        public async Task<IActionResult> SoftDeleteProduct(int productId)
        {
            // Kiểm tra xem sản phẩm đã bị ẩn chưa
            bool isAlreadyHidden = await _context.HiddenProduct
                .AnyAsync(h => h.ProductId == productId);

            if (!isAlreadyHidden)
            {
                var hidden = new HiddenProduct
                {
                    ProductId = productId,
                    Reason = "Ẩn bởi admin bán lẻ",
                    HiddenAt = DateTime.Now
                };

                _context.HiddenProduct.Add(hidden);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Products");
        }
        //hiển thị sản phẩm hạn chế
        public async Task<IActionResult> HiddenProducts(int page = 1, int pageSize = 5)
        {
            // Lấy danh sách ID sản phẩm đã bị ẩn
            var hiddenProductIds = await _context.HiddenProduct
                .Select(h => h.ProductId)
                .ToListAsync();

            // Tổng số sản phẩm bị ẩn
            var totalItems = await _context.Products
                .Where(p => hiddenProductIds.Contains(p.Id))
                .CountAsync();

            // Lấy danh sách sản phẩm đã bị ẩn theo trang
            var products = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => hiddenProductIds.Contains(p.Id))
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(products);
        }

        // hiển thị lại sản phẩm bị hạn chế
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreProduct(int productId)
        {
            var hidden = await _context.HiddenProduct.FirstOrDefaultAsync(h => h.ProductId == productId);
            if (hidden != null)
            {
                _context.HiddenProduct.Remove(hidden);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("HiddenProducts");
        }


        // Hiển thị danh sách mã giảm giá
        [HttpGet]
        [Route("DiscountCodes")]
        public IActionResult DiscountCodes()
        {
            var list = _context.DiscountCodes
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            return View("DiscountCode", list);
        }

        // Hiển thị form tạo mới
        [HttpGet]
        [Route("CreateDiscountCode")]
        public IActionResult CreateDiscountCode()
        {
            return View();
        }

        // POST: Tạo mới mã giảm giá
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("CreateDiscountCode")]
        public IActionResult CreateDiscountCode(DiscountCodes model)
        {
            // Kiểm tra độ dài mã giảm giá
            if (string.IsNullOrEmpty(model.Code) || model.Code.Length < 6)
            {
                ModelState.AddModelError("Code", "Mã giảm giá phải có ít nhất 6 ký tự.");
            }

            // Kiểm tra % giảm giá
            if (model.DiscountPercent < 1 || model.DiscountPercent > 100)
            {
                ModelState.AddModelError("DiscountPercent", "Phần trăm giảm giá phải từ 1% đến 100%.");
            }

            // Kiểm tra giới hạn sử dụng
            if (model.UsageLimit <= 1)
            {
                ModelState.AddModelError("UsageLimit", "Giới hạn sử dụng phải lớn hơn 1.");
            }

            // Kiểm tra ngày hết hạn
            if (model.ExpiredAt <= DateTime.Now)
            {
                ModelState.AddModelError("ExpiredAt", "Ngày hết hạn phải sau thời điểm hiện tại.");
            }

            // Nếu có lỗi => trả về lại View
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ.";
                return View(model);
            }

            // Kiểm tra trùng mã
            var exist = _context.DiscountCodes.Any(c => c.Code == model.Code);
            if (exist)
            {
                ModelState.AddModelError("Code", "Mã giảm giá đã tồn tại.");
                return View(model);
            }

            model.CreatedAt = DateTime.Now;
            _context.DiscountCodes.Add(model);
            _context.SaveChanges();

            TempData["Success"] = "Thêm thành công.";
            return RedirectToAction("DiscountCodes");
        }

        [HttpGet]
        [Route("AllCustomers")]
        public async Task<IActionResult> AllCustomers(string searchUser, int page = 1)
        {
            var username = HttpContext.Session.GetString("Username");
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (string.IsNullOrEmpty(username) || roleId != 2) // Only staff can manage
            {
                return View("404");
            }

            int pageSize = 10;
            var usersQuery = _context.Users.Include(u => u.Role).Where(u => u.RoleId == 3); // Only customers

            if (!string.IsNullOrEmpty(searchUser))
                usersQuery = usersQuery.Where(u => u.Username.Contains(searchUser));

            int totalUsers = await usersQuery.CountAsync();
            var users = await usersQuery.OrderByDescending(u => u.CreatedAt)
                                        .Skip((page - 1) * pageSize)
                                        .Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalUsers / pageSize);
            ViewBag.SearchUser = searchUser;
            return View("AllCustomers", users);
        }

        [HttpGet]
        [Route("EditCustomer/{id}")]
        public async Task<IActionResult> EditCustomer(int id)
        {
            try
            {
                var roleId = HttpContext.Session.GetInt32("RoleId");
                if (roleId != 2) return View("404");

                var user = await _context.Users.FindAsync(id);
                if (user == null || user.RoleId != 3)
                {
                    return NotFound();
                }
                ViewBag.Roles = await _context.Roles.Where(r => r.Id == 3).ToListAsync();
                return View("EditCustomer", user);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading customer: {ex.Message}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi tải thông tin khách hàng.";
                return RedirectToAction("AllCustomers");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("EditCustomer/{id}")]
        public async Task<IActionResult> EditCustomer(int id, User user, string newPassword)
        {
            try
            {
                var roleId = HttpContext.Session.GetInt32("RoleId");
                if (roleId != 2) return View("404");

                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrWhiteSpace(user.Username) || string.IsNullOrWhiteSpace(user.Email))
                {
                    ModelState.AddModelError("", "Vui lòng nhập đầy đủ thông tin bắt buộc.");
                    return View(user);
                }

                // --- Kiểm tra EMAIL ---
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    ModelState.AddModelError("email", "Vui lòng nhập email.");
                }
                else
                {
                    // Regex kiểm tra định dạng email chuẩn (tên@tênmiền)
                    var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$");
                    if (!emailRegex.IsMatch(user.Email))
                    {
                        ModelState.AddModelError("email", "Email không hợp lệ. Vui lòng nhập đúng định dạng: ten@tenmien.com.");
                    }
                    else if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                    {
                        ModelState.AddModelError("email", "Email đã được sử dụng.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(user.Phone))
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(user.Phone, @"^0\d{9}$"))
                        ModelState.AddModelError("phone", "Số điện thoại phải bắt đầu bằng 0 và có đúng 10 chữ số.");
                }

                var existingUser = await _context.Users.FindAsync(id);
                if (existingUser == null || existingUser.RoleId != 3)
                {
                    return NotFound();
                }

                // Kiểm tra trùng username/email
                if (await _context.Users.AnyAsync(u => u.Username == user.Username && u.Id != id))
                {
                    ModelState.AddModelError("Username", "Username đã tồn tại.");
                    return View(user);
                }
                if (await _context.Users.AnyAsync(u => u.Email == user.Email && u.Id != id))
                {
                    ModelState.AddModelError("Email", "Email đã tồn tại.");
                    return View(user);
                }

                // Cập nhật thông tin
                existingUser.Username = user.Username.Trim();
                existingUser.Email = user.Email.Trim();
                existingUser.Phone = user.Phone?.Trim();

                if (!string.IsNullOrEmpty(newPassword))
                {
                    existingUser.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã cập nhật thông tin khách hàng thành công.";
                return RedirectToAction("AllCustomers");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Lỗi khi cập nhật khách hàng: {ex.Message}");
                ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật.");
                return View(user);
            }
        }

        [HttpGet]
        [Route("AddCustomer")]
        public IActionResult AddCustomer()
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 2) return View("404");

            ViewBag.Roles = _context.Roles.Where(r => r.Id == 3).ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("AddCustomer")]
        public async Task<IActionResult> AddCustomer(string username, string email, string password, string phone)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 2) return View("404");

            // Kiểm tra các trường bắt buộc
            if (string.IsNullOrWhiteSpace(username))
            {
                TempData["Error"] = "Vui lòng nhập username.";
                return RedirectToAction("AddCustomer");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Vui lòng nhập email.";
                return RedirectToAction("AddCustomer");
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Vui lòng nhập mật khẩu.";
                return RedirectToAction("AddCustomer");
            }

            if (password.Length < 6)
            {
                TempData["Error"] = "Mật khẩu phải có ít nhất 6 ký tự.";
                return RedirectToAction("AddCustomer");
            }

            // Kiểm tra định dạng email
            var emailRegex = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-z0-9.-]+\.[a-z]{2,}$");
            if (!emailRegex.IsMatch(email))
            {
                TempData["Error"] = "Email không hợp lệ. Vui lòng nhập đúng định dạng: ten@tenmien.com.";
                return RedirectToAction("AddCustomer");
            }

            // Kiểm tra định dạng số điện thoại nếu có
            if (!string.IsNullOrWhiteSpace(phone))
            {
                if (!Regex.IsMatch(phone, @"^0\d{9}$"))
                {
                    TempData["Error"] = "Số điện thoại phải bắt đầu bằng 0 và có đúng 10 chữ số.";
                    return RedirectToAction("AddCustomer");
                }
            }

            // Kiểm tra trùng lặp username
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                TempData["Error"] = "Username đã tồn tại.";
                return RedirectToAction("AddCustomer");
            }

            // Kiểm tra trùng lặp email
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                TempData["Error"] = "Email đã được sử dụng.";
                return RedirectToAction("AddCustomer");
            }

            // Kiểm tra trùng lặp số điện thoại nếu có
            if (!string.IsNullOrWhiteSpace(phone))
            {
                if (await _context.Users.AnyAsync(u => u.Phone == phone.Trim()))
                {
                    TempData["Error"] = "Số điện thoại đã được sử dụng.";
                    return RedirectToAction("AddCustomer");
                }
            }

            // --- CREATE NEW CUSTOMER ---
            var newCustomer = new User
            {
                Username = username.Trim(),
                Email = email.Trim(),
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
                RoleId = 3, // Customer
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.Users.Add(newCustomer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Thêm khách hàng thành công.";
            return RedirectToAction("AllCustomers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("AdminRetail/BanCustomer/{id}")]
        public IActionResult BanUser(int id)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 2)
            {
                Response.StatusCode = 404;
                return View("404");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = false;
            _context.SaveChanges();
            TempData["SuccessMessage"] = $"Đã khóa tài khoản {user.Username}";
            return RedirectToAction("AllCustomers");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("AdminRetail/UnbanCustomer/{id}")]
        public IActionResult UnbanUser(int id)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 2)
            {
                Response.StatusCode = 404;
                return View("404");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = true;
            _context.SaveChanges();
            TempData["SuccessMessage"] = $"Đã mở khóa tài khoản {user.Username}";
            return RedirectToAction("AllCustomers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("AdminRetail/DeleteCustomer/{id}")]
        public IActionResult Delete(int id)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            var currentUserId = HttpContext.Session.GetInt32("UserId");

            // Chỉ cho phép admin chính (1) và admin bán lẻ (2)
            if (roleId != 2)
            {
                Response.StatusCode = 404;
                return View("404");
            }

            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                return NotFound();
            }


            _context.Users.Remove(user);
            _context.SaveChanges();

            LogHelper.SaveLog(_context, currentUserId, $"Xóa user: {user.Username} (ID: {user.Id})");

            TempData["SuccessMessage"] = $"Đã xóa tài khoản {user.Username}";

            return RedirectToAction(roleId == 2 ? "AllCustomers" : "AllUser");
        }
        [HttpGet]
        public IActionResult OrderListRetail()
        {
            return View(); 
        }
        public IActionResult OrderListProcessed()
        {
            return View();
        }
        public async Task<IActionResult> OrderDetailRetail(int id)
        {
            var order = await _context.RetailOrders
                .Include(o => o.User)
                .Include(o => o.RetailOrderItems)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order); // gắn vào OrderDetailRetail.cshtml
        }


        // xử lý hoàn hàng
        public async Task<IActionResult> ReturnOrderManage(string searchTerm, int page = 1, int pageSize = 10)
        {
            var query = _context.ReturnOrders
                .Include(r => r.User)
                .Include(r => r.Order)
                .Where(r => r.Order.Status == "inprocess");

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r =>
                    r.User.Email.Contains(searchTerm) ||
                    r.Id.ToString().Contains(searchTerm));
            }

            var totalItems = await query.CountAsync();

            var returnOrders = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.SearchTerm = searchTerm;

            return View(returnOrders);
        }


        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            var returnOrder = await _context.ReturnOrders
                .Include(r => r.Order) // bao gồm đơn hàng
                .FirstOrDefaultAsync(r => r.Id == id);

            if (returnOrder == null || returnOrder.Order == null)
                return NotFound();

           
            returnOrder.Order.Status = "returned"; // cập nhật đơn hàng

            await _context.SaveChangesAsync();

            
            return RedirectToAction(nameof(ReturnOrderManage));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            var returnOrder = await _context.ReturnOrders
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (returnOrder != null && returnOrder.Order != null)
            {
                returnOrder.Order.Status = "delivered"; // từ chối → giữ trạng thái giao hàng
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("ReturnOrderManage");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, string newStatus)
        {
            var order = await _context.RetailOrders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = newStatus;
                
                // Nếu trạng thái là cancelled, hoàn trả số lượng sản phẩm vào kho
                if (newStatus == "cancelled")
                {
                    var orderItems = await _context.RetailOrderItems
                        .Where(oi => oi.OrderId == orderId)
                        .Include(oi => oi.Product)
                        .ToListAsync();

                    foreach (var item in orderItems)
                    {
                        if (item.Product != null)
                        {
                            item.Product.Quantity += (int)item.Quantity;
                        }
                    }
                }
                
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái đơn hàng #{orderId} thành {newStatus}";
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng";
            }
            
            return RedirectToAction("OrderDetailRetail", new { id = orderId });
        }

        public async Task<IActionResult> ReturnOrderDetails(int id)
        {
            var returnOrder = await _context.ReturnOrders
                .Include(r => r.User)
                .Include(r => r.Order)
                .ThenInclude(o => o.RetailOrderItems) // nếu cần
                .FirstOrDefaultAsync(r => r.Id == id);

            if (returnOrder == null)
            {
                return NotFound();
            }

            return View(returnOrder);
        }

    }
}

