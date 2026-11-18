#nullable enable
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Acme.Ticketing.Application;
namespace Acme.Ticketing.Api;
public static partial class TicketingEndpoints
{
    public static IEndpointRouteBuilder MapTicketingEndpoints(this IEndpointRouteBuilder app)
    {
        // Object [AG][R]
        MapVenueEndpoints(app.MapGroup("/venues").WithTags("Venues"));

        MapShowEndpoints(app.MapGroup("/shows").WithTags("Shows"));

        MapBookingEndpoints(app.MapGroup("/bookings").WithTags("Bookings"));

        // Service 
        MapBoxOfficeEndpoints(app.MapGroup("/box-office").WithTags("BoxOffice"));

        return app;
    }
    // Object [AG][R]
    private static void MapVenueEndpoints(RouteGroupBuilder group)
    {

        group.MapGet("/", async (int? page, int? pageSize, string? orderBy, bool? descending, VenueService service, CancellationToken cancellationToken) =>
            (await service.QueryAsync(HttpResults.ToPageRequest(page, pageSize, orderBy, descending), cancellationToken)).ToHttpResult())

            .RequireRight("")

            ;

        group.MapGet("/{id}", async (Guid id, VenueService service, CancellationToken cancellationToken) =>
            (await service.GetAsync(id, cancellationToken)).ToHttpResult())

            .RequireRight("")

            ;

        group.MapPost("/", async (VenueInput input, VenueService service, CancellationToken cancellationToken) =>
            (await service.AddAsync(input, cancellationToken)).ToCreatedHttpResult(dto => $"/venues/{dto.Id}"))

            .RequireRight("ManageVenues")

            ;

        group.MapPut("/{id}", async (Guid id, VenueInput input, VenueService service, CancellationToken cancellationToken) =>
            (await service.UpdateAsync(id, input, cancellationToken)).ToHttpResult())

            .RequireRight("ManageVenues")

            ;

        group.MapDelete("/{id}", async (Guid id, VenueService service, CancellationToken cancellationToken) =>
            (await service.RemoveAsync(id, cancellationToken)).ToHttpResult())

            .RequireRight("ManageVenues")

            ;

    }

    private static void MapShowEndpoints(RouteGroupBuilder group)
    {

        group.MapGet("/", async (int? page, int? pageSize, string? orderBy, bool? descending, ShowService service, CancellationToken cancellationToken) =>
            (await service.QueryAsync(HttpResults.ToPageRequest(page, pageSize, orderBy, descending), cancellationToken)).ToHttpResult())

            .RequireRight("")

            ;

        group.MapGet("/{id}", async (Guid id, ShowService service, CancellationToken cancellationToken) =>
            (await service.GetAsync(id, cancellationToken)).ToHttpResult())

            .RequireRight("")

            ;

        group.MapPost("/", async (ShowInput input, ShowService service, CancellationToken cancellationToken) =>
            (await service.AddAsync(input, cancellationToken)).ToCreatedHttpResult(dto => $"/shows/{dto.Id}"))

            .RequireRight("ManageShows")

            ;

        group.MapPut("/{id}", async (Guid id, ShowInput input, ShowService service, CancellationToken cancellationToken) =>
            (await service.UpdateAsync(id, input, cancellationToken)).ToHttpResult())

            .RequireRight("ManageShows")

            ;

        group.MapDelete("/{id}", async (Guid id, ShowService service, CancellationToken cancellationToken) =>
            (await service.RemoveAsync(id, cancellationToken)).ToHttpResult())

            .RequireRight("ManageShows")

            ;

    }

    private static void MapBookingEndpoints(RouteGroupBuilder group)
    {

        group.MapGet("/", async (int? page, int? pageSize, string? orderBy, bool? descending, BookingService service, CancellationToken cancellationToken) =>
            (await service.QueryAsync(HttpResults.ToPageRequest(page, pageSize, orderBy, descending), cancellationToken)).ToHttpResult())

            .RequireRight("ManageShows")

            ;

        group.MapGet("/{id}", async (Guid id, BookingService service, CancellationToken cancellationToken) =>
            (await service.GetAsync(id, cancellationToken)).ToHttpResult())

            .RequireRight("ManageShows")

            ;

    }

    // Service 
    private static void MapBoxOfficeEndpoints(RouteGroupBuilder group)
    {

        group.MapPost("/book", async ([FromBody] Acme.Ticketing.Application.BookingRequestDto request, IBoxOfficeService service, CancellationToken cancellationToken) =>
            (await service.BookAsync(request, cancellationToken)).ToHttpResult())
            .ValidateBody()

            .RequireRight("")

            ;

    }

}