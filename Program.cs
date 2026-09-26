using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlataformaIncidencias.Data;
using PlataformaIncidencias.Services;

var builder = WebApplication.CreateBuilder(args);


// 1. Configuración de Base de Datos SQLite (EF Core)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? "DataSource=app.db;Cache=Shared";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 2. Configuración de Identity con Roles
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// 3. MVC y Razor Pages (Identity)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 4. Configuración de Caché con Redis (StackExchange.Redis)
var redisConnectionString = builder.Configuration["Redis:ConnectionString"]
    ?? builder.Configuration["Redis__ConnectionString"]
    ?? Environment.GetEnvironmentVariable("Redis__ConnectionString");

if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
        options.InstanceName = "BiciShared:";
    });
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

builder.Services.AddScoped<IIncidenciaCacheService, IncidenciaCacheService>();

// 5. Servicio de búsqueda en Algolia
builder.Services.AddHttpClient<IAlgoliaSearchService, AlgoliaSearchService>();

// 6. Servicio de Publicación WebSocket con PieHost
builder.Services.AddHttpClient<IPieHostService, PieHostService>();

var app = builder.Build();

// 4. Inicialización y Seed de Base de Datos
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al inicializar la base de datos.");
    }
}

// 5. Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Operaciones}/{action=Incidencias}/{id?}");

app.MapRazorPages();

app.Run();
