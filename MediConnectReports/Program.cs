var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddSession();

builder.Services.AddHttpClient("MediConnectAPI", client =>
{
    client.BaseAddress = new Uri("http://localhost:5050/");
});

builder.Services.AddScoped<MediConnectReports.Services.ApiService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Urls.Add("http://localhost:5255");
app.Run();

