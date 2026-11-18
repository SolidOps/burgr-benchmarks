#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace Acme.Ticketing.Application;
// Object [AG][EN]
/// <summary>The factories of Venue declared by the model; a new object is not saved by its factory.</summary>
public partial interface IVenueFactory
{

}
// the methods of IVenueFactory are written by hand in another part of this class
public partial class VenueFactory(

    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider) : IVenueFactory
{

    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;
    /// <summary>The services this class needs besides the repositories of its model dependencies.</summary>
    protected IServiceProvider Services { get; } = serviceProvider;
}
/// <summary>The factories of Show declared by the model; a new object is not saved by its factory.</summary>
public partial interface IShowFactory
{

}
// the methods of IShowFactory are written by hand in another part of this class
public partial class ShowFactory(
    // Dependency [AG]
    Domain.IVenueRepository VenueRepository,

    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider) : IShowFactory
{
    // Dependency [AG]
    protected Domain.IVenueRepository VenueRepository { get; } = VenueRepository;

    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;
    /// <summary>The services this class needs besides the repositories of its model dependencies.</summary>
    protected IServiceProvider Services { get; } = serviceProvider;
}
/// <summary>The factories of TicketTier declared by the model; a new object is not saved by its factory.</summary>
public partial interface ITicketTierFactory
{

}
// the methods of ITicketTierFactory are written by hand in another part of this class
public partial class TicketTierFactory(

    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider) : ITicketTierFactory
{

    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;
    /// <summary>The services this class needs besides the repositories of its model dependencies.</summary>
    protected IServiceProvider Services { get; } = serviceProvider;
}
/// <summary>The factories of Booking declared by the model; a new object is not saved by its factory.</summary>
public partial interface IBookingFactory
{

}
// the methods of IBookingFactory are written by hand in another part of this class
public partial class BookingFactory(

    IUnitOfWork unitOfWork,
    IServiceProvider serviceProvider) : IBookingFactory
{

    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;
    /// <summary>The services this class needs besides the repositories of its model dependencies.</summary>
    protected IServiceProvider Services { get; } = serviceProvider;
}