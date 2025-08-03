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
            .Where(p => p.ApprovalStatus == "accepted")
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

        ViewBag.Categories = await _context.ProductCategories.Where(c => c.Active).ToListAsync();

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
        // Kiểm tra sản phẩm có được phép mua không
        var product = _context.Products.FirstOrDefault(p => p.Id == productId && p.ApprovalStatus == "accepted");
        if (product == null)
        {
            TempData["Error"] = "Sản phẩm này không còn khả dụng để mua.";
            return RedirectToAction("Search");
        }
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
        // Lưu thông tin mã giảm giá từ session vào TempData (nếu cần thiết)
        TempData["DiscountCode"] = HttpContext.Session.GetString("DiscountCode");
        TempData["DiscountAmount"] = HttpContext.Session.GetString("DiscountAmount");

        // Lấy giỏ hàng từ session
        var cartItems = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
        // Lọc chỉ giữ lại sản phẩm còn được phép mua
        cartItems = cartItems.Where(item =>
            _context.Products.Any(p => p.Id == item.ProductId && p.ApprovalStatus == "accepted")
        ).ToList();
        foreach (var item in cartItems)
        {
            item.Product = _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefault(p => p.Id == item.ProductId);

            // Đảm bảo hình ảnh chính
            DefaultImage.EnsureSingle(item.Product, _env);
        }

        // Lấy địa chỉ từ bảng Address (chỉ 1 bản ghi duy nhất)
        int? userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Home");
        }

        // Lấy tất cả địa chỉ của user từ UserLocation
        var addresses = _context.UserLocations
            .Where(a => a.UserId == userId.Value)
            .ToList();

        ViewBag.Addresses = addresses;

        // Gán mã giảm giá vào ViewBag
        ViewBag.DiscountAmount = HttpContext.Session.GetInt32("DiscountAmount") ?? 0;
        ViewBag.DiscountCode = HttpContext.Session.GetString("DiscountCode");

        // Trả về view với danh sách sản phẩm trong giỏ hàng
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
        
        // Lấy địa chỉ giao hàng từ session
        var shippingAddress = HttpContext.Session.GetString("ShippingAddress");
        ViewBag.ShippingAddress = shippingAddress;
        
        // Xóa địa chỉ khỏi session sau khi đã sử dụng
        HttpContext.Session.Remove("ShippingAddress");
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
    public async Task<IActionResult> PlaceOrder(int selectedAddress)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            Response.StatusCode = 404;
            return View("404");
        }

        // Kiểm tra địa chỉ được chọn
        var selectedUserLocation = await _context.UserLocations
            .FirstOrDefaultAsync(ul => ul.Id == selectedAddress && ul.UserId == userId.Value);
        
        if (selectedUserLocation == null)
        {
            TempData["Error"] = "Vui lòng chọn địa chỉ giao hàng hợp lệ.";
            return RedirectToAction("Checkout");
        }

        var cart = HttpContext.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
        if (cart == null || !cart.Any())
        {
            return RedirectToAction("search", "shop");
        }

        // Kiểm tra tồn kho trước
        foreach (var item in cart)
        {
            var product = await _context.Products.FindAsync(item.ProductId);
            if (product == null || product.Quantity < item.Quantity)
            {
                TempData["Error"] = $"Sản phẩm \"{product?.Name ?? "Không xác định"}\" không đủ hàng trong kho.";
                return RedirectToAction("cart", "shop");
            }
        }

        // Tạo đơn hàng với thông tin địa chỉ
        var newOrder = new RetailOrder
        {
            UserId = userId.Value,
            OrderDate = DateTime.Now,
            Status = "pending"
            // ShippingAddress = selectedUserLocation.Address // Tạm comment cho đến khi có migration
        };
        _context.RetailOrders.Add(newOrder);
        await _context.SaveChangesAsync(); // Lấy OrderId

        // Thêm chi tiết đơn hàng và cập nhật tồn kho
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

                // Trừ tồn kho
                product.Quantity -= (int)item.Quantity;

                // Kiểm tra tránh quantity âm (nếu có lỗi đồng bộ nào đó)
                if (product.Quantity < 0)
                {
                    TempData["Error"] = $"Lỗi: Sản phẩm \"{product.Name}\" bị thiếu hàng trong khi xử lý.";
                    return RedirectToAction("cart", "shop");
                }
            }
        }

        await _context.SaveChangesAsync();

        // Tạo record Delivery với thông tin địa chỉ giao hàng
        var delivery = new Delivery
        {
            OrderType = "retail",
            OrderId = newOrder.Id,
            Status = "assigned", // Đã phân công cho shipper
            CustomerName = selectedUserLocation.User?.Username ?? "Không xác định",
            CustomerAddress = selectedUserLocation.Address,
            CustomerPhone = selectedUserLocation.User?.Phone ?? "Không xác định",
            ShippingFee = 0 // Phí vận chuyển cố định
        };
        _context.Deliveries.Add(delivery);
        await _context.SaveChangesAsync();

        // Lưu địa chỉ giao hàng vào session
        HttpContext.Session.SetString("ShippingAddress", selectedUserLocation.Address);

        // Clear giỏ hàng
        HttpContext.Session.Remove("Cart");
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
            .Where(p => p.ApprovalStatus == "accepted")
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        // Lấy sản phẩm liên quan cùng category, loại trừ chính nó
        var relatedProducts = await _context.Products
            .Where(p => p.ApprovalStatus == "accepted" && p.CategoryId == product.CategoryId && p.Id != id)
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Take(6)
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
            .Where(p => p.ApprovalStatus == "accepted")
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return NotFound();

        // Lấy sản phẩm liên quan cùng category, loại trừ chính nó
        var relatedProducts = await _context.Products
            .Where(p => p.ApprovalStatus == "accepted" && p.CategoryId == product.CategoryId && p.Id != id)
            .Include(p => p.Category)
            .Include(p => p.ProductImages)
            .Take(6)
            .ToListAsync();

        ViewBag.RelatedProducts = relatedProducts;

        return View(product); // Truyền sản phẩm chính vào model, related qua ViewBag
    }


}
