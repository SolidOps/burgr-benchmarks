#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
namespace Acme.Ticketing.Application;
// Object [AG][EN]
public sealed partial class VenueDto
{
    public Guid Id { get; set; }
    // Property [S][NO][CA][NAR][PUO]
    public System.String Name { get; set; } = string.Empty;

    public System.Int32 Capacity { get; set; } = default(System.Int32);

    // Property [M][NO][NAR][VO][PUO]
    public Domain.Address Address { get; set; } = null!;

    // Property [M][LNA][AG][EN][PUO]
    public List<ShowDto> Shows { get; set; } = [];

    public static VenueDto From(Domain.Venue entity) => new()
    {
        Id = entity.Id,
        // Property [S][NO][CA][NAR][PUO]
        Name = entity.Name,

        Capacity = entity.Capacity,

        // Property [M][NO][NAR][VO][PUO]
        Address = entity.Address,

        // Property [M][LNA][AG][EN][PUO]
        Shows = entity.Shows.Select(ShowDto.From).ToList(),

    };
}
public sealed partial class ShowDto
{
    public Guid Id { get; set; }
    // Property [S][NO][CA][NAR][PUO]
    public System.String Title { get; set; } = string.Empty;

    public System.DateTime StartsAt { get; set; } = default(System.DateTime);

    public System.Boolean IsSoldOut { get; set; } = default(System.Boolean);

    // Property [E][NO][CA][NAR][PUO]
    public Domain.ShowStatus Status { get; set; }

    // Property [M][NO][NAR][AG][EN][PUO]
    public Guid VenueId { get; set; }

    // Property [M][LNA][AG][EN][PUO]
    public List<TicketTierDto> TicketTiers { get; set; } = [];

    public static ShowDto From(Domain.Show entity) => new()
    {
        Id = entity.Id,
        // Property [S][NO][CA][NAR][PUO]
        Title = entity.Title,

        StartsAt = entity.StartsAt,

        IsSoldOut = entity.IsSoldOut,

        // Property [E][NO][CA][NAR][PUO]
        Status = entity.Status,

        // Property [M][NO][NAR][AG][EN][PUO]
        VenueId = entity.VenueId,

        // Property [M][LNA][AG][EN][PUO]
        TicketTiers = entity.TicketTiers.Select(TicketTierDto.From).ToList(),

    };
}
public sealed partial class TicketTierDto
{
    public Guid Id { get; set; }
    // Property [S][NO][CA][NAR][PUO]
    public System.String Name { get; set; } = string.Empty;

    public System.Decimal Price { get; set; } = default(System.Decimal);

    public System.Int32 Quota { get; set; } = default(System.Int32);

    public System.Int32 Sold { get; set; } = default(System.Int32);

    // Property [M][NO][NAR][AG][EN][PUO]
    public Guid ShowId { get; set; }

    public static TicketTierDto From(Domain.TicketTier entity) => new()
    {
        Id = entity.Id,
        // Property [S][NO][CA][NAR][PUO]
        Name = entity.Name,

        Price = entity.Price,

        Quota = entity.Quota,

        Sold = entity.Sold,

        // Property [M][NO][NAR][AG][EN][PUO]
        ShowId = entity.ShowId,

    };
}
public sealed partial class BookingDto
{
    public Guid Id { get; set; }
    // Property [S][NO][CA][NAR][PUO]
    public System.String Tier { get; set; } = string.Empty;

    public System.String CustomerEmail { get; set; } = string.Empty;

    public System.Int32 Quantity { get; set; } = default(System.Int32);

    public System.Decimal TotalPrice { get; set; } = default(System.Decimal);

    // Property [M][NO][NAR][AG][EN][PUO]
    public Guid ShowId { get; set; }

    public static BookingDto From(Domain.Booking entity) => new()
    {
        Id = entity.Id,
        // Property [S][NO][CA][NAR][PUO]
        Tier = entity.Tier,

        CustomerEmail = entity.CustomerEmail,

        Quantity = entity.Quantity,

        TotalPrice = entity.TotalPrice,

        // Property [M][NO][NAR][AG][EN][PUO]
        ShowId = entity.ShowId,

    };
}
// Object [AG][R]
public sealed partial class VenueDto
{
    /// <summary>The values accepted by the orderBy parameter of the query.</summary>
    public static readonly IReadOnlyCollection<string> SortableProperties =
    [
        "Id",
        // Property [S][NO][NAR][PUO]
        "Name",

        "Capacity",

    ];
}
public sealed partial class VenueInput
{
    // Property [S][NO][NAR][PUO][NRO]
    public System.String Name { get; set; } = string.Empty;

    public System.Int32 Capacity { get; set; } = default(System.Int32);

    // Property [M][NO][NAR][VO][PUO][NRO]
    public Domain.Address? Address { get; set; }

