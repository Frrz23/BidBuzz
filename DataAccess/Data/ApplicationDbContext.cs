using Microsoft.EntityFrameworkCore;
using Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DataAccess.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Bid> Bids { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Fluent API configurations (if needed)
            modelBuilder.Entity<Item>(entity =>
            {
                entity.Property(i => i.Price)
                    .HasPrecision(18, 2); // Precision: 18, Scale: 2

                entity.HasKey(i => i.ItemID);

                entity.HasOne(i => i.Category)
                    .WithMany(c => c.Items)
                    .HasForeignKey(i => i.CategoryID);
            });

            modelBuilder.Entity<Bid>(entity =>
            {
                entity.Property(b => b.BidAmount)
                    .HasPrecision(18, 2); // Precision: 18, Scale: 2

                entity.HasKey(b => b.BidID);

                entity.HasOne(b => b.Item)
                    .WithMany()
                    .HasForeignKey(b => b.ItemID);
            });

            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.CategoryID); // Primary key
                entity.Property(c => c.CategoryName)
                    .IsRequired() // Make the CategoryName mandatory
                    .HasMaxLength(100); // Optional: Limit name length to 100 characters

                entity.Property(c => c.Description)
                    .HasMaxLength(500); // Optional: Limit description length

                entity.HasMany(c => c.Items)
                    .WithOne(i => i.Category)
                    .HasForeignKey(i => i.CategoryID);
            });
        }
    }
}


