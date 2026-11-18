namespace Acme.Warehouse.Contracts;

public sealed partial class ProductInput
{
    partial void OnValidating(Dictionary<string, string[]> errors)
    {
        if (Stock < 0)
        {
            errors[nameof(Stock)] = ["Stock cannot be negative"];
        }
        if (ReorderLevel < 0)
        {
            errors[nameof(ReorderLevel)] = ["ReorderLevel cannot be negative"];
        }
    }
}
