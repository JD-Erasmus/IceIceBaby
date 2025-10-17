using IceIceBaby.Data;
using IceIceBaby.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IceIceBaby.Identity;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IRunService, RunService>();
builder.Services.AddScoped<IStorageService, FileSystemStorageService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

//// Seed roles/users + product catalog
//using (var scope = app.Services.CreateScope())
//{
//    var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
//        //// Run pending migrations
//    await context.Database.MigrateAsync();
//    await IdentitySeeder.SeedAsync(scope.ServiceProvider, cfg);

//    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
//    await DataSeeder.SeedAsync(db);
//}


// Seed roles/users + product catalog
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var cfg = services.GetRequiredService<IConfiguration>();
    var db = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Run pending migrations
        await db.Database.MigrateAsync();

        // Seed roles and users
        await IdentitySeeder.SeedAsync(services, cfg);

        // Seed product catalog or other data
        await DataSeeder.SeedAsync(db);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Identity UI (Razor Pages) endpoints
app.MapRazorPages();

app.Run();
