using System.Security.Cryptography.X509Certificates;
using GroupProject.Models;
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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();