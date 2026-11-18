using System.Net;
using Benchmarks.Acceptance;

namespace Benchmarks.SimpleCreate;

[TestClass]
public sealed class SupplierTests : AcceptanceTest
{
    [TestMethod]
    public async Task SupplierGoesThroughTheWholeCrud()
    {
        var name = Unique("Supplier");
        var created = await CreateAsync("/suppliers", new { name, email = "sales@example.com" });
        var id = Id(created);
        Assert.AreEqual(name, Str(created, "name"));

        var list = (await ReadAsync(HttpStatusCode.OK, await GetAsync("/suppliers"))).AsArray();
        Assert.IsTrue(list.Any(s => Id(s!) == id), "the list holds the new supplier");

        var renamed = Unique("Renamed");
        var updated = await ReadAsync(HttpStatusCode.OK, await PutAsync($"/suppliers/{id}", new { name = renamed, email = (string?)null }));
        Assert.AreEqual(renamed, Str(updated, "name"));
        Assert.AreEqual(renamed, Str(await GetObjectAsync($"/suppliers/{id}"), "name"));

        await ExpectAsync(HttpStatusCode.NoContent, await DeleteAsync($"/suppliers/{id}"));
        await ExpectAsync(HttpStatusCode.NotFound, await GetAsync($"/suppliers/{id}"));
    }

    [TestMethod]
    public async Task UnknownSupplierIsNotFound()
    {
        var id = Guid.NewGuid();
        await ExpectAsync(HttpStatusCode.NotFound, await GetAsync($"/suppliers/{id}"));
        await ExpectAsync(HttpStatusCode.NotFound, await PutAsync($"/suppliers/{id}", new { name = Unique("Supplier") }));
        await ExpectAsync(HttpStatusCode.NotFound, await DeleteAsync($"/suppliers/{id}"));
    }

    [TestMethod]
    public async Task SupplierNameIsRequiredAndLimited()
    {
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/suppliers", new { name = "" }), "name missing");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/suppliers", new { name = new string('a', 101) }), "name too long");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/suppliers", new { name = Unique("Supplier"), email = new string('a', 101) }), "email too long");
    }

    [TestMethod]
    public async Task SupplierNameIsUnique()
    {
        var name = Unique("Supplier");
        await CreateAsync("/suppliers", new { name });

        await ExpectAsync(HttpStatusCode.Conflict, await PostAsync("/suppliers", new { name }));
    }

    [TestMethod]
    public async Task SupplierIsReadWithItsProducts()
    {
        var supplierId = Id(await CreateAsync("/suppliers", new { name = Unique("Supplier") }));
        await CreateAsync("/products", ProductTests.NewProduct(supplierId, stock: 1, reorderLevel: 0));
        await CreateAsync("/products", ProductTests.NewProduct(supplierId, stock: 2, reorderLevel: 0));

        var products = Arr(await GetObjectAsync($"/suppliers/{supplierId}"), "products");

        Assert.HasCount(2, products);
    }
}

[TestClass]
public sealed class ProductTests : AcceptanceTest
{
    public static object NewProduct(Guid supplierId, int stock, int reorderLevel, int status = 0, string? sku = null) => new
    {
        sku = sku ?? Unique("S", 20),
        name = "Hammer",
        stock,
        reorderLevel,
        status,
        supplierId,
    };

    private async Task<Guid> NewSupplierAsync() => Id(await CreateAsync("/suppliers", new { name = Unique("Supplier") }));

