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

            // Configure Category
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(c => c.Id); 
                entity.Property(c => c.Name)
                      .IsRequired() 
                      .HasMaxLength(100);

                // Category -> Items (One-to-Many)
                entity.HasMany(c => c.Items)
                      .WithOne(i => i.Category)
                      .HasForeignKey(i => i.CategoryId)
                      .OnDelete(DeleteBehavior.Cascade); 
            });

            modelBuilder.Entity<Item>(entity =>
            {
                entity.HasKey(i => i.Id); 
                entity.Property(i => i.Name)
                      .IsRequired() 
                      .HasMaxLength(200); 

                entity.Property(i => i.Description)
                      .HasMaxLength(1000); 
                entity.Property(i => i.StartingPrice)
                      .HasPrecision(18, 2) 
                      .IsRequired(); 

                entity.Property(i => i.AuctionEndTime)
                      .IsRequired();

                // Item -> Category (Many-to-One)
                entity.HasOne(i => i.Category)
                      .WithMany(c => c.Items)
                      .HasForeignKey(i => i.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict); 
            });

      
            modelBuilder.Entity<Bid>(entity =>
            {
                entity.HasKey(b => b.Id); 
                entity.Property(b => b.BidAmount)
                      .HasPrecision(18, 2) 
                      .IsRequired(); 

                entity.Property(b => b.Timestamp)
                      .IsRequired();

                // Bid -> Item (Many-to-One)
                entity.HasOne(b => b.Item)
                      .WithMany(i => i.Bids)
                      .HasForeignKey(b => b.ItemId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}


