using Microsoft.EntityFrameworkCore;
using Village_Manager.Models;

namespace Village_Manager.Data
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options)
        {
        }
        // Define DbSet properties for your entities here, e.g.:
        // public DbSet<YourEntity> YourEntities { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<WholesaleOrder> WholesaleOrders { get; set; }
        public DbSet<RetailOrder> RetailOrders { get; set; }
    }
}
