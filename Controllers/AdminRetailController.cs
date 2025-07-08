<<<<<<< HEAD
﻿using Microsoft.AspNetCore.Mvc;
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
=======
﻿using Azure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;

namespace Village_Manager.Controllers
{
    public class AdminRetailController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AdminRetailController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        [HttpGet]
        [Route("adminretail")]
>>>>>>> user
        public IActionResult Dashboard()
        {
            var username = HttpContext.Session.GetString("Username");
            var roleId = HttpContext.Session.GetInt32("RoleId");

<<<<<<< HEAD
            if (string.IsNullOrEmpty(username) || roleId != 1)
=======
            if (string.IsNullOrEmpty(username) || roleId != 4)
>>>>>>> user
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
            int totalWholesaleOrders = _context.WholesaleOrders.Count();
            int totalOrders = totalRetailOrders + totalWholesaleOrders;
            ViewBag.TotalOrders = totalOrders;

            // Lấy category (name, image_url)
            var categories = _context.ProductCategory
                .Select(c => new
                {
                    Name = c.Name,
                    ImageUrl = c.ImageUrl
                }).ToList<dynamic>();
            ViewBag.Categories = categories;

<<<<<<< HEAD
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

            // Bán buôn (Wholesale)
            var wholesaleRevenue = _context.WholesaleOrders
                .Where(wo => wo.Status == "confirmed"
                    && wo.ConfirmedAt.HasValue
                    && wo.ConfirmedAt.Value.Year == currentYear)
                .Join(_context.WholesaleOrderItems,
                      wo => wo.Id,
                      wi => wi.OrderId,
                      (wo, wi) => wi.Quantity * wi.UnitPrice)
                .Sum();

            decimal totalRevenue = (retailRevenue ?? 0) + (wholesaleRevenue ?? 0);
=======
            // Tổng doanh thu delivered
            decimal totalRevenue = 0;
            // RetailOrder
            var retailRevenue = (from ro in _context.RetailOrders
                                 where ro.Status == "delivered"
                                 join ri in _context.RetailOrderItems on ro.Id equals ri.OrderId
                                 select ri.Quantity * ri.UnitPrice).Sum();

            totalRevenue = retailRevenue ?? 0;
>>>>>>> user
            ViewBag.TotalRevenue = totalRevenue;

            return View();
        }

<<<<<<< HEAD
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

        [HttpGet]
        [Route("alluser")]
        public IActionResult AllUser() => View();

    }
}

=======
        // Hiển thị danh sách khách mua lẻ
        [HttpGet]
        [Route("allretailcustomers")]
        public IActionResult AllRetailCustomers()
        {
            var model = _context.RetailCustomers
                                .Include(rc => rc.User) 
                                .ToList();

            return View(model);
        }
    }
}
>>>>>>> user
