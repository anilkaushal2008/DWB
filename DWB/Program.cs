//using DWB.Data;
using DWB.Models;
using DWB.GroupModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;

var builder = WebApplication.CreateBuilder(args);
//for session
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache(); // Required for storing session in memory
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Session timeout
    options.Cookie.HttpOnly = true; // Secure
    options.Cookie.IsEssential = true; // GDPR
});

//connections
var connection = builder.Configuration.GetConnectionString("DWBDATA");
builder.Services.AddDbContext<DWBEntity>(options =>
    options.UseSqlServer(connection));
var connection2 = builder.Configuration.GetConnectionString("GROUPDATA");
builder.Services.AddDbContext<GroupEntity>(options =>
    options.UseSqlServer(connection2));

// Add services to the container.
builder.Services.AddControllersWithViews().AddRazorRuntimeCompilation();



//Enable Cookie-based Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Home/Index"; // Redirect if not logged in
        options.AccessDeniedPath = "/Home/AccessDenied"; // Redirect if access denied
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

var app = builder.Build();
app.UseDeveloperExceptionPage();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();
