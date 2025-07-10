using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Controllers
{
    public class AdminRetailController(AppDbContext context, IConfiguration configuration) : Controller
    {
        private readonly AppDbContext _context = context;
        private readonly IConfiguration _configuration = configuration;

        // kiểm tra quyền truy cập
        [HttpGet]
        [Route("AdminRetail")]
        public IActionResult Dashboard()
        {
            var username = HttpContext.Session.GetString("Username");
            var roleId = HttpContext.Session.GetInt32("RoleId");

            if (string.IsNullOrEmpty(username) || roleId != 2)
            {
                Response.StatusCode = 404;
                return View("404");
            }

            // Tổng số khách hàng
            int totalCustomers = _context.Users.Count();
            ViewBag.TotalCustomers = totalCustomers;

            // Tổng số sản phẩm
            int totalProducts = _context.Products.Count();
            ViewBag.TotalProducts = totalProducts;

            // Tổng số đơn hàng
            int totalRetailOrders = _context.RetailOrders.Count();
            ViewBag.TotalOrders = totalRetailOrders;

            // Lấy category (name, image_url)
            var categories = _context.ProductCategory
                .Select(c => new
                {
                    Name = c.Name,
                    ImageUrl = c.ImageUrl
                }).ToList<dynamic>();
            ViewBag.Categories = categories;

            // Tổng doanh thu confirmed
            decimal currentYear = DateTime.Now.Year;

            // Bán lẻ (Retail)
            var retailRevenue = _context.RetailOrders
                .Where(ro => ro.Status == "confirmed"
                    && ro.ConfirmedAt.HasValue
                    && ro.ConfirmedAt.Value.Year == currentYear)
                .Join(_context.RetailOrderItems,
                      ro => ro.Id,
                      ri => ri.OrderId,
                      (ro, ri) => ri.Quantity * ri.UnitPrice)
                .Sum();

            decimal totalRevenue = (retailRevenue ?? 0);
            ViewBag.TotalRevenue = totalRevenue;

            return View();
        }

        [HttpGet]
        [Route("products")]
        public IActionResult Products()
        {
            // Lấy dữ liệu thực từ database
            var products = _context.Products.Include(p => p.ProductImages).ToList();

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

            //if (!ModelState.IsValid)
            //{
            //    foreach (var key in ModelState.Keys)
            //    {
            //        var errors = ModelState[key].Errors;
            //        foreach (var error in errors)
            //        {
            //            Console.WriteLine($"Lỗi tại {key}: {error.ErrorMessage}");
            //        }
            //    }
            //    //ViewBag.Categories = await _context.ProductCategory
            //    //    .Select(c => new { c.Id, c.Name })
            //    //    .ToListAsync();

            //    //return View(model);
            //}

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

            // Cập nhật description trong bảng ProductImage
            //var image = product.ProductImages.FirstOrDefault();
            //if (image != null)
            //{
            //    image.Description = model.ProductImages.FirstOrDefault()?.Description ?? image.Description;
            //}

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction("Products");
            }
            catch (DbUpdateException ex)
            {
                ModelState.AddModelError("", "Lỗi khi cập nhật: " + ex.Message);
                return RedirectToAction("Products");
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
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Dữ liệu không hợp lệ.";
                return View(model); // Trả về lại form
            }

            var exist = _context.DiscountCodes.Any(c => c.Code == model.Code);
            if (exist)
            {
                TempData["Error"] = "Mã giảm giá đã tồn tại.";
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
        [Route("AdminRetail/UpdateCustomer/{id}")]
        public async Task<IActionResult> UpdateCustomer(int id)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 2) return View("404");

            var user = await _context.Users.FindAsync(id);
            if (user == null || user.RoleId != 3)
                return NotFound();

            return View("UpdateCustomer", user);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("UpdateCustomer/{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, string actionType, string? reason, string? newEmail, string? newPhone)
        {
            var staffId = HttpContext.Session.GetInt32("UserId");
            var staffRole = HttpContext.Session.GetInt32("RoleId");

            // Chỉ cho phép staff gửi yêu cầu
            if (staffId == null || staffRole != 2)
                return View("404");

            var customer = await _context.Users.FindAsync(id);
            if (customer == null || customer.RoleId != 3)
                return NotFound();

            string? content = actionType.ToLower() switch
            {
                "deactivate" => $"[YÊU CẦU] Nhân viên (ID: {staffId}) đề nghị KHÓA tài khoản khách hàng '{customer.Username}' (ID: {customer.Id}). Lý do: {reason}",
                "update" => $"[YÊU CẦU] Nhân viên (ID: {staffId}) đề nghị CẬP NHẬT thông tin cho khách hàng '{customer.Username}' (ID: {customer.Id}).\nEmail mới: {newEmail}, SĐT mới: {newPhone}. Lý do: {reason}",
                _ => null
            };

            if (content == null)
                return BadRequest("Loại hành động không hợp lệ.");

            // Gửi thông báo đến tất cả admin
            var admins = await _context.Users.Where(u => u.RoleId == 1).ToListAsync();
            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Content = content,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Yêu cầu cập nhật tài khoản đã được gửi tới quản trị viên.";
            return RedirectToAction("AllCustomers");
        }

        [HttpGet]
        [Route("AddCustomer")]
        public IActionResult AddCustomer()
        {
            // Chỉ staff mới có quyền gửi
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 2) return View("404");

            return View(); // trả về form để nhập thông tin customer
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("AddCustomer")]
        public async Task<IActionResult> AddCustomer(string username, string email, string password, string phone, string? reason)
        {
            var staffId = HttpContext.Session.GetInt32("UserId");
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (staffId == null || roleId != 2)
                return View("404");

            // Validate sơ bộ
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin bắt buộc.";
                return RedirectToAction("AddCustomer");
            }

            // Kiểm tra trùng username hoặc email
            if (await _context.Users.AnyAsync(u => u.Username == username))
            {
                TempData["Error"] = "Username đã tồn tại.";
                return RedirectToAction("AddCustomer");
            }
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                TempData["Error"] = "Email đã tồn tại.";
                return RedirectToAction("AddCustomer");
            }

            // Tạo nội dung thông báo gửi cho admin
            var content = $"[YÊU CẦU] Nhân viên (ID: {staffId}) đề nghị THÊM MỚI khách hàng:\n" +
                          $"- Username: {username}\n- Email: {email}\n- Phone: {phone}\nLý do: {reason}";

            // Gửi thông báo cho toàn bộ admin
            var admins = await _context.Users.Where(u => u.RoleId == 1).ToListAsync();
            foreach (var admin in admins)
            {
                _context.Notifications.Add(new Notification
                {
                    UserId = admin.Id,
                    Content = content,
                    CreatedAt = DateTime.Now,
                    IsRead = false
                });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Yêu cầu thêm khách hàng đã được gửi tới quản trị viên.";
            return RedirectToAction("AddCustomer");
        }

    }
}

