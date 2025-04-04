﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Utility;


namespace DataAccess.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Item> Items { get; set; }
        public DbSet<Bid> Bids { get; set; }
        public DbSet<Auction> Auctions { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);             // ensures Identity’s database schema (tables, relationships, indexes) is properly configured.
            modelBuilder.Entity<Auction>().HasData(
         new Auction
         {
             Id = 1,
             ItemId = 1,  // Make sure an item exists with this ID
             Status = AuctionStatus.Approved,
             StartTime = new DateTime(2025, 2, 24, 12, 0, 0),  // Static DateTime value for StartTime
             EndTime = new DateTime(2025, 2, 25, 12, 0, 0)    // Static DateTime value for EndTime
         }
     );



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
            modelBuilder.Entity<Item>().Property(i => i.CategoryId).IsRequired();
            modelBuilder.Entity<Item>()
                  .HasOne(i => i.Category)
                  .WithMany()
                  .HasForeignKey(i => i.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Item>()
                .Property(i => i.Quantity)
                .IsRequired(); // Quantity must not be null
            modelBuilder.Entity<Item>()
                .Property(i => i.Condition)
                .IsRequired(); // Condition must not be null





            // Bid Configuration
            modelBuilder.Entity<Bid>()
                .HasKey(b => b.Id); // Primary key
            modelBuilder.Entity<Bid>()
                .Property(b => b.Amount)
                .HasColumnType("decimal(18,2)"); // Decimal configuration for bid amount
            modelBuilder.Entity<Bid>()
                .HasOne(b => b.Auction)
                .WithMany(a => a.Bids)
                .HasForeignKey(b => b.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);  // If auction is deleted, its bids are also deleted.

            // Auction Configuration
            modelBuilder.Entity<Auction>()
                .HasKey(a => a.Id); // Primary key
            modelBuilder.Entity<Auction>()
                .HasOne(a => a.Item)
                .WithMany(i => i.Auctions) 
                .HasForeignKey(a => a.ItemId)
                .OnDelete(DeleteBehavior.Cascade);  // If item is deleted, its auctions are also deleted.
            modelBuilder.Entity<Auction>()
                .Property(a => a.Status)
                .HasConversion<int>();  
        }
    }
}


