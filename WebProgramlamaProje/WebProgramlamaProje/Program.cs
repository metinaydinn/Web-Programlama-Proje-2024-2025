using WebProgramlamaProje.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
// DbContext'i kaydedin
builder.Services.AddDbContext<SalonDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
// Authentication ekleyin
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/User/Login"; // Login sayfasý
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // Oturum süresi
        options.SlidingExpiration = true; // Oturum süresi yenilenebilir
    });


builder.Services.AddSession(
               opt =>
               {
                   opt.IdleTimeout = TimeSpan.FromMinutes(10);
                   opt.Cookie.HttpOnly = true;
                   opt.Cookie.IsEssential = true;
               }
               );

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
app.UseAuthentication();
app.UseAuthorization();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=Login}/{id?}");

app.Run();
