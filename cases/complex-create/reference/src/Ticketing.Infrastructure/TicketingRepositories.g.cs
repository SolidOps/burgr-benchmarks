#nullable enable
using System.Linq;
using Microsoft.EntityFrameworkCore;
namespace Acme.Ticketing.Infrastructure;
// Object [AG]
public partial class VenueRepository(TicketingDbContext db) : EfRepository<Domain.Venue>(db), Domain.IVenueRepository
{
    protected override IQueryable<Domain.Venue> WithDetails(IQueryable<Domain.Venue> query)
    {
        return query
            // Property [M][LNA][AG][EN]
            .Include(x => x.Shows)

            ;
    }
}
public partial class ShowRepository(TicketingDbContext db) : EfRepository<Domain.Show>(db), Domain.IShowRepository
{
    protected override IQueryable<Domain.Show> WithDetails(IQueryable<Domain.Show> query)
    {
        return query
            // Property [M][LNA][AG][EN]
            .Include(x => x.TicketTiers)

            ;
    }
}
public partial class BookingRepository(TicketingDbContext db) : EfRepository<Domain.Booking>(db), Domain.IBookingRepository
{
    protected override IQueryable<Domain.Booking> WithDetails(IQueryable<Domain.Booking> query)
    {
        return query

            ;
    }
}