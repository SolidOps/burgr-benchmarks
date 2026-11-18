#nullable enable
using System;
using System.Collections.Generic;
namespace Acme.Ticketing.Domain;
// Enum 
public enum ShowStatus
{
    Draft = 0,
OnSale = 1,
Cancelled = 2
}
// Object [AG]
public partial class Venue : AggregateRoot
{
}
public partial class Show : AggregateRoot
{
}
public partial class Booking : AggregateRoot
{
}
// Object [EN]
public partial class TicketTier : Entity
{
}
// Object [AG][EN]
public partial class Venue
{
    // Property [S][NO][NAR]
    public System.String Name { get; set; } = string.Empty;

    public System.Int32 Capacity { get; set; } = default(System.Int32);

    // Property [M][NO][NAR][VO]
    public Address Address { get; set; } = null!;

    // Property [M][LNA][AG][EN]
    public List<Show> Shows { get; set; } = [];

}
public partial class Show
{
    // Property [S][NO][NAR]
    public System.String Title { get; set; } = string.Empty;

    public System.DateTime StartsAt { get; set; } = default(System.DateTime);

    // Property [E][NO][NAR]
    public ShowStatus Status { get; set; }

    // Property [M][NO][NAR][AG][EN]
    public Guid VenueId { get; set; }
    public Venue? Venue { get; set; }

    // Property [M][LNA][AG][EN]
    public List<TicketTier> TicketTiers { get; set; } = [];

    // Property [S][CA][NAR]
    public System.Boolean IsSoldOut => CalculateIsSoldOut();
    private partial System.Boolean CalculateIsSoldOut();

}
public partial class TicketTier
{
    // Property [S][NO][NAR]
    public System.String Name { get; set; } = string.Empty;

    public System.Decimal Price { get; set; } = default(System.Decimal);

    public System.Int32 Quota { get; set; } = default(System.Int32);

    public System.Int32 Sold { get; set; } = default(System.Int32);

    // Property [M][NO][NAR][AG][EN]
    public Guid ShowId { get; set; }
    public Show? Show { get; set; }

}
public partial class Booking
{
    // Property [S][NO][NAR]
    public System.String Tier { get; set; } = string.Empty;

    public System.String CustomerEmail { get; set; } = string.Empty;

    public System.Int32 Quantity { get; set; } = default(System.Int32);

    public System.Decimal TotalPrice { get; set; } = default(System.Decimal);

    // Property [M][NO][NAR][AG][EN]
    public Guid ShowId { get; set; }
    public Show? Show { get; set; }

}
// Object [VO]
public sealed partial record Address
{
    // Property [S][NO][NAR]
    public System.String Street { get; init; } = string.Empty;

    public System.String City { get; init; } = string.Empty;

    public System.String PostalCode { get; init; } = string.Empty;

}