namespace Acme.Ticketing.Application;

public partial class ShowService
{
    private async partial Task<Error?> CheckCapacityAsync(Domain.Show aggregate, CancellationToken cancellationToken)
    {
        var venue = await VenueRepository.FindAsync(aggregate.VenueId, cancellationToken);
        if (venue is null)
        {
            return null;
        }
        var quota = aggregate.TicketTiers.Sum(tier => tier.Quota);
        return quota > venue.Capacity
            ? Error.Invalid(new Dictionary<string, string[]> { ["TicketTiers"] = [$"The tiers offer {quota} tickets, the venue holds {venue.Capacity}"] })
            : null;
    }
}
