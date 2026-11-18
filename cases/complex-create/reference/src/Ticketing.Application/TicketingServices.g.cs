#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Acme.Ticketing.Domain;
namespace Acme.Ticketing.Application;
// Object [AG][R]
public partial class VenueService(
    Domain.IVenueRepository repository,

    IUnitOfWork unitOfWork,
    IEventPublisher events,
    ICurrentUser currentUser,
    IServiceProvider serviceProvider)
{
    protected Domain.IVenueRepository Repository { get; } = repository;

    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;
    protected IEventPublisher Events { get; } = events;
    protected ICurrentUser CurrentUser { get; } = currentUser;
    /// <summary>The services the rules need besides the repositories of the model dependencies.</summary>
    protected IServiceProvider Services { get; } = serviceProvider;

    public async Task<Result<Page<VenueDto>>> QueryAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        var errors = Paging.Validate(request, VenueDto.SortableProperties);
        if (errors.Count > 0)
        {
            return Error.Invalid(errors);
        }
        var page = await Repository.QueryAsync(request, cancellationToken);
        return page.Map(VenueDto.From);
    }

    public async Task<Result<VenueDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var aggregate = await Repository.FindAsync(id, cancellationToken);
        return aggregate is null ? Error.NotFound() : VenueDto.From(aggregate);
    }

    public async Task<Result<VenueDto>> AddAsync(VenueInput input, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(input, cancellationToken);
        if (errors.Count > 0)
        {
            return Error.Invalid(errors);
        }
        var aggregate = new Domain.Venue();
        input.ApplyTo(aggregate);

        Repository.Add(aggregate);
        if (await UnitOfWork.SaveChangesAsync(cancellationToken) is { } error)
        {
            return error;
        }

        return VenueDto.From(aggregate);
    }

    public async Task<Result<VenueDto>> UpdateAsync(Guid id, VenueInput input, CancellationToken cancellationToken = default)
    {
        var aggregate = await Repository.FindAsync(id, cancellationToken);
        if (aggregate is null)
        {
            return Error.NotFound();
        }
        var errors = await ValidateAsync(input, cancellationToken);
        if (errors.Count > 0)
        {
            return Error.Invalid(errors);
        }
        input.ApplyTo(aggregate);

        if (await UnitOfWork.SaveChangesAsync(cancellationToken) is { } error)
        {
            return error;
        }

        return VenueDto.From(aggregate);
    }

    public async Task<Result> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var aggregate = await Repository.FindAsync(id, cancellationToken);
        if (aggregate is null)
        {
            return Error.NotFound();
        }
        // Rule [BR]
        if (await CheckNoShowAsync(aggregate, cancellationToken) is { } CheckNoShowError)
        {
            return CheckNoShowError;
        }

        Repository.Remove(aggregate);
        if (await UnitOfWork.SaveChangesAsync(cancellationToken) is { } error)
        {
            return error;
        }

        return Result.Success();
    }

    // Rule 
    /// <summary>The rule CheckNoShow of the model, written by hand: null to go on, or the error that stops the use case.</summary>
    private partial Task<Error?> CheckNoShowAsync(Domain.Venue aggregate, CancellationToken cancellationToken);

    // the input's own rules, then the existence of the aggregates and entities it references
    private async Task<Dictionary<string, string[]>> ValidateAsync(VenueInput input, CancellationToken cancellationToken)
    {
        var errors = input.Validate();

        return errors;
    }
}
public partial class ShowService(
    Domain.IShowRepository repository,
    // Dependency [AG]
    Domain.IVenueRepository VenueRepository,

    IUnitOfWork unitOfWork,
    IEventPublisher events,
    ICurrentUser currentUser,
    IServiceProvider serviceProvider)
{
    protected Domain.IShowRepository Repository { get; } = repository;
    // Dependency [AG]
    protected Domain.IVenueRepository VenueRepository { get; } = VenueRepository;

    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;
    protected IEventPublisher Events { get; } = events;
    protected ICurrentUser CurrentUser { get; } = currentUser;
    /// <summary>The services the rules need besides the repositories of the model dependencies.</summary>
    protected IServiceProvider Services { get; } = serviceProvider;

    public async Task<Result<Page<ShowDto>>> QueryAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        var errors = Paging.Validate(request, ShowDto.SortableProperties);
        if (errors.Count > 0)
        {
            return Error.Invalid(errors);
        }
        var page = await Repository.QueryAsync(request, cancellationToken);
        return page.Map(ShowDto.From);
    }

    public async Task<Result<ShowDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var aggregate = await Repository.FindAsync(id, cancellationToken);
        return aggregate is null ? Error.NotFound() : ShowDto.From(aggregate);
    }

    public async Task<Result<ShowDto>> AddAsync(ShowInput input, CancellationToken cancellationToken = default)
    {
        var errors = await ValidateAsync(input, cancellationToken);
        if (errors.Count > 0)
        {
            return Error.Invalid(errors);
        }
        var aggregate = new Domain.Show();
        input.ApplyTo(aggregate);
        // Rule [BA]
        if (await CheckCapacityAsync(aggregate, cancellationToken) is { } CheckCapacityError)
        {
            return CheckCapacityError;
        }

        Repository.Add(aggregate);
        if (await UnitOfWork.SaveChangesAsync(cancellationToken) is { } error)
        {
            return error;
        }

        return ShowDto.From(aggregate);
    }

    public async Task<Result<ShowDto>> UpdateAsync(Guid id, ShowInput input, CancellationToken cancellationToken = default)
    {
        var aggregate = await Repository.FindAsync(id, cancellationToken);
        if (aggregate is null)
        {
            return Error.NotFound();
        }
        var errors = await ValidateAsync(input, cancellationToken);
        if (errors.Count > 0)
        {
            return Error.Invalid(errors);
        }
        input.ApplyTo(aggregate);
        // Rule [BU]
        if (await CheckCapacityAsync(aggregate, cancellationToken) is { } CheckCapacityError)
        {
            return CheckCapacityError;
        }

        if (await UnitOfWork.SaveChangesAsync(cancellationToken) is { } error)
        {
            return error;
        }

        return ShowDto.From(aggregate);
    }

    public async Task<Result> RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var aggregate = await Repository.FindAsync(id, cancellationToken);
        if (aggregate is null)
        {
            return Error.NotFound();
        }

        Repository.Remove(aggregate);
        if (await UnitOfWork.SaveChangesAsync(cancellationToken) is { } error)
        {
            return error;
        }

        return Result.Success();
    }

    // Rule 
    /// <summary>The rule CheckCapacity of the model, written by hand: null to go on, or the error that stops the use case.</summary>
    private partial Task<Error?> CheckCapacityAsync(Domain.Show aggregate, CancellationToken cancellationToken);

    // the input's own rules, then the existence of the aggregates and entities it references
    private async Task<Dictionary<string, string[]>> ValidateAsync(ShowInput input, CancellationToken cancellationToken)
    {
        var errors = input.Validate();
        // Property [M][NO][NAR][AG][EN][PUO][NRO][NN]
        if (!await UnitOfWork.ExistsAsync<Domain.Venue>(input.VenueId, cancellationToken))
        {
            errors["VenueId"] = ["Venue not found"];
        }

        return errors;
    }
}
public partial class BookingService(
    Domain.IBookingRepository repository,

    IUnitOfWork unitOfWork,
    IEventPublisher events,
    ICurrentUser currentUser,
    IServiceProvider serviceProvider)
{
    protected Domain.IBookingRepository Repository { get; } = repository;

    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;
    protected IEventPublisher Events { get; } = events;
    protected ICurrentUser CurrentUser { get; } = currentUser;
    /// <summary>The services the rules need besides the repositories of the model dependencies.</summary>
    protected IServiceProvider Services { get; } = serviceProvider;

    public async Task<Result<Page<BookingDto>>> QueryAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        var errors = Paging.Validate(request, BookingDto.SortableProperties);
        if (errors.Count > 0)
        {
            return Error.Invalid(errors);
        }
        var page = await Repository.QueryAsync(request, cancellationToken);
        return page.Map(BookingDto.From);
    }

    public async Task<Result<BookingDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var aggregate = await Repository.FindAsync(id, cancellationToken);
        return aggregate is null ? Error.NotFound() : BookingDto.From(aggregate);
    }

    // the input's own rules, then the existence of the aggregates and entities it references
    private async Task<Dictionary<string, string[]>> ValidateAsync(BookingInput input, CancellationToken cancellationToken)
    {
        var errors = input.Validate();
        // Property [M][NO][NAR][AG][EN][PUO][NRO][NN]
        if (!await UnitOfWork.ExistsAsync<Domain.Show>(input.ShowId, cancellationToken))
        {
            errors["ShowId"] = ["Show not found"];
        }

        return errors;
    }
}