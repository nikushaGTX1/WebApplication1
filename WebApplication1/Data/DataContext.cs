using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        // Products table
        public DbSet<Api> Medicines { get; set; } = null!;

        // Users table
        public DbSet<User> Users { get; set; } = null!;

        // Wishlist table
        public DbSet<WishlistItem> WishList { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Primary Keys
            builder.Entity<Api>().HasKey(x => x.Id);
            builder.Entity<User>().HasKey(x => x.Id);
            builder.Entity<WishlistItem>().HasKey(x => x.Id);

            // Wishlist → User
            builder.Entity<WishlistItem>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Wishlist → Medicine
            builder.Entity<WishlistItem>()
                .HasOne(w => w.Api)
                .WithMany()
                .HasForeignKey(w => w.MedicineId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
