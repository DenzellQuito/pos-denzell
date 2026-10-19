using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using POS_DENZELL.Data;
using POS_DENZELL.Models;
using POS_DENZELL.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login"; // A donde enviará a los intrusos
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // Dura un turno de trabajo completo
        options.AccessDeniedPath = "/Auth/AccesoDenegado";
    });

builder.Services.AddScoped<IVentaService, VentaService>();
builder.Services.AddScoped<ICajaService, CajaService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    await DbInicializador.EjecutarAsync(
        sp.GetRequiredService<ApplicationDbContext>(),
        sp.GetRequiredService<IPasswordHasher<Usuario>>(),
        app.Configuration, app.Environment);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}") // se cambia el home por el index recordar
    .WithStaticAssets();


app.Run();
