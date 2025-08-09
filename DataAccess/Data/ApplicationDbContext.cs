using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
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
        public DbSet<AuctionSchedule>AuctionSchedules { get; set; }
        public DbSet<AutoBid> AutoBids { get; set; }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);             
            modelBuilder.Entity<AuctionSchedule>().HasData(
             new AuctionSchedule
             {
               Id = 1,
               Week = "Current",
               StartDay = "Saturday",
               StartHour = 12,
               EndDay = "Sunday",
               EndHour = 1
    },
             new AuctionSchedule
             {
                 Id = 2,
                 Week = "Next",
                 StartDay = "Saturday",
                 StartHour = 12,
                 EndDay = "Sunday",
                 EndHour = 1
             }
);

            modelBuilder.Entity<AutoBid>()
    .HasOne(a => a.Auction)
    .WithMany()
    .HasForeignKey(a => a.AuctionId)
    .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AutoBid>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Restrict);



            
            
           
            modelBuilder.Entity<Item>()
                .Property(i => i.StartingPrice)
                .HasColumnType("decimal(18,2)"); 
           





            
            modelBuilder.Entity<Bid>()
                .HasKey(b => b.Id); 
            modelBuilder.Entity<Bid>()
                .Property(b => b.Amount)
                .HasColumnType("decimal(18,2)"); 
            modelBuilder.Entity<Bid>()
                .HasOne(b => b.Auction)
                .WithMany(a => a.Bids)
                .HasForeignKey(b => b.AuctionId)
                .OnDelete(DeleteBehavior.Cascade);  

            
            modelBuilder.Entity<Auction>()
                .HasKey(a => a.Id); 
            modelBuilder.Entity<Auction>()
                .HasOne(a => a.Item)
                .WithMany(i => i.Auctions) 
                .HasForeignKey(a => a.ItemId)
                .OnDelete(DeleteBehavior.Cascade);  
            modelBuilder.Entity<Auction>()
                .Property(a => a.Status)
                .HasConversion<int>();  
        }
    }
}


