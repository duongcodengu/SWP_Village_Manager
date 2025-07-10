namespace Utils
{
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.AspNetCore.Hosting;
    using Village_Manager.Models;
    public static class DefaultImage
    {
        private const string DefaultImagePath = "/images/product/default-product.png";

        public static void Ensure(List<CartItem> cartItems, IWebHostEnvironment env)
        {
            foreach (var item in cartItems)
            {
                var img = item.Product?.ProductImages?.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(img?.ImageUrl) ||
                    !File.Exists(Path.Combine(env.WebRootPath, img.ImageUrl.TrimStart('/'))))
                {
                    item.Product.ProductImages = new List<ProductImage>
                {
                    new ProductImage { ImageUrl = DefaultImagePath }
                };
                }
            }
        }

        public static void EnsureSingle(Village_Manager.Models.Product product, IWebHostEnvironment env)
        {
            var img = product?.ProductImages?.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(img?.ImageUrl) ||
                !File.Exists(Path.Combine(env.WebRootPath, img.ImageUrl.TrimStart('/'))))
            {
                product.ProductImages = new List<ProductImage>
            {
                new ProductImage { ImageUrl = DefaultImagePath }
            };
            }
        }
    }
}