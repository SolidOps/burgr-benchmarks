namespace Acme.Ticketing.Application;

public partial class BoxOfficeService
{
    public async Task<Result<BookingDto>> BookAsync(BookingRequestDto request, CancellationToken cancellationToken = default)
    {
        var show = await ShowRepository.FindAsync(request.ShowId, cancellationToken);
        if (show is null)
        {
            return Error.NotFound("Unknown show");
        }
        if (show.Status != Domain.ShowStatus.OnSale || show.StartsAt <= DateTime.UtcNow)
        {
            return Error.Conflict("The show is not on sale");
        }
        var tier = show.TicketTiers.FirstOrDefault(t => t.Name == request.Tier);
        if (tier is null)
        {
            return Error.Invalid(new Dictionary<string, string[]> { [nameof(request.Tier)] = ["Unknown tier for this show"] });
        }
        if (tier.Quota - tier.Sold < request.Quantity)
        {
            return Error.Conflict("Not enough tickets left in this tier");
        }

        tier.Sold += request.Quantity;
        var booking = new Domain.Booking
        {
            ShowId = show.Id,
            Tier = tier.Name,
            CustomerEmail = request.CustomerEmail,
            Quantity = request.Quantity,
            TotalPrice = tier.Price * request.Quantity,
        };
        BookingRepository.Add(booking);
        if (await UnitOfWork.SaveChangesAsync(cancellationToken) is { } error)
        {
            return error;
        }
        return BookingDto.From(booking);
    }
}

public sealed partial class BookingRequestDto
{
    partial void OnValidating(Dictionary<string, string[]> errors)
    {
        if (Quantity is < 1 or > 10)
        {
            errors[nameof(Quantity)] = ["Quantity is between 1 and 10"];
        }
        if (!CustomerEmail.Contains('@'))
        {
            errors[nameof(CustomerEmail)] = ["CustomerEmail is not an email address"];
        }
    }
}
