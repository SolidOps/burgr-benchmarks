using System.Net;
using System.Text.Json.Nodes;
using Benchmarks.Acceptance;

namespace Benchmarks.ComplexCreate;

public abstract class TicketingTest : AcceptanceTest
{
    protected static readonly string Admin = TokenFor("admin", "*");

    protected TicketingTest() => Token = Admin;

    protected static object NewVenue(int capacity = 100, string? name = null, string city = "Lyon") => new
    {
        name = name ?? Unique("Venue"),
        address = new { street = "1 Main Street", city, postalCode = "69001" },
        capacity,
    };

    protected static object NewShow(Guid venueId, int status = 1, DateTime? startsAt = null, string? title = null, params object[] tiers) => new
    {
        title = title ?? Unique("Show"),
        startsAt = startsAt ?? DateTime.UtcNow.AddDays(30),
        status,
        venueId,
        ticketTiers = tiers,
    };

    protected static object Tier(string name, decimal price, int quota, Guid? id = null, int? sold = null) =>
        sold is null ? new { id, name, price, quota } : new { id, name, price, quota, sold };

    protected async Task<Guid> NewVenueAsync(int capacity = 100) => Id(await CreateAsync("/venues", NewVenue(capacity)));

    protected async Task<JsonObject> NewShowAsync(int capacity, int status, params object[] tiers) =>
        await CreateAsync("/shows", NewShow(await NewVenueAsync(capacity), status, tiers: tiers));

    protected Task<HttpResponseMessage> BookAsync(Guid showId, string tier, int quantity, string email = "ada@example.com") =>
        PostAsync("/box-office/book", new { showId, tier, customerEmail = email, quantity });

    protected static JsonNode TierNamed(JsonNode show, string name) =>
        Arr(show, "ticketTiers").Single(t => Str(t!, "name") == name)!;
}

[TestClass]
public sealed class SecurityTests : TicketingTest
{
    [TestMethod]
    public async Task AnonymousCallersAreRejected()
    {
        Token = null;
        await ExpectAsync(HttpStatusCode.Unauthorized, await GetAsync("/venues"));
        await ExpectAsync(HttpStatusCode.Unauthorized, await GetAsync("/shows"));
        await ExpectAsync(HttpStatusCode.Unauthorized, await PostAsync("/venues", NewVenue()));
        await ExpectAsync(HttpStatusCode.Unauthorized, await BookAsync(Guid.NewGuid(), "Pit", 1));
    }

    [TestMethod]
    public async Task ATokenSignedWithAnotherKeyIsRejected()
    {
        Token = Admin[..^4] + "AAAA";
        await ExpectAsync(HttpStatusCode.Unauthorized, await GetAsync("/venues"));
    }

    [TestMethod]
    public async Task AnyAuthenticatedUserReadsVenuesAndShows()
    {
        var venueId = await NewVenueAsync();
        Token = TokenFor("reader");

        await ExpectAsync(HttpStatusCode.OK, await GetAsync("/venues"));
        await ExpectAsync(HttpStatusCode.OK, await GetAsync($"/venues/{venueId}"));
        await ExpectAsync(HttpStatusCode.OK, await GetAsync("/shows"));
    }

    [TestMethod]
    public async Task WritingVenuesRequiresManageVenues()
    {
        var venueId = await NewVenueAsync();
        Token = TokenFor("show manager", "ManageShows");

        await ExpectAsync(HttpStatusCode.Forbidden, await PostAsync("/venues", NewVenue()));
        await ExpectAsync(HttpStatusCode.Forbidden, await PutAsync($"/venues/{venueId}", NewVenue()));
        await ExpectAsync(HttpStatusCode.Forbidden, await DeleteAsync($"/venues/{venueId}"));

        Token = TokenFor("venue manager", "ManageVenues");
        await CreateAsync("/venues", NewVenue());
    }

    [TestMethod]
    public async Task WritingShowsRequiresManageShows()
    {
        var venueId = await NewVenueAsync();
        Token = TokenFor("venue manager", "ManageVenues");

        await ExpectAsync(HttpStatusCode.Forbidden, await PostAsync("/shows", NewShow(venueId)));

        Token = TokenFor("show manager", "ManageShows");
        await CreateAsync("/shows", NewShow(venueId));
    }

    [TestMethod]
    public async Task ReadingBookingsRequiresManageShows()
    {
        var show = await NewShowAsync(100, 1, Tier("Pit", 20m, 10));
        Token = TokenFor("customer");
        var booking = await ReadAsync(HttpStatusCode.OK, await BookAsync(Id(show), "Pit", 1));

        await ExpectAsync(HttpStatusCode.Forbidden, await GetAsync("/bookings"));
        await ExpectAsync(HttpStatusCode.Forbidden, await GetAsync($"/bookings/{Id(booking)}"));

        Token = TokenFor("show manager", "ManageShows");
        var read = await GetObjectAsync($"/bookings/{Id(booking)}");
        Assert.AreEqual(Id(show), Id(read, "showId"));
    }
}

