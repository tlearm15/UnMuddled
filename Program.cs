using System.Security.Cryptography.X509Certificates;
using GroupProject.Models;
using GroupProject.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var certsDir = Path.Combine(builder.Environment.ContentRootPath, "certs");

//Load the client certificate 
var clientCertPath = Path.Combine(certsDir, "dev.pfx");
var clientCert = new X509Certificate2(clientCertPath, "Unmudd13d");

var caCertPath = Path.Combine(certsDir, "ca.crt");
var caCert = new X509Certificate2(caCertPath);

var connString = builder.Configuration.GetConnectionString("Default");
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connString);

dataSourceBuilder.UseClientCertificate(clientCert);
dataSourceBuilder.UseRootCertificate(caCert);

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
// TQ- SendGrid email service - API key needs to go in user secrets (SendGrid:ApiKey)
builder.Services
    .Configure<SendGridSettings>( builder.Configuration.GetSection( "SendGrid" ) )
    .AddTransient<IEmailSender, EmailSender>();

var app = builder.Build();

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