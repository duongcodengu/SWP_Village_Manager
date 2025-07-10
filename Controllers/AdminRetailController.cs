
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


        //dashboeard bắt đầu
        // kiểm tra quyền truy cập


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
        [Route("productsRetail")]
        public async Task<IActionResult> Products(string sortOrder, string keyword, int page = 1, int pageSize = 10)
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
                case "name_desc": query = query.OrderByDescending(p => p.Name); break;
                case "price_asc": query = query.OrderBy(p => p.Price); break;
                case "price_desc": query = query.OrderByDescending(p => p.Price); break;
                case "expiry_asc": query = query.OrderBy(p => p.ExpirationDate); break;
                default: query = query.OrderBy(p => p.Name); break;
            }

            int total = await query.CountAsync();
            var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)total / pageSize);
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
        public async Task<IActionResult> HiddenProducts()
        {
            // Lấy danh sách sản phẩm có trong bảng HiddenProduct
            var hiddenProductIds = await _context.HiddenProduct
                .Select(h => h.ProductId)
                .ToListAsync();

            // Lấy thông tin chi tiết sản phẩm tương ứng
            var products = await _context.Products
                .Include(p => p.ProductImages)
                .Where(p => hiddenProductIds.Contains(p.Id))
                .ToListAsync();

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

        [HttpGet]
        [Route("alluser")]
        public IActionResult AllUser() => View();

    }
}
