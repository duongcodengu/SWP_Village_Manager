using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Utils;
using Village_Manager.Data;
using Village_Manager.Models;

namespace Village_Manager.Utils
{
    public static class CartHelper
    {
        public static List<CartItem> GetCartWithProducts(HttpContext context, AppDbContext db)
        {
            var cartSession = context.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            var productIds = cartSession.Select(c => c.ProductId).ToList();
            var cartItems = new List<CartItem>();

            if (productIds.Any())
            {
                var products = db.Products
                    .Where(p => productIds.Contains(p.Id) && p.ApprovalStatus == "accepted")
                    .Include(p => p.ProductImages)
                    .ToList();

                foreach (var item in cartSession)
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (product != null)
                    {
                        cartItems.Add(new CartItem
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Product = product
                        });
                    }
                }

                var env = context.RequestServices.GetService(typeof(IWebHostEnvironment)) as IWebHostEnvironment;
                if (env != null)
                {
                    DefaultImage.Ensure(cartItems, env);
                }
            }

            return cartItems;
        }
        public static int GetCartCount(HttpContext context)
        {
            var cartSession = context.Session.Get<List<CartItem>>("Cart") ?? new List<CartItem>();
            return cartSession.Sum(i => i.Quantity ?? 0);
        }
    }
}