using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using Utils;
using Village_Manager.Data;
using Village_Manager.Models;
using Village_Manager.Utils;

public class ShopController : Controller
{
    private readonly AppDbContext _context;
    private readonly IWebHostEnvironment _env;
    public ShopController(AppDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // Hiển thị sản phẩm + tìm kiếm + lịch sử tìm kiếm
    public async Task<IActionResult> Search(
    string? search,
    List<int>? categoryIds,
    int? minPrice,
    int? maxPrice,
    string? remove,
    string? clear,
    string? sort)
    {
        // Cart info

        // Search history
        const string sessionKey = "SearchHistory";
        var history = HttpContext.Session.Get<List<string>>(sessionKey) ?? new List<string>();
        if (!string.IsNullOrWhiteSpace(clear)) history.Clear();
        else if (!string.IsNullOrWhiteSpace(remove)) history.Remove(remove);
        else if (!string.IsNullOrWhiteSpace(search) && !history.Contains(search)) history.Add(search);
        HttpContext.Session.Set(sessionKey, history);
        ViewBag.SearchHistory = history;

        // Base query
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .AsQueryable();

        // Filter logic
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p => p.Name.Contains(search));

        if (categoryIds?.Any() == true)
            query = query.Where(p => categoryIds.Contains(p.CategoryId));         // FK từ Product → ProductCategory

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        //Sort
        if (!string.IsNullOrWhiteSpace(sort))
        {
            switch (sort)
            {
                case "price_asc":
                    query = query.OrderBy(p => p.Price);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(p => p.Price);
                    break;
                case "name_asc":
                    query = query.OrderBy(p => p.Name);
                    break;
                case "name_desc":
                    query = query.OrderByDescending(p => p.Name);
                    break;
            }
        }

        ViewBag.Categories = await _context.ProductCategories.ToListAsync();

        var products = await query.ToListAsync();

        foreach (var p in products)
        {
            DefaultImage.EnsureSingle(p, _env);
        }
        return View("Search", products);
    }

    // Thêm vào giỏ hàng
    [HttpPost]
    public IActionResult AddToCart(int productId, int quantity)
    {
        //lấy giỏ hàng từ session hoặc tạo mới
        var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();

        //kiểm tra sản phẩm đã có trong giỏ chưa
        var item = cart.FirstOrDefault(ci => ci.ProductId == productId);
        if (item != null)
        {
            item.Quantity = (item.Quantity ?? 0) + quantity;
        }
        else
        {
            cart.Add(new CartItem
            {
                ProductId = productId,
                Quantity = quantity
            });
        }

        HttpContext.Session.Set("Cart", cart);

        return RedirectToAction("Search");
    }

    // Trang giỏ hàng
    [HttpGet]
    public IActionResult Cart()
    {
        var cartItems = CartHelper.GetCartWithProducts(HttpContext, _context);
        DefaultImage.Ensure(cartItems, _env);
        return View(cartItems);
    }

