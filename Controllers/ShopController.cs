using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            .Include(p => p.Category)          // liên kết với ProductCategory
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
        // B1: lấy giỏ hàng từ session hoặc tạo mới
        var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();

        // B2: kiểm tra sản phẩm đã có trong giỏ chưa
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

        // B3: lưu lại danh sách đơn giản (không navigation)
        HttpContext.Session.Set("Cart", cart);

        return RedirectToAction("Search");
    }

    // Trang giỏ hàng
    [HttpGet]
    public IActionResult Cart()
    {
        var cartItems = CartHelper.GetCartWithProducts(HttpContext, _context);
        DefaultImage.Ensure(cartItems, _env);
        return View(cartItems); // model là List<CartItem>
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
        // Lấy giỏ hàng từ Session
        var cartItems = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
        foreach (var item in cartItems)
        {
            item.Product = _context.Products
            .Include(p => p.ProductImages)
            .FirstOrDefault(p => p.Id == item.ProductId);
            DefaultImage.EnsureSingle(item.Product, _env);
        }
        return View(cartItems);
    }

    public IActionResult PlaceOrder()
    {
        // Xoá giỏ hàng:
        HttpContext.Session.Remove("Cart");
        // Điều hướng đến trang thành công
        return RedirectToAction("Success");
    }

    public IActionResult Success()
    {
        // Hiển thị trang thành công
        return View();
    }

    public IActionResult Tracking(string orderId)
    {
        // Có thể dùng ViewBag.OrderId = orderId; nếu muốn
        return View();
    }

    public IActionResult Detail(int id)
    {
        var product = _context.Products
        .Include(p => p.ProductImages)
        .Include(p => p.Category)
        .FirstOrDefault(p => p.Id == id);

        if (product == null)
            return NotFound();
        DefaultImage.EnsureSingle(product, _env);

        return View(product);
    }
}
