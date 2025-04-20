using DataAccess.Data;
using DataAccess.Repository;
using DataAccess.Repository.IRepository;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Utility;
using BidBuzz.Hubs;


var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders(); // Clear default providers
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information); // Or LogLevel.Debug for more details

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BuyerOnly", policy => policy.RequireRole("Buyer"));
    options.AddPolicy("SellerOnly", policy => policy.RequireRole("Seller"));
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("dbcs")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddRazorPages();
builder.Services.AddScoped<IAuctionRepository, AuctionRepository>();
builder.Services.AddScoped<IBidRepository, BidRepository>(); // Add this line
builder.Services.AddHangfire(config =>config.UseSqlServerStorage(builder.Configuration.GetConnectionString("dbcs")));
builder.Services.AddHangfireServer();


//builder.Services.Configure<AuctionScheduleConfig>(builder.Configuration.GetSection("AuctionSchedule"));

builder.Services.AddSignalR();

builder.Services.AddScoped<IAuctionScheduleRepository, AuctionScheduleRepository>();



var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var auctionScheduleRepo = services.GetRequiredService<IAuctionScheduleRepository>();
    await auctionScheduleRepo.SeedInitialScheduleAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();
app.UseHangfireDashboard();  // Enables the Hangfire dashboard

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var auctionRepo = services.GetRequiredService<IAuctionRepository>();
    var scheduleRepo = services.GetRequiredService<IAuctionScheduleRepository>();

    var currentSchedule = await scheduleRepo.GetCurrentScheduleAsync();
    var startDow = Enum.Parse<DayOfWeek>(currentSchedule.StartDay);
    var endDow = Enum.Parse<DayOfWeek>(currentSchedule.EndDay);
    if (currentSchedule != null)
    {
        // Start Auctions
        RecurringJob.AddOrUpdate(
    "start-auctions",
    () => auctionRepo.StartAuctionAsync(),
    Cron.Weekly(startDow, currentSchedule.StartHour)
);

        RecurringJob.AddOrUpdate(
            "end-auctions",
            () => auctionRepo.EndAuctionAsync(),
            Cron.Weekly(endDow, currentSchedule.EndHour)
        );

        RecurringJob.AddOrUpdate(
            "relist-unsold-items",
            () => auctionRepo.RelistUnsoldItemsAsync(),
            Cron.Weekly(endDow, currentSchedule.EndHour)
        );
    }
}






app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapHub<BidHub>("/bidHub");


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
