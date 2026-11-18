using System.Text;
using Acme.Ticketing.Api;
using Acme.Ticketing.Application;
using Acme.Ticketing.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var secret = builder.Configuration["Jwt:Key"] ?? "development key of Ticketing, replaced in production";
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret.PadRight(32))),
    });

builder.Services.AddTicketingApi();
builder.Services.AddTicketingApplication();
builder.Services.AddTicketingInfrastructure(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Ticketing") ?? "Data Source=ticketing.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<TicketingDbContext>().Database.EnsureCreated();
}

app.MapTicketingEndpoints();

app.Run();
