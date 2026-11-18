#nullable enable
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
namespace Acme.Ticketing.Infrastructure;
public static partial class TicketingInfrastructureRegistration
{
    /// <summary>The Ticketing DbContext on the database chosen by <paramref name="configureDatabase"/>, its unit of work and repositories.</summary>
    public static IServiceCollection AddTicketingInfrastructure(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDatabase)
    {
        services.AddDbContext<TicketingDbContext>(configureDatabase);
        services.AddScoped<Application.IUnitOfWork, UnitOfWork>();
        // Object [AG]
        services.AddScoped<Domain.IVenueRepository, VenueRepository>();

        services.AddScoped<Domain.IShowRepository, ShowRepository>();

        services.AddScoped<Domain.IBookingRepository, BookingRepository>();

        return services;
    }
}