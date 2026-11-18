using System.Net;
using System.Net.Http.Json;
using Acme.Warehouse.Contracts;
using Acme.Warehouse.Domain;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Acme.Warehouse.Tests;

[TestClass]
public sealed class WarehouseApiTests
{
    private string databasePath = null!;
    private WebApplicationFactory<Program> factory = null!;
    private HttpClient client = null!;

    [TestInitialize]
    public void Initialize()
    {
        databasePath = Path.Combine(Path.GetTempPath(), $"warehouse-{Guid.NewGuid():N}.db");
        factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:Warehouse", $"Data Source={databasePath};Pooling=False"));
        client = factory.CreateClient();
    }

    [TestCleanup]
    public void Cleanup()
    {
        client.Dispose();
        factory.Dispose();
        File.Delete(databasePath);
    }

    [TestMethod]
    public async Task SupplierIsReadWithItsProducts()
    {
        var supplier = await PostAsync<SupplierDto>("/suppliers", new SupplierInput { Name = "Acme Tools" });
        await PostAsync<ProductDto>("/products", NewProduct(supplier.Id, "SKU-1", stock: 2, reorderLevel: 5));
        await PostAsync<ProductDto>("/products", NewProduct(supplier.Id, "SKU-2", stock: 10, reorderLevel: 5));

        var read = await client.GetFromJsonAsync<SupplierDto>($"/suppliers/{supplier.Id}");

        Assert.HasCount(2, read!.Products);
    }

    [TestMethod]
    [DataRow(ProductStatus.Active, 5, 5, true)]
    [DataRow(ProductStatus.Active, 6, 5, false)]
    [DataRow(ProductStatus.Discontinued, 0, 5, false)]
    public async Task ProductNeedsReorderWhenActiveAndLow(ProductStatus status, int stock, int reorderLevel, bool expected)
    {
        var supplier = await PostAsync<SupplierDto>("/suppliers", new SupplierInput { Name = "Acme Tools" });

        var product = await PostAsync<ProductDto>("/products", NewProduct(supplier.Id, "SKU-1", stock, reorderLevel, status));

        Assert.AreEqual(expected, product.NeedsReorder);
    }

    [TestMethod]
    public async Task NegativeStockIsInvalid()
    {
        var supplier = await PostAsync<SupplierDto>("/suppliers", new SupplierInput { Name = "Acme Tools" });

        var response = await client.PostAsJsonAsync("/products", NewProduct(supplier.Id, "SKU-1", stock: -1, reorderLevel: 0));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task DuplicateSkuIsAConflict()
    {
        var supplier = await PostAsync<SupplierDto>("/suppliers", new SupplierInput { Name = "Acme Tools" });
        await PostAsync<ProductDto>("/products", NewProduct(supplier.Id, "SKU-1", 1, 0));

        var response = await client.PostAsJsonAsync("/products", NewProduct(supplier.Id, "SKU-1", 1, 0));

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    private static ProductInput NewProduct(Guid supplierId, string sku, int stock, int reorderLevel, ProductStatus status = ProductStatus.Active) => new()
    {
        Sku = sku,
        Name = "A product",
        Stock = stock,
        ReorderLevel = reorderLevel,
        Status = status,
        SupplierId = supplierId,
    };

    private async Task<T> PostAsync<T>(string url, object input)
    {
        var response = await client.PostAsJsonAsync(url, input);
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
