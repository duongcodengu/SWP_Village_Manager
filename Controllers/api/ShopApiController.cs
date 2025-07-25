using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using Village_Manager.Models;
using Village_Manager.Utils;

namespace Village_Manager.Controllers.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShopApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ShopApiController(AppDbContext context)
        {
            _context = context;
        }

        // Lọc, sắp xếp sản phẩm
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts(
            string? search,
            [FromQuery] List<int> categoryIds,
            int? minPrice,
            int? maxPrice,
            string? sort)
        {
            var query = _context.Products
                .Where(p => p.ApprovalStatus == "accepted")
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.Name.Contains(search));

            if (categoryIds.Any())
                query = query.Where(p => categoryIds.Contains(p.CategoryId));

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            if (!string.IsNullOrWhiteSpace(sort))
            {
                query = sort switch
                {
                    "price_asc" => query.OrderBy(p => p.Price),
                    "price_desc" => query.OrderByDescending(p => p.Price),
                    "name_asc" => query.OrderBy(p => p.Name),
                    "name_desc" => query.OrderByDescending(p => p.Name),
                    _ => query
                };
            }

            var products = await query.ToListAsync();

            var result = products.Select(p => new
            {
                p.Id,
                p.Name,
                CategoryName = p.Category?.Name ?? "",
                p.Price,
                p.ProductType,
                Image = (p.ProductImages != null && p.ProductImages.Any())
                    ? p.ProductImages.First().ImageUrl
                    : "/images/product/default-product.png"
            });

            return Ok(result);
        }

        // Thêm vào giỏ hàng
        [HttpPost("add-to-cart")]
        public IActionResult AddToCart(int productId, int quantity)
        {
            var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            var existing = cart.FirstOrDefault(i => i.ProductId == productId);

            if (existing != null)
                existing.Quantity += quantity;
            else
                cart.Add(new CartItem { ProductId = productId, Quantity = quantity });

            HttpContext.Session.Set("Cart", cart);
            return Ok(new { success = true, count = cart.Sum(i => i.Quantity) });
        }
    }
}
