#nullable enable
using Microsoft.Extensions.DependencyInjection;
namespace Acme.Ticketing.Application;
public static partial class TicketingApplicationRegistration
{
    /// <summary>The application services of the Ticketing module: one per aggregate root with an API, those of the model, the factories and the event handlers.</summary>
    public static IServiceCollection AddTicketingApplication(this IServiceCollection services)
    {
        // Object [AG][R]
        services.AddScoped<VenueService>();

        services.AddScoped<ShowService>();

        services.AddScoped<BookingService>();

        // Service 
        services.AddScoped<IBoxOfficeService, BoxOfficeService>();

        // Object [AG][EN]
        services.AddScoped<IVenueFactory, VenueFactory>();

        services.AddScoped<IShowFactory, ShowFactory>();

        services.AddScoped<ITicketTierFactory, TicketTierFactory>();

        services.AddScoped<IBookingFactory, BookingFactory>();

        services.AddEventHandlers(typeof(TicketingApplicationRegistration).Assembly);
        return services;
    }
}