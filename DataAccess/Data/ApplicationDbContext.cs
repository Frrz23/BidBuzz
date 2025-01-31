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
        public DbSet<Auction> Auctions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Category Configuration
            modelBuilder.Entity<Category>().HasKey(c => c.Id); // Primary key
            modelBuilder.Entity<Category>()
                .Property(c => c.Name)
                .IsRequired() // Name must not be null
                .HasMaxLength(100);
            modelBuilder.Entity<Category>()
                .Property(c => c.Description)
                .HasMaxLength(500);

            // Item Configuration
            modelBuilder.Entity<Item>().HasKey(i => i.Id); // Primary key
            modelBuilder.Entity<Item>()
                .Property(i => i.Name)
                .IsRequired() // Name must not be null
                .HasMaxLength(200);
            modelBuilder.Entity<Item>()
                .Property(i => i.Description)
                .IsRequired()
                .HasMaxLength(1000);
            modelBuilder.Entity<Item>()
                .Property(i => i.StartingPrice)
                .HasColumnType("decimal(18,2)"); // Decimal configuration for pricing

            // Bid Configuration
            modelBuilder.Entity<Bid>()
                .HasKey(b => b.Id); // Primary key
            modelBuilder.Entity<Bid>()
                .Property(b => b.Amount)
                .HasColumnType("decimal(18,2)"); // Decimal configuration for bid amount
            modelBuilder.Entity<Bid>()
                .HasOne(b => b.Item)
                .WithMany(i => i.Bids)
                .HasForeignKey(b => b.ItemId) // Foreign key to Item
                .OnDelete(DeleteBehavior.Cascade); // When an Item is deleted, its related Bids are deleted too

            // Auction Configuration
            modelBuilder.Entity<Auction>()
                .HasKey(a => a.Id); // Primary key
            modelBuilder.Entity<Auction>()
                .HasOne(a => a.Item)
                .WithOne()
                .HasForeignKey<Auction>(a => a.ItemId) // One-to-one relationship with Item
                .OnDelete(DeleteBehavior.Cascade); // When an Auction is deleted, its related Item is deleted too
        }
    }
}


