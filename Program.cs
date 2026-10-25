using GroupProject.Models;
using GroupProject.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Security.Cryptography.X509Certificates;

//loads the env file in the project root - TL
// added saftey check to make sure site build does not fail if env is missing - TL
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    DotNetEnv.Env.Load(envPath);
}
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
//Adds oracle and storage service - TL
builder.Services.AddScoped<IStorageService, OracleStorageService>();

var certsDir = Path.Combine(builder.Environment.ContentRootPath, "certs");

var connString = builder.Configuration.GetConnectionString("Default");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);

//TL- Added saftey check for dev certificate
var clientCertPath = Path.Combine(certsDir, "dev.pfx");

if(File.Exists(clientCertPath))
{
    var clientCert = new X509Certificate2(clientCertPath, "Unmudd13d");
    dataSourceBuilder.UseClientCertificate(clientCert);

}

// Added saftey check for ca.crt - TL

var caCertPath = Path.Combine(certsDir, "ca.crt");

if (File.Exists(caCertPath))
{
    var caCert = new X509Certificate2(caCertPath);
    dataSourceBuilder.UseRootCertificate(caCert);

}

var dataSource = dataSourceBuilder.Build();

builder.Services.AddDbContext<UnMuddledDbContext>(options =>
    options.UseNpgsql(dataSource));

// Add Identity services with custom password and user options-RW
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
}).AddEntityFrameworkStores<UnMuddledDbContext>()
    .AddDefaultTokenProviders();

var app = builder.Build();

await SeedService.SeedDatabase(app.Services);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();