    /// <summary>Required values, maximum lengths, enum values, value objects and synchronized children, then <see cref="OnValidating"/>.</summary>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        // Property [S][NO][NAR][PUO][NRO]
        // VALIDATION RULE - NULLABLE_PROPERTY
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors["Name"] = ["Name is required"];
        }

        // Property [S][NO][NAR][PUO][NRO]
        // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
        if (Name?.Length > 100)
        {
            errors["Name"] = ["Name is longer than 100 characters"];
        }

        // Property [M][NO][NAR][VO][PUO][NRO][NN]
        if (Address is null)
        {
            errors["Address"] = ["Address is required"];
        }
        else
        {
            AddressValidation.Validate(Address, "Address", errors);
        }

        OnValidating(errors);
        return errors;
    }
    partial void OnValidating(Dictionary<string, string[]> errors);
    public void ApplyTo(Domain.Venue entity)
    {
        // Property [S][NO][NAR][PUO][NRO]
        entity.Name = Name;

        entity.Capacity = Capacity;

        // Property [M][NO][NAR][VO][PUO][NRO]
        entity.Address = Address!;

    }
}
public sealed partial class ShowDto
{
    /// <summary>The values accepted by the orderBy parameter of the query.</summary>
    public static readonly IReadOnlyCollection<string> SortableProperties =
    [
        "Id",
        // Property [S][NO][NAR][PUO]
        "Title",

        "StartsAt",

        // Property [E][NO][NAR][PUO]
        "Status",

        // Property [M][NO][NAR][AG][EN][PUO]
        "VenueId",

    ];
}
public sealed partial class ShowInput
{
    // Property [S][NO][NAR][PUO][NRO]
    public System.String Title { get; set; } = string.Empty;

    public System.DateTime StartsAt { get; set; } = default(System.DateTime);

    // Property [E][NO][NAR][PUO][NRO]
    public Domain.ShowStatus Status { get; set; }

    // Property [M][NO][NAR][AG][EN][PUO][NRO]
    public Guid VenueId { get; set; }

    // Property [M][LNA][SY][EN][PUO][NRO]
    /// <summary>The whole list: a child without Id is added, a child missing from the list is removed.</summary>
    public List<TicketTierInput> TicketTiers { get; set; } = [];

    /// <summary>Required values, maximum lengths, enum values, value objects and synchronized children, then <see cref="OnValidating"/>.</summary>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        // Property [S][NO][NAR][PUO][NRO]
        // VALIDATION RULE - NULLABLE_PROPERTY
        if (string.IsNullOrWhiteSpace(Title))
        {
            errors["Title"] = ["Title is required"];
        }

        // Property [S][NO][NAR][PUO][NRO]
        // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
        if (Title?.Length > 200)
        {
            errors["Title"] = ["Title is longer than 200 characters"];
        }

        // Property [E][NO][NAR][PUO][NRO][NN]
        if (!Enum.IsDefined(Status))
        {
            errors["Status"] = ["Status is not a valid value"];
        }

        // Property [M][LNA][SY][EN][PUO][NRO]
        for (var i = 0; i < TicketTiers.Count; i++)
        {
            foreach (var (key, messages) in TicketTiers[i].Validate())
            {
                errors[$"TicketTiers[{i}].{key}"] = messages;
            }
        }

        OnValidating(errors);
        return errors;
    }
    partial void OnValidating(Dictionary<string, string[]> errors);
    public void ApplyTo(Domain.Show entity)
    {
        // Property [S][NO][NAR][PUO][NRO]
        entity.Title = Title;

        entity.StartsAt = StartsAt;

        // Property [E][NO][NAR][PUO][NRO]
        entity.Status = Status;

        // Property [M][NO][NAR][AG][EN][PUO][NRO]
        entity.VenueId = VenueId;

        // Property [M][LNA][SY][EN][PUO][NRO]
        entity.TicketTiers.RemoveAll(child => !TicketTiers.Exists(input => Equals(input.Id, child.Id)));
        foreach (var childInput in TicketTiers)
        {
            var child = childInput.Id is { } childId ? entity.TicketTiers.Find(c => c.Id.Equals(childId)) : null;
            if (child is null)
            {
                child = new Domain.TicketTier();
                entity.TicketTiers.Add(child);
            }
            childInput.ApplyTo(child);
        }

    }
}
public sealed partial class BookingDto
{
    /// <summary>The values accepted by the orderBy parameter of the query.</summary>
    public static readonly IReadOnlyCollection<string> SortableProperties =
    [
        "Id",
        // Property [S][NO][NAR][PUO]
        "Tier",

        "CustomerEmail",

        "Quantity",

        "TotalPrice",

        // Property [M][NO][NAR][AG][EN][PUO]
        "ShowId",

    ];
}
public sealed partial class BookingInput
{
    // Property [S][NO][NAR][PUO][NRO]
    public System.String Tier { get; set; } = string.Empty;

    public System.String CustomerEmail { get; set; } = string.Empty;

    public System.Int32 Quantity { get; set; } = default(System.Int32);

    public System.Decimal TotalPrice { get; set; } = default(System.Decimal);

    // Property [M][NO][NAR][AG][EN][PUO][NRO]
    public Guid ShowId { get; set; }

