using InvoiceApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InvoiceApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Invoice> Invoices => Set<Invoice>();
        public DbSet<Payment> Payments => Set<Payment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(e =>
            {
                e.Property(u => u.Name).IsRequired().HasMaxLength(100);
                e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
                e.HasData(
                    new User { Id = 1, Name = "Finance Admin", Role = UserRole.Finance, Address = "Head office, Prishtina" },
                    new User { Id = 2, Name = "Postman One", Role = UserRole.Postman, Address = "Post depot, Prishtina" },
                    new User { Id = 3, Name = "Client One", Role = UserRole.Client, Address = "Rruga B, Prishtina" },
                    new User { Id = 4, Name = "Client Two", Role = UserRole.Client, Address = "Rruga Muharrem Fejza, Prishtina" });
            });

            modelBuilder.Entity<Invoice>(e =>
            {
                e.Property(i => i.InvoiceNumber).IsRequired().HasMaxLength(50);
                e.HasIndex(i => i.InvoiceNumber).IsUnique();
                e.Property(i => i.TotalAmount).HasPrecision(18, 2);
                e.Property(i => i.Status).HasConversion<string>().HasMaxLength(20);

                e.HasOne(i => i.Client).WithMany().HasForeignKey(i => i.ClientId).OnDelete(DeleteBehavior.Restrict);
                e.HasOne(i => i.Postman).WithMany().HasForeignKey(i => i.PostmanId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Payment>(e =>
            {
                e.Property(p => p.Amount).HasPrecision(18, 2);
                e.HasOne(p => p.Invoice).WithMany(i => i.Payments).HasForeignKey(p => p.InvoiceId);
            });
        }
    }
}