    [HttpGet("shop/removefromcart/{productId}")]
    public IActionResult RemoveFromCart(int productId)
    {
        var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
        var item = cart.FirstOrDefault(i => i.ProductId == productId);
        if (item != null)
        {
            cart.Remove(item);
            HttpContext.Session.Set("Cart", cart);
        }

        // Điều hướng quay về đúng trang (popup hoặc cart)
        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer))
            return Redirect(referer);
        return RedirectToAction("Cart");
    }

    public IActionResult Checkout()
    {
        TempData["DiscountCode"] = HttpContext.Session.GetString("DiscountCode");
        TempData["DiscountAmount"] = HttpContext.Session.GetString("DiscountAmount");
        // Lấy giỏ hàng từ Session
        var cartItems = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
        foreach (var item in cartItems)
        {
            item.Product = _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefault(p => p.Id == item.ProductId);
            DefaultImage.EnsureSingle(item.Product, _env);
        }
        // Lấy địa chỉ của user từ Session
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }
        var locations = _context.UserLocations
            .Where(l => l.UserId == userId.Value)
            .ToList();
        ViewBag.Location = locations;

        ViewBag.DiscountAmount = HttpContext.Session.GetInt32("DiscountAmount") ?? 0;
        ViewBag.DiscountCode = HttpContext.Session.GetString("DiscountCode");

        return View(cartItems);
    }

    [HttpGet]
    [Route("shop/success")]
    public IActionResult Success()
    {
        // Lấy OrderId từ TempData
        int orderId = TempData["OrderId"] != null ? (int)TempData["OrderId"] : 0;
        if (orderId == 0)
        {
            return RedirectToAction("Search", "Shop");
        }

        // Lấy đơn hàng cùng chi tiết sản phẩm từ DB
        var order = _context.RetailOrders
            .Include(o => o.RetailOrderItems)
                .ThenInclude(oi => oi.Product)
                .ThenInclude(p => p.ProductImages)
            .FirstOrDefault(o => o.Id == orderId);

        if (order == null)
        {
            return RedirectToAction("Search", "Shop");
        }

        HttpContext.Session.Remove("Cart");

        int userId = order.UserId.Value;
        var address = _context.UserLocations
            .Include(u => u.User)
            .Where(a => a.UserId == userId)
            .FirstOrDefault();

        ViewBag.Address = address;
        ViewBag.OrderId = order.Id;
        ViewBag.PaymentMethod = "Tiền mặt";
        ViewBag.DiscountAmount = HttpContext.Session.GetInt32("DiscountAmount") ?? 0;

        // Chuẩn bị model: danh sách CartItem tương ứng chi tiết đơn hàng
        var cartItems = order.RetailOrderItems.Select(oi => new CartItem
        {
            ProductId = oi.ProductId,
            Quantity = oi.Quantity,
            Product = oi.Product
        }).ToList();

        DefaultImage.Ensure(cartItems, _env);

        return View(cartItems);
    }

    //place order
    [HttpPost("/shop/place-order")]
    public async Task<IActionResult> PlaceOrder()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            TempData["Error"] = "Bạn chưa đăng nhập.";
            return RedirectToAction("", "login");
        }

        var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
        if (cart == null || !cart.Any())
        {
            TempData["Error"] = "Bạn chưa có hàng trong giỏ hàng.";
            return RedirectToAction("search", "shop");
        }

        // Tạo đơn hàng
        var newOrder = new RetailOrder
        {
            UserId = userId.Value,
            OrderDate = DateTime.Now,
            Status = "pending"
        };
        _context.RetailOrders.Add(newOrder);
        await _context.SaveChangesAsync(); // để lấy được OrderId

        // Thêm chi tiết đơn hàng
        foreach (var item in cart)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                var orderItem = new RetailOrderItem
                {
                    OrderId = newOrder.Id,
                    ProductId = product.Id,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                };
                _context.RetailOrderItems.Add(orderItem);
            }
        }

        await _context.SaveChangesAsync();

        // Lưu OrderId để hiển thị ở trang thành công
        TempData["OrderId"] = newOrder.Id;

        return RedirectToAction("Success");
    }

    public IActionResult Tracking(string orderId)
    {
        // Có thể dùng ViewBag.OrderId = orderId; nếu muốn
        return View();
    }

    public async Task<IActionResult> Detail(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        // Lấy sản phẩm liên quan cùng category, loại trừ chính nó
        var relatedProducts = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
            .Take(6) // Giới hạn 6 sản phẩm liên quan
            .ToListAsync();

        ViewBag.RelatedProducts = relatedProducts;

        return View(product); // Truyền sản phẩm chính vào model, related qua ViewBag
    }
    [HttpGet("top4-selling")]
    public async Task<IActionResult> GetTopSelling()
    {
        var result = await _context.RetailOrderItems
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                ProductName = g.First().Product.Name,
                ImageUrl = g.First().Product.ProductImages.FirstOrDefault().ImageUrl,
                TotalSold = g.Sum(x => x.Quantity * x.UnitPrice)
            })
            .OrderByDescending(x => x.TotalSold)
            .Take(4)
            .ToListAsync();

        return Ok(result);
    }

    public async Task<IActionResult> RelateProduct(int id)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        // Lấy sản phẩm liên quan cùng category, loại trừ chính nó
        var relatedProducts = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
            .Take(6) // Giới hạn 6 sản phẩm liên quan
            .ToListAsync();

        ViewBag.RelatedProducts = relatedProducts;

        return View(product); // Truyền sản phẩm chính vào model, related qua ViewBag
    }


}
