#nullable enable
namespace Acme.Ticketing.Domain;
// Object [AG]
public partial interface IVenueRepository : IRepository<Venue>
{
}
public partial interface IShowRepository : IRepository<Show>
{
}
public partial interface IBookingRepository : IRepository<Booking>
{
}