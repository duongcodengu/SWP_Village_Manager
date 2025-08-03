using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Data;
using Village_Manager.Models;
using Village_Manager.Utils;
using Village_Manager.Models.Dto;

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
            string? sort,
            int page = 1,
            int pageSize = 20)
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

            int totalItems = await query.CountAsync();
            var products = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

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

            return Ok(new { data = result, totalItems });
        }

        // Thêm vào giỏ hàng
        [HttpPost("add-to-cart")]
        public IActionResult AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
                var existing = cart.FirstOrDefault(i => i.ProductId == request.ProductId);

                if (existing != null)
                    existing.Quantity += request.Quantity;
                else
                    cart.Add(new CartItem { ProductId = request.ProductId, Quantity = request.Quantity });

                HttpContext.Session.Set("Cart", cart);
                
                // Lấy thông tin chi tiết về cart để trả về
                var cartWithProducts = CartHelper.GetCartWithProducts(HttpContext, _context);
                var total = cartWithProducts.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);
                
                return Ok(new { 
                    success = true, 
                    count = cart.Sum(i => i.Quantity),
                    total = total,
                    cartItems = cartWithProducts.Select(item => new {
                        productId = item.ProductId,
                        name = item.Product?.Name,
                        price = item.Product?.Price,
                        quantity = item.Quantity,
                        image = item.Product?.ProductImages?.FirstOrDefault()?.ImageUrl ?? "/images/product/default-product.png"
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Lấy thông tin giỏ hàng
        [HttpGet("cart-info")]
        public IActionResult GetCartInfo()
        {
            try
            {
                var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
                var cartWithProducts = CartHelper.GetCartWithProducts(HttpContext, _context);
                var total = cartWithProducts.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);
                
                return Ok(new { 
                    count = cart.Sum(i => i.Quantity),
                    total = total,
                    cartItems = cartWithProducts.Select(item => new {
                        productId = item.ProductId,
                        name = item.Product?.Name,
                        price = item.Product?.Price,
                        quantity = item.Quantity,
                        image = item.Product?.ProductImages?.FirstOrDefault()?.ImageUrl ?? "/images/product/default-product.png"
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // Xóa sản phẩm khỏi giỏ hàng
        [HttpDelete("remove-from-cart/{productId}")]
        public IActionResult RemoveFromCart(int productId)
        {
            try
            {
                var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
                var itemToRemove = cart.FirstOrDefault(i => i.ProductId == productId);
                
                if (itemToRemove != null)
                {
                    cart.Remove(itemToRemove);
                    HttpContext.Session.Set("Cart", cart);
                }
                
                // Lấy thông tin chi tiết về cart để trả về
                var cartWithProducts = CartHelper.GetCartWithProducts(HttpContext, _context);
                var total = cartWithProducts.Sum(i => (i.Product?.Price ?? 0) * i.Quantity);
                
                return Ok(new { 
                    success = true, 
                    count = cart.Sum(i => i.Quantity),
                    total = total,
                    cartItems = cartWithProducts.Select(item => new {
                        productId = item.ProductId,
                        name = item.Product?.Name,
                        price = item.Product?.Price,
                        quantity = item.Quantity,
                        image = item.Product?.ProductImages?.FirstOrDefault()?.ImageUrl ?? "/images/product/default-product.png"
                    })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}
