using System.Net;
using System.Net.Http.Json;
using Acme.Ticketing.Application;

namespace Acme.Ticketing.Tests;

[TestClass]
public sealed class TicketingTests : ApiTestBase
{
    private async Task<VenueDto> NewVenueAsync(int capacity) =>
        await PostAsync<VenueDto>("/venues", new { name = $"Venue {Guid.NewGuid():N}", address = new { street = "1 Main St", city = "Lyon", postalCode = "69001" }, capacity });

    private static object NewShow(Guid venueId, int status, params (string Name, decimal Price, int Quota)[] tiers) => new
    {
        title = "Concert",
        startsAt = DateTime.UtcNow.AddDays(30),
        status,
        venueId,
        ticketTiers = tiers.Select(t => new { name = t.Name, price = t.Price, quota = t.Quota }).ToArray(),
    };

    [TestMethod]
    public async Task TiersCannotOfferMoreThanTheVenueHolds()
    {
        var venue = await NewVenueAsync(capacity: 100);

        var response = await Client.PostAsJsonAsync("/shows", NewShow(venue.Id, 1, ("Pit", 50m, 60), ("Balcony", 30m, 60)));

        await AssertInvalidAsync(response, "TicketTiers");
    }

    [TestMethod]
    public async Task BookingTakesTicketsUntilTheShowIsSoldOut()
    {
        var venue = await NewVenueAsync(capacity: 100);
        var show = await PostAsync<ShowDto>("/shows", NewShow(venue.Id, 1, ("Pit", 50m, 3)));

        var booking = await Client.PostAsJsonAsync("/box-office/book", new { showId = show.Id, tier = "Pit", customerEmail = "ada@example.com", quantity = 3 });
        Assert.AreEqual(HttpStatusCode.OK, booking.StatusCode, await booking.Content.ReadAsStringAsync());
        Assert.AreEqual(150m, (await booking.Content.ReadFromJsonAsync<BookingDto>())!.TotalPrice);

        var read = await Client.GetFromJsonAsync<ShowDto>($"/shows/{show.Id}");
        Assert.IsTrue(read!.IsSoldOut);
        Assert.AreEqual(3, read.TicketTiers[0].Sold);

        var refused = await Client.PostAsJsonAsync("/box-office/book", new { showId = show.Id, tier = "Pit", customerEmail = "bob@example.com", quantity = 1 });
        Assert.AreEqual(HttpStatusCode.Conflict, refused.StatusCode);
    }

    [TestMethod]
    public async Task ADraftShowIsNotOnSale()
    {
        var venue = await NewVenueAsync(capacity: 100);
        var show = await PostAsync<ShowDto>("/shows", NewShow(venue.Id, 0, ("Pit", 50m, 3)));

        var response = await Client.PostAsJsonAsync("/box-office/book", new { showId = show.Id, tier = "Pit", customerEmail = "ada@example.com", quantity = 1 });

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    public async Task QuantityIsBetweenOneAndTen()
    {
        var response = await Client.PostAsJsonAsync("/box-office/book", new { showId = Guid.NewGuid(), tier = "Pit", customerEmail = "ada@example.com", quantity = 11 });

        await AssertInvalidAsync(response, "Quantity");
    }

    [TestMethod]
    public async Task WritingAShowRequiresItsRight()
    {
        var venue = await NewVenueAsync(capacity: 100);
        AuthenticateAs("ManageVenues");

        var response = await Client.PostAsJsonAsync("/shows", NewShow(venue.Id, 1));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
