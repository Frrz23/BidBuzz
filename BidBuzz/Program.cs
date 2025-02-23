using DataAccess.Data;
using DataAccess.Repository;
using DataAccess.Repository.IRepository;
using Hangfire;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("dbcs")));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("dbcs")));
builder.Services.AddHangfireServer();



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
var auctionRepo = app.Services.CreateScope().ServiceProvider.GetRequiredService<IAuctionRepository>();

RecurringJob.AddOrUpdate("start-auctions",
    () => auctionRepo.StartAuctionAsync(), "0 12 * * 6");  // Every Saturday at 12 PM

RecurringJob.AddOrUpdate("end-auctions",
    () => auctionRepo.EndAuctionAsync(), "0 0 * * 7");  // Every Sunday at 12 AM

RecurringJob.AddOrUpdate("relist-unsold-items",
    () => auctionRepo.RelistUnsoldItemsAsync(), "0 1 * * 7");  // Sunday at 1 AM


app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
