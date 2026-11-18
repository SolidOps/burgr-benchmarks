#nullable enable
using System;
using System.Collections.Generic;
namespace Acme.Warehouse.Domain;
// Enum 
public enum ProductStatus
{
    Active = 0,
Discontinued = 1
}
// Object [AG]
public partial class Supplier
{
    public Guid Id { get; set; }
    // Property [S][NO][NAR]
    public System.String Name { get; set; } = string.Empty;

    public String? Email { get; set; } = null;

    // Property [M][LNA][AG]
    public List<Product> Products { get; set; } = [];

}
public partial class Product
{
    public Guid Id { get; set; }
    // Property [S][NO][NAR]
    public System.String Sku { get; set; } = string.Empty;

    public System.String Name { get; set; } = string.Empty;

    public System.Int32 Stock { get; set; } = default(System.Int32);

    public System.Int32 ReorderLevel { get; set; } = default(System.Int32);

    // Property [E][NO][NAR]
    public ProductStatus Status { get; set; }

    // Property [M][NO][NAR][AG]
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    // Property [S][CA][NAR]
    public System.Boolean NeedsReorder => CalculateNeedsReorder();
    private partial System.Boolean CalculateNeedsReorder();

}