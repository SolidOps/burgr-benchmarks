#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
namespace Acme.Warehouse.Contracts;
// Object [AG]
public sealed partial class SupplierDto
{
    public Guid Id { get; set; }
    // Property [S][NO][NAR][PUO]
    public System.String Name { get; set; } = string.Empty;

    public String? Email { get; set; } = null;

    // Property [M][LNA][AG][PUO]
    public List<ProductDto> Products { get; set; } = [];

    public static SupplierDto From(Domain.Supplier entity) => new()
    {
        Id = entity.Id,
        // Property [S][NO][CA][NAR][PUO]
        Name = entity.Name,

        Email = entity.Email,

        // Property [M][LNA][AG][PUO]
        Products = entity.Products.Select(ProductDto.From).ToList(),

    };
}
public sealed partial class SupplierInput
{
    // Property [S][NO][NAR][PUO][NRO]
    public System.String Name { get; set; } = string.Empty;

    public String? Email { get; set; } = null;

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

        // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
        if (Email?.Length > 100)
        {
            errors["Email"] = ["Email is longer than 100 characters"];
        }

        OnValidating(errors);
        return errors;
    }
    partial void OnValidating(Dictionary<string, string[]> errors);
    public void ApplyTo(Domain.Supplier entity)
    {
        // Property [S][NO][NAR][PUO][NRO]
        entity.Name = Name;

        entity.Email = Email;

    }
}
public sealed partial class ProductDto
{
    public Guid Id { get; set; }
    // Property [S][NO][NAR][PUO]
    public System.String Sku { get; set; } = string.Empty;

    public System.String Name { get; set; } = string.Empty;

    public System.Int32 Stock { get; set; } = default(System.Int32);

    public System.Int32 ReorderLevel { get; set; } = default(System.Int32);

    // Property [S][CA][NAR][PUO]
    public System.Boolean NeedsReorder { get; set; } = default(System.Boolean);

    // Property [E][NO][CA][NAR][PUO]
    public Domain.ProductStatus Status { get; set; }

    // Property [M][NO][NAR][AG][PUO]
    public Guid SupplierId { get; set; }

    public static ProductDto From(Domain.Product entity) => new()
    {
        Id = entity.Id,
        // Property [S][NO][CA][NAR][PUO]
        Sku = entity.Sku,

        Name = entity.Name,

        Stock = entity.Stock,

        ReorderLevel = entity.ReorderLevel,

        NeedsReorder = entity.NeedsReorder,

        // Property [E][NO][CA][NAR][PUO]
        Status = entity.Status,

        // Property [M][NO][NAR][AG][PUO]
        SupplierId = entity.SupplierId,

    };
}
public sealed partial class ProductInput
{
    // Property [S][NO][NAR][PUO][NRO]
    public System.String Sku { get; set; } = string.Empty;

    public System.String Name { get; set; } = string.Empty;

    public System.Int32 Stock { get; set; } = default(System.Int32);

    public System.Int32 ReorderLevel { get; set; } = default(System.Int32);

    // Property [E][NO][NAR][PUO][NRO]
    public Domain.ProductStatus Status { get; set; }

    // Property [M][NO][NAR][AG][PUO][NRO]
    public Guid SupplierId { get; set; }

    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();
        // Property [S][NO][NAR][PUO][NRO]
        // VALIDATION RULE - NULLABLE_PROPERTY
        if (string.IsNullOrWhiteSpace(Sku))
        {
            errors["Sku"] = ["Sku is required"];
        }

        // VALIDATION RULE - NULLABLE_PROPERTY
        if (string.IsNullOrWhiteSpace(Name))
        {
            errors["Name"] = ["Name is required"];
        }

        // Property [S][NO][NAR][PUO][NRO]
        // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
        if (Sku?.Length > 20)
        {
            errors["Sku"] = ["Sku is longer than 20 characters"];
        }

        // VALIDATION RULE - PROPERTY_HAS_MAX_SIZE
        if (Name?.Length > 100)
        {
            errors["Name"] = ["Name is longer than 100 characters"];
        }

        // Property [E][NO][NAR][PUO][NRO][NN]
        if (!Enum.IsDefined(Status))
        {
            errors["Status"] = ["Status is not a valid value"];
        }

        OnValidating(errors);
        return errors;
    }
    partial void OnValidating(Dictionary<string, string[]> errors);
    public void ApplyTo(Domain.Product entity)
    {
        // Property [S][NO][NAR][PUO][NRO]
        entity.Sku = Sku;

        entity.Name = Name;

        entity.Stock = Stock;

        entity.ReorderLevel = ReorderLevel;

        // Property [E][NO][NAR][PUO][NRO]
        entity.Status = Status;

        // Property [M][NO][NAR][AG][PUO][NRO]
        entity.SupplierId = SupplierId;

    }
}