    /// <summary>Required values, maximum lengths, enum values, value objects and synchronized children, then <see cref="OnValidating"/>.</summary>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        // Property [S][NO][NAR][PUO][NRO]
        // VALIDATION RULE - NULLABLE_PROPERTY
        if (string.IsNullOrWhiteSpace(Tier))
        {
            errors["Tier"] = ["Tier is required"];
        }

        // VALIDATION RULE - NULLABLE_PROPERTY
        if (string.IsNullOrWhiteSpace(CustomerEmail))
        {
            errors["CustomerEmail"] = ["CustomerEmail is required"];
        }

        // Property [S][NO][NAR][PUO][NRO]
        // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
        if (Tier?.Length > 50)
        {
            errors["Tier"] = ["Tier is longer than 50 characters"];
        }

        // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
        if (CustomerEmail?.Length > 100)
        {
            errors["CustomerEmail"] = ["CustomerEmail is longer than 100 characters"];
        }

        OnValidating(errors);
        return errors;
    }
    partial void OnValidating(Dictionary<string, string[]> errors);
    public void ApplyTo(Domain.Booking entity)
    {
        // Property [S][NO][NAR][PUO][NRO]
        entity.Tier = Tier;

        entity.CustomerEmail = CustomerEmail;

        entity.Quantity = Quantity;

        entity.TotalPrice = TotalPrice;

        // Property [M][NO][NAR][AG][EN][PUO][NRO]
        entity.ShowId = ShowId;

    }
}
// Object [EN]
// a child of a synchronized navigation; its references to aggregate roots are set by the navigation
public sealed partial class TicketTierInput
{
    /// <summary>The child to update; null for a new child.</summary>
    public Guid? Id { get; set; }
    // Property [S][NO][NAR][PUO][NRO]
    public System.String Name { get; set; } = string.Empty;

    public System.Decimal Price { get; set; } = default(System.Decimal);

    public System.Int32 Quota { get; set; } = default(System.Int32);

    /// <summary>Required values, maximum lengths, enum values and value objects, then <see cref="OnValidating"/>.</summary>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        // Property [S][NO][NAR][PUO][NRO]
        // VALIDATION RULE - NULLABLE_PROPERTY
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors["Name"] = ["Name is required"];
        }

        // Property [S][NO][NAR][PUO][NRO]
        // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
        if (Name?.Length > 50)
        {
            errors["Name"] = ["Name is longer than 50 characters"];
        }

        OnValidating(errors);
        return errors;
    }
    partial void OnValidating(Dictionary<string, string[]> errors);
    public void ApplyTo(Domain.TicketTier entity)
    {
        // Property [S][NO][NAR][PUO][NRO]
        entity.Name = Name;

        entity.Price = Price;

        entity.Quota = Quota;

    }
}
// Object [TR]
public sealed partial class BookingRequestDto : IValidatable
{
    // Property [S][NP][NAR][PUO]
    public System.Guid ShowId { get; set; } = default(System.Guid);

    public System.String Tier { get; set; } = string.Empty;

    public System.String CustomerEmail { get; set; } = string.Empty;

    public System.Int32 Quantity { get; set; } = default(System.Int32);

    /// <summary>Required values, enum values and value objects, then <see cref="OnValidating"/>.</summary>
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        // Property [S][NP][NAR][PUO]
        // VALIDATION RULE - NULLABLE_PROPERTY
        if (string.IsNullOrWhiteSpace(Tier))
        {
            errors["Tier"] = ["Tier is required"];
        }

        // VALIDATION RULE - NULLABLE_PROPERTY
        if (string.IsNullOrWhiteSpace(CustomerEmail))
        {
            errors["CustomerEmail"] = ["CustomerEmail is required"];
        }

        OnValidating(errors);
        return errors;
    }
    partial void OnValidating(Dictionary<string, string[]> errors);
}
// Object [VO]
internal static partial class AddressValidation
{
    public static void Validate(Domain.Address value, string path, Dictionary<string, string[]> errors)
    {
        // Property [S][NO][NAR]
        // VALIDATION RULE - NULLABLE_PROPERTY
        if (string.IsNullOrWhiteSpace(value.Street))
        {
            errors[path + ".Street"] = ["Street is required"];
        }

        // VALIDATION RULE - NULLABLE_PROPERTY
        if (string.IsNullOrWhiteSpace(value.City))
        {
            errors[path + ".City"] = ["City is required"];
        }

        // VALIDATION RULE - NULLABLE_PROPERTY
        if (string.IsNullOrWhiteSpace(value.PostalCode))
        {
            errors[path + ".PostalCode"] = ["PostalCode is required"];
        }

        // Property [S][NO][NAR]
        // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
        if (value.Street?.Length > 100)
        {
            errors[path + ".Street"] = ["Street is longer than 100 characters"];
        }

        // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
        if (value.City?.Length > 50)
        {
            errors[path + ".City"] = ["City is longer than 50 characters"];
        }

        // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
        if (value.PostalCode?.Length > 10)
        {
            errors[path + ".PostalCode"] = ["PostalCode is longer than 10 characters"];
        }

    }
}