[TestClass]
public sealed class VenueTests : TicketingTest
{
    [TestMethod]
    public async Task VenueGoesThroughTheWholeCrudWithItsAddress()
    {
        var created = await CreateAsync("/venues", NewVenue(capacity: 500));
        var id = Id(created);
        Assert.AreEqual(500, Int(created, "capacity"));
        Assert.AreEqual("Lyon", Str(Prop(created, "address")!, "city"));

        var renamed = Unique("Renamed");
        await ExpectAsync(HttpStatusCode.OK, await PutAsync($"/venues/{id}", NewVenue(capacity: 800, name: renamed, city: "Paris")));
        var read = await GetObjectAsync($"/venues/{id}");
        Assert.AreEqual(renamed, Str(read, "name"));
        Assert.AreEqual(800, Int(read, "capacity"));
        Assert.AreEqual("Paris", Str(Prop(read, "address")!, "city"));
        Assert.AreEqual("69001", Str(Prop(read, "address")!, "postalCode"));

        await ExpectAsync(HttpStatusCode.NoContent, await DeleteAsync($"/venues/{id}"));
        await ExpectAsync(HttpStatusCode.NotFound, await GetAsync($"/venues/{id}"));
    }

    [TestMethod]
    public async Task VenuesArePaged()
    {
        await NewVenueAsync();
        await NewVenueAsync();

        var page = await GetObjectAsync("/venues?page=1&pageSize=1&orderBy=Name");

        Assert.HasCount(1, Arr(page, "items"));
        Assert.AreEqual(1, Int(page, "pageNumber"));
        Assert.AreEqual(1, Int(page, "pageSize"));
        Assert.IsGreaterThanOrEqualTo(2, Int(page, "totalCount"));
    }

    [TestMethod]
    public async Task InvalidPagesAreRejected()
    {
        await ExpectAsync(HttpStatusCode.BadRequest, await GetAsync("/venues?pageSize=201"), "page size above 200");
        await ExpectAsync(HttpStatusCode.BadRequest, await GetAsync("/venues?page=0"), "page below 1");
        await ExpectAsync(HttpStatusCode.BadRequest, await GetAsync("/venues?orderBy=unknown"), "unknown order");
    }

    [TestMethod]
    public async Task ShowsAreOrderedOnRequest()
    {
        var venueId = await NewVenueAsync();
        var prefix = Unique("Order", 20);
        await CreateAsync("/shows", NewShow(venueId, title: prefix + "-a"));
        await CreateAsync("/shows", NewShow(venueId, title: prefix + "-b"));

        var page = await GetObjectAsync("/shows?pageSize=200&orderBy=Title&descending=true");

        var titles = Arr(page, "items").Select(s => Str(s!, "title")!).Where(t => t.StartsWith(prefix)).ToList();
        CollectionAssert.AreEqual(new[] { prefix + "-b", prefix + "-a" }, titles);
    }

    [TestMethod]
    public async Task InvalidVenuesAreRejected()
    {
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/venues", NewVenue(name: "")), "name missing");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/venues", NewVenue(name: new string('v', 101))), "name too long");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/venues", NewVenue(city: new string('c', 51))), "city too long");
    }

    [TestMethod]
    public async Task VenueNameIsUnique()
    {
        var name = Unique("Venue");
        await CreateAsync("/venues", NewVenue(name: name));

        await ExpectAsync(HttpStatusCode.Conflict, await PostAsync("/venues", NewVenue(name: name)));
    }

    [TestMethod]
    public async Task AVenueWithShowsCannotBeDeleted()
    {
        var venueId = await NewVenueAsync();
        await CreateAsync("/shows", NewShow(venueId));

        await ExpectAsync(HttpStatusCode.Conflict, await DeleteAsync($"/venues/{venueId}"));
    }

    [TestMethod]
    public async Task VenueIsReadWithItsShows()
    {
        var venueId = await NewVenueAsync();
        await CreateAsync("/shows", NewShow(venueId));
        await CreateAsync("/shows", NewShow(venueId));

        Assert.HasCount(2, Arr(await GetObjectAsync($"/venues/{venueId}"), "shows"));
    }
}

