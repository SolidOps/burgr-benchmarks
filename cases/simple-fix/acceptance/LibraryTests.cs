using System.Net;
using Benchmarks.Acceptance;

namespace Benchmarks.SimpleFix;

[TestClass]
public sealed class LibraryTests : AcceptanceTest
{
    private async Task<Guid> NewAuthorAsync() => Id(await CreateAsync("/authors", new { name = Unique("Author") }));

    private static object NewBook(Guid authorId, int status, int copies, string? isbn = null) => new
    {
        title = "A title",
        isbn = isbn ?? Unique("I", 20),
        copies,
        status,
        authorId,
    };

    [TestMethod]
    [DataRow(1, 1, true, DisplayName = "published, one copy")]
    [DataRow(1, 0, false, DisplayName = "published, no copy left")]
    [DataRow(0, 3, false, DisplayName = "draft")]
    [DataRow(2, 3, false, DisplayName = "out of print")]
    public async Task ABookIsAvailableWhenPublishedWithCopiesLeft(int status, int copies, bool expected)
    {
        var created = await CreateAsync("/books", NewBook(await NewAuthorAsync(), status, copies));

        Assert.AreEqual(expected, Bool(created, "isAvailable"), "returned by POST");
        Assert.AreEqual(expected, Bool(await GetObjectAsync($"/books/{Id(created)}"), "isAvailable"), "returned by GET");
    }

    [TestMethod]
    public async Task ABookIsNoLongerAvailableOnceItsLastCopyIsGone()
    {
        var authorId = await NewAuthorAsync();
        var isbn = Unique("I", 20);
        var id = Id(await CreateAsync("/books", NewBook(authorId, 1, 1, isbn)));

        await ExpectAsync(HttpStatusCode.OK, await PutAsync($"/books/{id}", NewBook(authorId, 1, 0, isbn)));

        Assert.IsFalse(Bool(await GetObjectAsync($"/books/{id}"), "isAvailable"));
        var books = Arr(await GetObjectAsync($"/authors/{authorId}"), "books");
        Assert.IsFalse(Bool(books.Single()!, "isAvailable"), "through the author");
    }

    [TestMethod]
    public async Task AuthorsAndBooksStillGoThroughTheirCrud()
    {
        var authorId = await NewAuthorAsync();
        var bookId = Id(await CreateAsync("/books", NewBook(authorId, 1, 2)));

        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/books", NewBook(authorId, 1, -1)), "negative copies");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/books", NewBook(Guid.NewGuid(), 1, 1)), "unknown author");
        await ExpectAsync(HttpStatusCode.NoContent, await DeleteAsync($"/books/{bookId}"));
        await ExpectAsync(HttpStatusCode.NoContent, await DeleteAsync($"/authors/{authorId}"));
        await ExpectAsync(HttpStatusCode.NotFound, await GetAsync($"/authors/{authorId}"));
    }
}
