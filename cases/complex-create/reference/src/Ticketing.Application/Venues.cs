namespace Acme.Ticketing.Application;

public partial class VenueService
{
    private partial Task<Error?> CheckNoShowAsync(Domain.Venue aggregate, CancellationToken cancellationToken) =>
        Task.FromResult(aggregate.Shows.Count > 0 ? Error.Conflict("The venue still has shows") : null);
}