[TestClass]
public sealed class ShowTests : TicketingTest
{
    [TestMethod]
    public async Task ShowIsCreatedWithItsTiers()
    {
        var venueId = await NewVenueAsync();
        var created = await CreateAsync("/shows", NewShow(venueId, tiers: [Tier("Pit", 50m, 30), Tier("Balcony", 25.5m, 20)]));
        AssertEnum(created, "status", 1, "OnSale");

        var read = await GetObjectAsync($"/shows/{Id(created)}");

        Assert.AreEqual(venueId, Id(read, "venueId"));
        Assert.HasCount(2, Arr(read, "ticketTiers"));
        Assert.AreEqual(25.5m, Dec(TierNamed(read, "Balcony"), "price"));
        Assert.AreEqual(0, Int(TierNamed(read, "Pit"), "sold"));
        Assert.IsFalse(Bool(read, "isSoldOut"));
    }

    [TestMethod]
    public async Task UpdateSynchronizesTheTiers()
    {
        var show = await NewShowAsync(100, 1, Tier("Pit", 50m, 30), Tier("Balcony", 25m, 20));
        var id = Id(show);
        var pit = Id(TierNamed(show, "Pit"));

        await ExpectAsync(HttpStatusCode.OK, await PutAsync($"/shows/{id}", NewShow(Id(show, "venueId"), tiers: [Tier("Pit", 55m, 40, pit), Tier("Gallery", 10m, 10)])));

        var read = await GetObjectAsync($"/shows/{id}");
        CollectionAssert.AreEquivalent(new[] { "Pit", "Gallery" }, Arr(read, "ticketTiers").Select(t => Str(t!, "name")).ToArray());
        Assert.AreEqual(pit, Id(TierNamed(read, "Pit")), "a tier sent with its id is kept");
        Assert.AreEqual(40, Int(TierNamed(read, "Pit"), "quota"));
    }

    [TestMethod]
    public async Task TiersCannotOfferMoreThanTheVenueHolds()
    {
        var venueId = await NewVenueAsync(capacity: 100);
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/shows", NewShow(venueId, tiers: [Tier("Pit", 50m, 60), Tier("Balcony", 25m, 41)])), "on add");

        var show = await CreateAsync("/shows", NewShow(venueId, tiers: [Tier("Pit", 50m, 60), Tier("Balcony", 25m, 40)]));
        await ExpectAsync(HttpStatusCode.BadRequest, await PutAsync($"/shows/{Id(show)}", NewShow(venueId, tiers: [Tier("Pit", 50m, 61, Id(TierNamed(show, "Pit"))), Tier("Balcony", 25m, 40, Id(TierNamed(show, "Balcony")))])), "on update");
    }

    [TestMethod]
    public async Task InvalidShowsAreRejected()
    {
        var venueId = await NewVenueAsync();
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/shows", NewShow(Guid.NewGuid())), "unknown venue");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/shows", NewShow(venueId, title: "")), "title missing");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/shows", NewShow(venueId, status: 9)), "unknown status");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/shows", NewShow(venueId, tiers: [Tier(new string('t', 51), 1m, 1)])), "tier name too long");
    }

    [TestMethod]
    public async Task ShowWithoutBookingIsDeletedWithItsTiers()
    {
        var show = await NewShowAsync(100, 1, Tier("Pit", 50m, 30));

        await ExpectAsync(HttpStatusCode.NoContent, await DeleteAsync($"/shows/{Id(show)}"));
        await ExpectAsync(HttpStatusCode.NotFound, await GetAsync($"/shows/{Id(show)}"));
    }

    [TestMethod]
    public async Task UnknownShowIsNotFound()
    {
        await ExpectAsync(HttpStatusCode.NotFound, await GetAsync($"/shows/{Guid.NewGuid()}"));
        await ExpectAsync(HttpStatusCode.NotFound, await PutAsync($"/shows/{Guid.NewGuid()}", NewShow(await NewVenueAsync())));
        await ExpectAsync(HttpStatusCode.NotFound, await DeleteAsync($"/shows/{Guid.NewGuid()}"));
    }
}

[TestClass]
public sealed class BoxOfficeTests : TicketingTest
{
    [TestMethod]
    public async Task BookingTakesTicketsFromTheTier()
    {
        var show = await NewShowAsync(100, 1, Tier("Pit", 42.5m, 10), Tier("Balcony", 20m, 10));

        var booking = await ReadAsync(HttpStatusCode.OK, await BookAsync(Id(show), "Pit", 4, "grace@example.com"));

        Assert.AreEqual(Id(show), Id(booking, "showId"));
        Assert.AreEqual("Pit", Str(booking, "tier"));
        Assert.AreEqual("grace@example.com", Str(booking, "customerEmail"));
        Assert.AreEqual(4, Int(booking, "quantity"));
        Assert.AreEqual(170m, Dec(booking, "totalPrice"));
        var read = await GetObjectAsync($"/shows/{Id(show)}");
        Assert.AreEqual(4, Int(TierNamed(read, "Pit"), "sold"));
        Assert.AreEqual(0, Int(TierNamed(read, "Balcony"), "sold"));
        var bookings = Arr(await GetObjectAsync("/bookings?pageSize=200"), "items");
        Assert.IsTrue(bookings.Any(b => Id(b!) == Id(booking)), "the booking is listed");
    }

