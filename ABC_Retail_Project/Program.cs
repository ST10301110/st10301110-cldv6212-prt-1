using ABC_Retail_Project.Models;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

try
{
    builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to load configuration: {ex.Message}");
    throw;
}

builder.Services.AddControllersWithViews();

try
{
    var connectionString = builder.Configuration.GetConnectionString("AzureStorage") ??
        throw new InvalidOperationException("AzureStorage connection string is missing in configuration");

    builder.Services.AddAzureClients(clientBuilder =>
    {
        clientBuilder.AddBlobServiceClient(connectionString);
        clientBuilder.AddTableServiceClient(connectionString);
        clientBuilder.AddQueueServiceClient(connectionString);
        clientBuilder.AddFileServiceClient(connectionString);
    });

    // Register services in the correct order
    builder.Services.AddScoped<CustomerService>();
    builder.Services.AddScoped<ProductService>();
    builder.Services.AddScoped<OrderService>();

    builder.Services.AddScoped<ContractService>(provider =>
        new ContractService(connectionString));
}
catch (Exception ex)
{
    Console.WriteLine($"Service configuration failed: {ex.Message}");
    throw;
}

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();