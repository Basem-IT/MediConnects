using MediConnectAPI.Data;
using MediConnectMVC.Hubs;
using MediConnectMVC.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();

builder.Services.AddSignalR();

builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddDbContext<MediConnectDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Login/session
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Static files
app.MapStaticAssets();

app.UseRouting();

// Login/session
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.MapHub<AppointmentHub>("/hubs/appointments");

app.Urls.Clear();
app.Urls.Add("http://localhost:5069");

app.Run();