using Acme.Warehouse.Api;
using Acme.Warehouse.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WarehouseDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Warehouse") ?? "Data Source=warehouse.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<WarehouseDbContext>().Database.EnsureCreated();
}

app.MapWarehouseEndpoints();

app.Run();

public partial class Program;
