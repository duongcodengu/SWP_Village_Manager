using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Village_Manager.Models;

namespace Village_Manager.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
   


    public virtual DbSet<Address> Addresses { get; set; }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Delivery> Deliveries { get; set; }

    public virtual DbSet<Farmer> Farmers { get; set; }

    public virtual DbSet<FarmerRegistrationRequest> FarmerRegistrationRequests { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<ImportInvoice> ImportInvoices { get; set; }

    public virtual DbSet<ImportInvoiceDetail> ImportInvoiceDetails { get; set; }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<ProcessedProduct> ProcessedProducts { get; set; }

    public virtual DbSet<ProcessingOrder> ProcessingOrders { get; set; }

    public virtual DbSet<ProcessingReceipt> ProcessingReceipts { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<ProductCategory> ProductCategory { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductProcessingHistory> ProductProcessingHistories { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<RetailOrder> RetailOrders { get; set; }

    public virtual DbSet<RetailOrderItem> RetailOrderItems { get; set; }

    public virtual DbSet<ReturnOrder> ReturnOrders { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<Shipper> Shippers { get; set; }

    public virtual DbSet<ShipperRegistrationRequest> ShipperRegistrationRequests { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<Stock> Stocks { get; set; }

    public virtual DbSet<Supplier> Suppliers { get; set; }

    public virtual DbSet<SupplyRequest> SupplyRequests { get; set; }

    public virtual DbSet<TransportRoute> TransportRoutes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserLocation> UserLocations { get; set; }
    public virtual DbSet<Warehouse> Warehouses { get; set; }
    public virtual DbSet<DeliveryIssue> DeliveryIssues { get; set; }

    public virtual DbSet<DeliveryProof> DeliveryProofs { get; set; }

    public virtual DbSet<ContactMessages> ContactMessages { get; set; }

    public virtual DbSet<DiscountCodes> DiscountCodes { get; set; }

    public DbSet<HiddenProduct> HiddenProduct { get; set; }
    public virtual DbSet<HomepageImage> HomepageImages { get; set; }

    public DbSet<ChatMessages> ChatMessages { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<HomepageImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__HomepageImage__Id");

            entity.ToTable("HomepageImage");

            entity.Property(e => e.Id)
                .HasColumnName("id");

            entity.Property(e => e.ProductImageId)
                .HasColumnName("product_image_id")
                .IsRequired(false); // Cho phép NULL

            entity.Property(e => e.Section)
                .HasMaxLength(50)
                .IsRequired()
                .HasColumnName("section");

            entity.Property(e => e.DisplayOrder)
                .HasColumnName("display_order")
                .HasDefaultValue(0);

            entity.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            entity.Property(e => e.Banner)
                .HasMaxLength(500)
                .IsRequired(false)
                .HasColumnName("Banner");

            entity.Property(e => e.Position)
                .HasMaxLength(50)
                .IsRequired(false)
                .HasColumnName("Position");

            // Cấu hình relationship với ProductImage
            entity.HasOne(e => e.ProductImage)
                .WithMany()
                .HasForeignKey(e => e.ProductImageId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Address__3213E83F70EB8CBA");

            entity.ToTable("Address");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AddressLine)
                .HasColumnType("text")
                .HasColumnName("address_line");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.PostalCode)
                .HasMaxLength(20)
                .HasColumnName("postal_code");
            entity.Property(e => e.Province)
                .HasMaxLength(100)
                .HasColumnName("province");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Addresses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Address__user_id__02FC7413");
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Cart__3213E83F999BEB3E");

            entity.ToTable("Cart");

            entity.HasIndex(e => e.UserId, "UQ__Cart__B9BE370E972242E4").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.Cart)
                .HasForeignKey<Cart>(d => d.UserId)
                .HasConstraintName("FK__Cart__user_id__693CA210");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__CartItem__3213E83F7EB68196");

            entity.ToTable("CartItem");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CartId).HasColumnName("cart_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK__CartItem__cart_i__6C190EBB");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__CartItem__produc__6D0D32F4");
        });

        modelBuilder.Entity<Delivery>(entity =>
        {
            entity.ToTable("Delivery");

            entity.HasKey(e => e.Id)
                .HasName("PK__Delivery__3213E83FF10F0108");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.Property(e => e.OrderType)
                .HasMaxLength(10)
                .HasColumnName("order_type");

            entity.Property(e => e.OrderId)
                .HasColumnName("order_id");

            entity.Property(e => e.ShipperId)
                .HasColumnName("shipper_id");

            entity.Property(e => e.ShippingFee)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("shipping_fee");

            entity.Property(e => e.StartTime)
                .HasColumnType("datetime")
                .HasColumnName("start_time");

            entity.Property(e => e.EndTime)
                .HasColumnType("datetime")
                .HasColumnName("end_time");

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.Property(e => e.CustomerName)
                .HasMaxLength(100)
                .HasColumnName("customer_name");

            entity.Property(e => e.CustomerAddress)
                .HasMaxLength(255)
                .HasColumnName("customer_address");

            entity.Property(e => e.CustomerPhone)
                .HasMaxLength(20)
                .HasColumnName("customer_phone");

            entity.HasOne(d => d.Shipper)
                .WithMany(p => p.Deliveries)
                .HasForeignKey(d => d.ShipperId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Delivery__shipper_id");

            entity.HasOne(d => d.RetailOrder)
                .WithMany()
                .HasForeignKey(d => d.OrderId)
                .HasPrincipalKey(r => r.Id)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });


        modelBuilder.Entity<Farmer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Farmer__3213E83FA07F6285");

            entity.ToTable("Farmer");

            entity.HasIndex(e => e.Phone, "UQ__Farmer__B43B145F0A47026A").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasColumnType("text")
                .HasColumnName("address");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Farmers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Farmer__user_id__440B1D61");
        });

        modelBuilder.Entity<FarmerRegistrationRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__FarmerRe__3213E83FADE0B635");

            entity.ToTable("FarmerRegistrationRequest");

            entity.HasIndex(e => e.Phone, "UQ__FarmerRe__B43B145F88ECEB90").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasColumnType("text")
                .HasColumnName("address");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("requested_at");
            entity.Property(e => e.ReviewedAt)
                .HasColumnType("datetime")
                .HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.FarmerRegistrationRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK__FarmerReg__revie__2FCF1A8A");

            entity.HasOne(d => d.User).WithMany(p => p.FarmerRegistrationRequestUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__FarmerReg__user___2EDAF651");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Feedback__3213E83F58004605");

            entity.ToTable("Feedback");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderType)
                .HasMaxLength(10)
                .HasColumnName("order_type");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Feedback__user_i__00200768");
        });

        modelBuilder.Entity<ImportInvoice>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ImportIn__3213E83F46781BFF");

            entity.ToTable("ImportInvoice");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.PurchaseTime)
                .HasColumnType("datetime")
                .HasColumnName("purchase_time");
            entity.Property(e => e.SupplierName)
                .HasMaxLength(100)
                .HasColumnName("supplier_name");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("total_amount");
        });

        modelBuilder.Entity<ImportInvoiceDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ImportIn__3213E83F5F2A88E9");

            entity.ToTable("ImportInvoiceDetail");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ImportInvoiceId).HasColumnName("import_invoice_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.ImportInvoice).WithMany(p => p.ImportInvoiceDetails)
                .HasForeignKey(d => d.ImportInvoiceId)
                .HasConstraintName("FK__ImportInv__impor__5CD6CB2B");

            entity.HasOne(d => d.Product).WithMany(p => p.ImportInvoiceDetails)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ImportInv__produ__5DCAEF64");
        });

        modelBuilder.Entity<Log>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Log__3213E83F3E64BAE8");

            entity.ToTable("Log");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Action)
                .HasColumnType("text")
                .HasColumnName("action");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Logs)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Log__user_id__114A936A");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3213E83F0593F2CC");

            entity.ToTable("Notification");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Notificat__user___0E6E26BF");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Payment__3213E83F43D8BF43");

            entity.ToTable("Payment");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.Method)
                .HasMaxLength(50)
                .HasColumnName("method");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderType)
                .HasMaxLength(10)
                .HasColumnName("order_type");
            entity.Property(e => e.PaidAt)
                .HasColumnType("datetime")
                .HasColumnName("paid_at");
            entity.Property(e => e.PaymentType)
                .HasMaxLength(10)
                .HasColumnName("payment_type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Payments)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Payment__payment__08B54D69");
        });

        modelBuilder.Entity<ProcessedProduct>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Processe__3213E83FFCA60AF7");

            entity.ToTable("ProcessedProduct");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.HistoryId).HasColumnName("history_id");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("image_url");
            entity.Property(e => e.ProcessedDate).HasColumnName("processed_date");
            entity.Property(e => e.ProcessingNote)
                .HasColumnType("text")
                .HasColumnName("processing_note");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.ReturnDate).HasColumnName("return_date");
            entity.Property(e => e.TotalWeight)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_weight");

            entity.HasOne(d => d.History).WithMany(p => p.ProcessedProducts)
                .HasForeignKey(d => d.HistoryId)
                .HasConstraintName("FK__Processed__histo__7C4F7684");
        });

        modelBuilder.Entity<ProcessingOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Processi__3213E83F6F5CFA0E");

            entity.ToTable("ProcessingOrder");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExpectedReturnDate).HasColumnName("expected_return_date");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.SendDate).HasColumnName("send_date");

            entity.HasOne(d => d.Product).WithMany(p => p.ProcessingOrders)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Processin__produ__73BA3083");
        });

        modelBuilder.Entity<ProcessingReceipt>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Processi__3213E83F41337C2B");

            entity.ToTable("ProcessingReceipt");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ActualQuantity).HasColumnName("actual_quantity");
            entity.Property(e => e.ProcessingOrderId).HasColumnName("processing_order_id");
            entity.Property(e => e.ReceivedDate).HasColumnName("received_date");

            entity.HasOne(d => d.ProcessingOrder).WithMany(p => p.ProcessingReceipts)
                .HasForeignKey(d => d.ProcessingOrderId)
                .HasConstraintName("FK__Processin__proce__76969D2E");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Product__3213E83F7444950A");

            entity.ToTable("Product");

            entity.HasIndex(e => e.Name, "UQ__Product__72E12F1B4A7EF3F4").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ExpirationDate).HasColumnName("expiration_date");
            entity.Property(e => e.FarmerId).HasColumnName("farmer_id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProcessingTime).HasColumnName("processing_time");
            entity.Property(e => e.ProductType)
                .HasMaxLength(20)
                .HasColumnName("product_type");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Product__categor__5070F446");

            entity.HasOne(d => d.Farmer).WithMany(p => p.Products)
                .HasForeignKey(d => d.FarmerId)
                .HasConstraintName("FK__Product__farmer___5165187F");
        });

        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProductC__3213E83F07D45EBD");

            entity.ToTable("ProductCategory");

            entity.HasIndex(e => e.Name, "UQ__ProductC__72E12F1B4CF748D6").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Active) // thêm phần này
         .HasColumnName("active")
         .HasDefaultValue(true);
        });

        modelBuilder.Entity<ProductImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProductI__3213E83F9B55A56E");

            entity.ToTable("ProductImage");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(255)
                .HasColumnName("image_url");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("uploaded_at");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductImages)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ProductIm__produ__5535A963");
        });

        modelBuilder.Entity<ProductProcessingHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProductP__3213E83FBE9032B7");

            entity.ToTable("ProductProcessingHistory");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.ReturnDate).HasColumnName("return_date");
            entity.Property(e => e.SentDate).HasColumnName("sent_date");

            entity.HasOne(d => d.Product).WithMany(p => p.ProductProcessingHistories)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__ProductPr__produ__797309D9");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Report__3213E83FF797FAEC");

            entity.ToTable("Report");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content)
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Title)
                .HasMaxLength(100)
                .HasColumnName("title");
        });

        modelBuilder.Entity<RetailOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RetailOr__3213E83F4A3A25D1");

            entity.ToTable("RetailOrder");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ConfirmedAt)
                .HasColumnType("datetime")
                .HasColumnName("confirmed_at");
            entity.Property(e => e.OrderDate)
                .HasColumnType("datetime")
                .HasColumnName("order_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.RetailOrders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__RetailOrd__user___619B8048");
        });

        modelBuilder.Entity<RetailOrderItem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RetailOr__3213E83FFD4429ED");

            entity.ToTable("RetailOrderItem");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.Order).WithMany(p => p.RetailOrderItems)
                .HasForeignKey(d => d.OrderId)
                .HasConstraintName("FK__RetailOrd__order__6477ECF3");

            entity.HasOne(d => d.Product).WithMany(p => p.RetailOrderItems)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__RetailOrd__produ__656C112C");
        });

        modelBuilder.Entity<ReturnOrder>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ReturnOr__3213E83F31B10B09");

            entity.ToTable("ReturnOrder");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.OrderType)
                .HasMaxLength(10)
                .HasColumnName("order_type");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Reason)
                .HasColumnType("text")
                .HasColumnName("reason");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ImageUrl)
        .HasColumnName("image_url")
        .HasColumnType("nvarchar(max)");

            entity.HasOne(d => d.User).WithMany(p => p.ReturnOrders)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__ReturnOrd__user___1F98B2C1");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Roles__3213E83F403C7334");

            entity.HasIndex(e => e.Name, "UQ__Roles__72E12F1BE251CB2B").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Session__3213E83F484B900A");

            entity.ToTable("Session");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.SessionToken)
                .HasColumnType("text")
                .HasColumnName("session_token");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Session__user_id__14270015");
        });

        modelBuilder.Entity<Shipper>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Shipper__3213E83F657ABAE9");

            entity.ToTable("Shipper");

            entity.HasIndex(e => e.Phone, "UQ__Shipper__B43B145F9B3AB25F").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VehicleInfo)
                .HasColumnType("text")
                .HasColumnName("vehicle_info");

            entity.HasOne(d => d.User).WithMany(p => p.Shippers)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Shipper__user_id__48CFD27E");
        });

        modelBuilder.Entity<ShipperRegistrationRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ShipperR__3213E83F01E6DC46");

            entity.ToTable("ShipperRegistrationRequest");

            entity.HasIndex(e => e.Phone, "UQ__ShipperR__B43B145F8C480E01").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Address)
                .HasColumnType("text")
                .HasColumnName("address");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("requested_at");
            entity.Property(e => e.ReviewedAt)
                .HasColumnType("datetime")
                .HasColumnName("reviewed_at");
            entity.Property(e => e.ReviewedBy).HasColumnName("reviewed_by");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VehicleInfo)
                .HasColumnType("text")
                .HasColumnName("vehicle_info");

            entity.HasOne(d => d.ReviewedByNavigation).WithMany(p => p.ShipperRegistrationRequestReviewedByNavigations)
                .HasForeignKey(d => d.ReviewedBy)
                .HasConstraintName("FK__ShipperRe__revie__3F115E1A");

            entity.HasOne(d => d.User).WithMany(p => p.ShipperRegistrationRequestUsers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ShipperRe__user___3E1D39E1");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Staff__3213E83FB9F743E6");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Role)
                .HasMaxLength(100)
                .HasColumnName("role");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Staff)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Staff__user_id__18EBB532");
        });

        modelBuilder.Entity<Stock>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Stock__3213E83FAB6B186D");

            entity.ToTable("Stock");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LastUpdated)
                .HasColumnType("datetime")
                .HasColumnName("last_updated");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");

            entity.HasOne(d => d.Product).WithMany(p => p.Stocks)
                .HasForeignKey(d => d.ProductId)
                .HasConstraintName("FK__Stock__product_i__5812160E");
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Supplier__3213E83FB27F2159");

            entity.ToTable("Supplier");

            entity.HasIndex(e => e.Name, "UQ__Supplier__72E12F1B855DE082").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ContactInfo)
                .HasColumnType("text")
                .HasColumnName("contact_info");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<SupplyRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SupplyRe__3213E83F4F7A8494");

            entity.ToTable("SupplyRequest");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FarmerId).HasColumnName("farmer_id");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.ProductName)
                .HasMaxLength(100)
                .HasColumnName("product_name");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.ReceiverId).HasColumnName("receiver_id");
            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("requested_at");
            entity.Property(e => e.RequesterId).HasColumnName("requester_id");
            entity.Property(e => e.RequesterType)
                .HasMaxLength(10)
                .HasColumnName("requester_type");
            entity.Property(e => e.RespondedAt)
                .HasColumnType("datetime")
                .HasColumnName("responded_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("pending")
                .HasColumnName("status");

            entity.HasOne(d => d.Farmer).WithMany(p => p.SupplyRequests)
                .HasForeignKey(d => d.FarmerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SupplyReq__farme__282DF8C2");

            entity.HasOne(d => d.Receiver).WithMany(p => p.SupplyRequestReceivers)
                .HasForeignKey(d => d.ReceiverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SupplyReq__recei__2739D489");

            entity.HasOne(d => d.Requester).WithMany(p => p.SupplyRequestRequesters)
                .HasForeignKey(d => d.RequesterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__SupplyReq__reque__2645B050");
        });

        modelBuilder.Entity<TransportRoute>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Transpor__3213E83F797EED7A");

            entity.ToTable("TransportRoute");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DistanceKm)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("distance_km");
            entity.Property(e => e.EndLocation)
                .HasColumnType("text")
                .HasColumnName("end_location");
            entity.Property(e => e.EstimatedTime)
                .HasMaxLength(50)
                .HasColumnName("estimated_time");
            entity.Property(e => e.StartLocation)
                .HasColumnType("text")
                .HasColumnName("start_location");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Users__3213E83FA5F5900D");

            entity.HasIndex(e => e.Phone, "UQ__Users__5C7E359EB517686A").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E6164F06F44EC").IsUnique();

            entity.HasIndex(e => e.Username, "UQ__Users__F3DBC572DC2431C6").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.Phone).HasMaxLength(10);
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.Username)
                .HasMaxLength(100)
                .HasColumnName("username");
            entity.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);
            entity.Property(e => e.DeletedAt)
            .HasColumnName("deleted_at")
            .IsRequired(false);
                    entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__role_id__3F466844");
        });

        // ... existing code ...
        modelBuilder.Entity<DeliveryProof>(entity =>
        {
            entity.ToTable("DeliveryProofs");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeliveryId).HasColumnName("delivery_id");
            entity.Property(e => e.ShipperId).HasColumnName("shipper_id");
            entity.Property(e => e.ImagePath).HasColumnName("image_path");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });
        // ... existing code ...

        modelBuilder.Entity<UserLocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__UserLoca__3214EC07B578FC35");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Label).HasMaxLength(100);

            entity.HasOne(d => d.User).WithMany(p => p.UserLocations)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__UserLocat__UserI__339FAB6E");
        });

        modelBuilder.Entity<DeliveryProof>(entity =>
        {
            entity.ToTable("DeliveryProofs");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DeliveryId).HasColumnName("delivery_id");
            entity.Property(e => e.ShipperId).HasColumnName("shipper_id");
            entity.Property(e => e.ImagePath).HasColumnName("image_path");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<HiddenProduct>(entity =>
        {
            entity.ToTable("HiddenProduct");

            entity.Property(h => h.Reason)
                  .HasColumnType("TEXT");

            entity.Property(h => h.HiddenAt)
                  .HasDefaultValueSql("GETDATE()");

            entity.HasOne(h => h.Product)
                  .WithMany()
                  .HasForeignKey(h => h.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Global query filter để tự động lọc user đã soft delete
        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}