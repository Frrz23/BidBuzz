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
using BidBuzz.Services;
using System.IO;
using System.Text;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Stripe;

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
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

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
builder.Services.AddHangfire(config => config.UseSqlServerStorage(builder.Configuration.GetConnectionString("dbcs")));
builder.Services.AddHangfireServer();
builder.Services.AddScoped<AuctionSchedulerService>();

builder.Services.AddSignalR();
builder.Services.AddScoped<IAuctionScheduleRepository, AuctionScheduleRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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
    var schedRepo = services.GetRequiredService<IAuctionScheduleRepository>();
    var auctionScheduler = services.GetRequiredService<AuctionSchedulerService>();

    await schedRepo.SeedInitialScheduleAsync();

    // load the “Current” schedule
    var sched = await schedRepo.GetScheduleAsync("Current");
    if (sched != null)
    {
        var nowUtc = DateTime.UtcNow;
        // compute this week’s end
        var endUtc = auctionScheduler
            .GetNextUtcForLocal(
               Enum.Parse<DayOfWeek>(sched.EndDay),
               sched.EndHour,
               nowUtc,
               forceNextWeek: false
            );

        if (nowUtc <= endUtc)
        {
            var monitor = JobStorage.Current.GetMonitoringApi();
            var scheduledJobs = monitor.ScheduledJobs(0, 100);

            bool alreadyScheduled = scheduledJobs.Any(j =>
                j.Value.Job.Type == typeof(AuctionSchedulerService) &&
                (j.Value.Job.Method.Name == nameof(AuctionSchedulerService.StartAuctionJob) ||
                 j.Value.Job.Method.Name == nameof(AuctionSchedulerService.EndAuctionJob)));

            if (!alreadyScheduled)
            {
                await auctionScheduler.ScheduleNextCycleAsync(forceNextWeek: false);
            }
        }

        // otherwise, let the EndAuctionJob path (below) rotate+schedule the next week
    }
}


app.UseAuthentication();
app.UseAuthorization();
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Use UTC for time-based operations (this isn't strictly necessary unless needed elsewhere)
app.Use(async (context, next) =>
{
    // Force UTC time on all request/response headers (optional, based on your needs)
    context.Response.Headers["Time-Zone"] = "UTC";
    await next();
});

app.MapRazorPages();
app.MapHub<BidHub>("/bidHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
