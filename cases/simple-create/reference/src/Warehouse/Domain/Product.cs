namespace Acme.Warehouse.Domain;

public partial class Product
{
    private partial bool CalculateNeedsReorder() => Status == ProductStatus.Active && Stock <= ReorderLevel;
}