    [TestMethod]
    public async Task ShowIsSoldOutWhenEveryTierIs()
    {
        var show = await NewShowAsync(100, 1, Tier("Pit", 10m, 2), Tier("Balcony", 10m, 1));

        await ExpectAsync(HttpStatusCode.OK, await BookAsync(Id(show), "Pit", 2));
        Assert.IsFalse(Bool(await GetObjectAsync($"/shows/{Id(show)}"), "isSoldOut"), "a tier is left");
        await ExpectAsync(HttpStatusCode.OK, await BookAsync(Id(show), "Balcony", 1));

        Assert.IsTrue(Bool(await GetObjectAsync($"/shows/{Id(show)}"), "isSoldOut"));
        await ExpectAsync(HttpStatusCode.Conflict, await BookAsync(Id(show), "Pit", 1));
    }

    [TestMethod]
    public async Task ATierCannotBeOverbooked()
    {
        var show = await NewShowAsync(100, 1, Tier("Pit", 10m, 3));
        await ExpectAsync(HttpStatusCode.OK, await BookAsync(Id(show), "Pit", 2));

        await ExpectAsync(HttpStatusCode.Conflict, await BookAsync(Id(show), "Pit", 2));

        Assert.AreEqual(2, Int(TierNamed(await GetObjectAsync($"/shows/{Id(show)}"), "Pit"), "sold"));
    }

    [TestMethod]
    public async Task UpdatingAShowKeepsTheTicketsSold()
    {
        var show = await NewShowAsync(100, 1, Tier("Pit", 10m, 10));
        var pit = Id(TierNamed(show, "Pit"));
        await ExpectAsync(HttpStatusCode.OK, await BookAsync(Id(show), "Pit", 3));

        await ExpectAsync(HttpStatusCode.OK, await PutAsync($"/shows/{Id(show)}", NewShow(Id(show, "venueId"), tiers: [Tier("Pit", 12m, 10, pit, sold: 0)])));

        var tier = TierNamed(await GetObjectAsync($"/shows/{Id(show)}"), "Pit");
        Assert.AreEqual(3, Int(tier, "sold"), "sold is read-only");
        Assert.AreEqual(12m, Dec(tier, "price"));
    }

    [TestMethod]
    public async Task OnlyAShowOnSaleInTheFutureCanBeBooked()
    {
        var venueId = await NewVenueAsync();
        var draft = await CreateAsync("/shows", NewShow(venueId, status: 0, tiers: [Tier("Pit", 10m, 10)]));
        var cancelled = await CreateAsync("/shows", NewShow(venueId, status: 2, tiers: [Tier("Pit", 10m, 10)]));
        var past = await CreateAsync("/shows", NewShow(venueId, status: 1, startsAt: DateTime.UtcNow.AddDays(-1), tiers: [Tier("Pit", 10m, 10)]));

        await ExpectAsync(HttpStatusCode.Conflict, await BookAsync(Id(draft), "Pit", 1), "draft");
        await ExpectAsync(HttpStatusCode.Conflict, await BookAsync(Id(cancelled), "Pit", 1), "cancelled");
        await ExpectAsync(HttpStatusCode.Conflict, await BookAsync(Id(past), "Pit", 1), "past");
    }

    [TestMethod]
    public async Task InvalidBookingsAreRejected()
    {
        var show = await NewShowAsync(100, 1, Tier("Pit", 10m, 50));

        await ExpectAsync(HttpStatusCode.NotFound, await BookAsync(Guid.NewGuid(), "Pit", 1), "unknown show");
        await ExpectAsync(HttpStatusCode.BadRequest, await BookAsync(Id(show), "Stalls", 1), "unknown tier");
        await ExpectAsync(HttpStatusCode.BadRequest, await BookAsync(Id(show), "Pit", 0), "no ticket");
        await ExpectAsync(HttpStatusCode.BadRequest, await BookAsync(Id(show), "Pit", 11), "more than 10 tickets");
        await ExpectAsync(HttpStatusCode.BadRequest, await BookAsync(Id(show), "Pit", 1, "not-an-email"), "invalid email");
        Assert.AreEqual(0, Int(TierNamed(await GetObjectAsync($"/shows/{Id(show)}"), "Pit"), "sold"));
    }

    [TestMethod]
    public async Task AShowWithBookingsCannotBeDeleted()
    {
        var show = await NewShowAsync(100, 1, Tier("Pit", 10m, 10));
        await ExpectAsync(HttpStatusCode.OK, await BookAsync(Id(show), "Pit", 1));

        await ExpectAsync(HttpStatusCode.Conflict, await DeleteAsync($"/shows/{Id(show)}"));
    }
}
