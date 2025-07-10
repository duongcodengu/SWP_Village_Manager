using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using Village_Manager.Extensions;
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
        [Route("EditCustomer/{id}")]
        public async Task<IActionResult> UpdateCustomer(int id)
        {
            var roleId = HttpContext.Session.GetInt32("RoleId");
            if (roleId != 2) return View("404");

            ViewBag.Roles = _context.Roles.Where(r => r.Id == 3).ToList();

            var user = await _context.Users.FindAsync(id);
            if (user == null || user.RoleId != 3)
                return NotFound();

            return View("EditCustomer", user);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("EditCustomer/{id}")]
        public async Task<IActionResult> EditCustomer(int id, string actionType, string? reason, string? newEmail, string? newPhone)
        {
            var staffRole = HttpContext.Session.GetInt32("RoleId");
            if (staffRole != 2)
                return View("404");

            var customer = await _context.Users.FindAsync(id);
            if (customer == null || customer.RoleId != 3)
                return NotFound();

            if (actionType == "update")
            {
                if (!string.IsNullOrWhiteSpace(newEmail)) customer.Email = newEmail.Trim();
                if (!string.IsNullOrWhiteSpace(newPhone)) customer.Phone = newPhone.Trim();
            }
            else if (actionType == "deactivate")
            {
                customer.IsActive = false;
            }
            else
            {
                TempData["Error"] = "Hành động không hợp lệ.";
                return RedirectToAction("EditCustomer", new { id });
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thông tin khách hàng đã được cập nhật thành công.";
            return RedirectToAction("AllCustomers");
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

            // Validate
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin bắt buộc.";
                return RedirectToAction("AddCustomer");
            }

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

            var newCustomer = new User
            {
                Username = username.Trim(),
                Email = email.Trim(),
                Password = password, // Nếu cần mã hóa thì dùng HashPassword(password)
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
            if ( roleId != 2)
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
    }
}

