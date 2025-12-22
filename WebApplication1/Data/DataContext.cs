using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class DataContext : DbContext
    {
        public DbSet<Api> Medicines { get; set; }  // Products table
        public DbSet<User> Users { get; set; }     // Users table
        public DbSet<WishlistItem> WishList { get; set; }  // Fix type here

        public DataContext(DbContextOptions<DataContext> options) : base(options) { }
    }
}
