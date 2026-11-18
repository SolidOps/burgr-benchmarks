#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace Acme.Ticketing.Application;
// Service 
public partial interface IBoxOfficeService
{

    Task<Result<BookingDto>> BookAsync(Acme.Ticketing.Application.BookingRequestDto request, CancellationToken cancellationToken = default);

}
// the methods of IBoxOfficeService are written by hand in another part of this class
public partial class BoxOfficeService(
    // Dependency [AG]
    Domain.IShowRepository ShowRepository,

    Domain.IBookingRepository BookingRepository,

    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IServiceProvider serviceProvider) : IBoxOfficeService
{
    // Dependency [AG]
    protected Domain.IShowRepository ShowRepository { get; } = ShowRepository;

    protected Domain.IBookingRepository BookingRepository { get; } = BookingRepository;

    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;
    protected ICurrentUser CurrentUser { get; } = currentUser;
    /// <summary>The services this class needs besides the repositories of its model dependencies.</summary>
    protected IServiceProvider Services { get; } = serviceProvider;
}