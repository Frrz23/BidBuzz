using DataAccess.Data;
using DataAccess.Repository;
using DataAccess.Repository.IRepository;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("dbcs")));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddRazorPages();
builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();
builder.Services.AddScoped<IBidRepository, BidRepository>(); // Add this line
builder.Services.AddHangfire(config =>config.UseSqlServerStorage(builder.Configuration.GetConnectionString("dbcs")));
builder.Services.AddHangfireServer();

builder.Services.Configure<AuctionScheduleConfig>(builder.Configuration.GetSection("AuctionSchedule"));




var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseHangfireDashboard();  // Enables the Hangfire dashboard

var auctionScheduleConfig = app.Services.GetRequiredService<IOptions<AuctionScheduleConfig>>().Value;

var auctionRepo = app.Services.CreateScope().ServiceProvider.GetRequiredService<IAuctionRepository>();


RecurringJob.AddOrUpdate("start-auctions",
    () => auctionRepo.StartAuctionAsync(),
    $"0 {auctionScheduleConfig.StartHour} * * {auctionScheduleConfig.StartDay}");

RecurringJob.AddOrUpdate("end-auctions",
    () => auctionRepo.EndAuctionAsync(),
    $"0 {auctionScheduleConfig.EndHour} * * {auctionScheduleConfig.EndDay}");

RecurringJob.AddOrUpdate("relist-unsold-items",
    () => auctionRepo.RelistUnsoldItemsAsync(),
    $"0 {auctionScheduleConfig.RelistHour} * * {auctionScheduleConfig.EndDay}");





app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
