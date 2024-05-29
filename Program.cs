using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Practice.WebApp.DataProvider;
using Practice.WebApp.Model;
using System.Runtime;

var builder = WebApplication.CreateBuilder(args);

string? appEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

builder.Configuration.AddJsonFile("appsettings.json", false, true);
builder.Configuration.AddJsonFile($"appsettings.{appEnvironment}.json", true, true);
builder.Configuration.AddEnvironmentVariables();

builder.Services.Configure<MyAppSetting>(builder.Configuration.GetSection("MySettings"));

builder.Services.AddDbContext<BundleDBContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddAntiforgery(o => o.HeaderName = "XSRF-TOKEN");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
	endpoints.MapRazorPages(); // Map Razor Pages
});

app.Run();