    [TestMethod]
    public async Task ProductGoesThroughTheWholeCrud()
    {
        var supplierId = await NewSupplierAsync();
        var sku = Unique("S", 20);
        var created = await CreateAsync("/products", NewProduct(supplierId, stock: 10, reorderLevel: 3, sku: sku));
        var id = Id(created);
        Assert.AreEqual(sku, Str(created, "sku"));
        Assert.AreEqual(10, Int(created, "stock"));
        Assert.AreEqual(supplierId, Id(created, "supplierId"));
        AssertEnum(created, "status", 0, "Active");

        var list = (await ReadAsync(HttpStatusCode.OK, await GetAsync("/products"))).AsArray();
        Assert.IsTrue(list.Any(p => Id(p!) == id), "the list holds the new product");

        var updated = await ReadAsync(HttpStatusCode.OK, await PutAsync($"/products/{id}", NewProduct(supplierId, stock: 4, reorderLevel: 3, status: 1, sku: sku)));
        Assert.AreEqual(4, Int(updated, "stock"));
        var read = await GetObjectAsync($"/products/{id}");
        Assert.AreEqual(4, Int(read, "stock"));
        AssertEnum(read, "status", 1, "Discontinued");

        await ExpectAsync(HttpStatusCode.NoContent, await DeleteAsync($"/products/{id}"));
        await ExpectAsync(HttpStatusCode.NotFound, await GetAsync($"/products/{id}"));
    }

    [TestMethod]
    [DataRow(0, 5, 5, true, DisplayName = "active, stock at the reorder level")]
    [DataRow(0, 4, 5, true, DisplayName = "active, stock below the reorder level")]
    [DataRow(0, 6, 5, false, DisplayName = "active, stock above the reorder level")]
    [DataRow(1, 0, 5, false, DisplayName = "discontinued")]
    public async Task ProductNeedsReorderWhenActiveAndLow(int status, int stock, int reorderLevel, bool expected)
    {
        var created = await CreateAsync("/products", NewProduct(await NewSupplierAsync(), stock, reorderLevel, status));

        Assert.AreEqual(expected, Bool(created, "needsReorder"), "returned by POST");
        Assert.AreEqual(expected, Bool(await GetObjectAsync($"/products/{Id(created)}"), "needsReorder"), "returned by GET");
    }

    [TestMethod]
    public async Task NeedsReorderFollowsTheStock()
    {
        var supplierId = await NewSupplierAsync();
        var sku = Unique("S", 20);
        var id = Id(await CreateAsync("/products", NewProduct(supplierId, stock: 10, reorderLevel: 5, sku: sku)));

        await ExpectAsync(HttpStatusCode.OK, await PutAsync($"/products/{id}", NewProduct(supplierId, stock: 2, reorderLevel: 5, sku: sku)));

        Assert.IsTrue(Bool(await GetObjectAsync($"/products/{id}"), "needsReorder"));
    }

    [TestMethod]
    public async Task InvalidProductsAreRejected()
    {
        var supplierId = await NewSupplierAsync();
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/products", NewProduct(supplierId, stock: -1, reorderLevel: 0)), "negative stock");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/products", NewProduct(supplierId, stock: 0, reorderLevel: -1)), "negative reorder level");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/products", NewProduct(supplierId, stock: 0, reorderLevel: 0, sku: new string('s', 21))), "sku too long");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/products", NewProduct(supplierId, stock: 0, reorderLevel: 0, sku: "")), "sku missing");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/products", NewProduct(supplierId, stock: 0, reorderLevel: 0, status: 7)), "unknown status");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/products", NewProduct(Guid.NewGuid(), stock: 0, reorderLevel: 0)), "unknown supplier");
    }

    [TestMethod]
    public async Task SkuIsUnique()
    {
        var supplierId = await NewSupplierAsync();
        var sku = Unique("S", 20);
        await CreateAsync("/products", NewProduct(supplierId, stock: 1, reorderLevel: 0, sku: sku));

        await ExpectAsync(HttpStatusCode.Conflict, await PostAsync("/products", NewProduct(supplierId, stock: 1, reorderLevel: 0, sku: sku)));
    }

    [TestMethod]
    public async Task UnknownProductIsNotFound()
    {
        await ExpectAsync(HttpStatusCode.NotFound, await GetAsync($"/products/{Guid.NewGuid()}"));
        await ExpectAsync(HttpStatusCode.NotFound, await DeleteAsync($"/products/{Guid.NewGuid()}"));
    }
}
