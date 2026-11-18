#nullable enable
using System.Net;
using System.Threading.Tasks;
namespace Acme.Ticketing.Tests;
// the endpoints of every aggregate root answer, and refuse an anonymous caller unless is_anonymous
[TestClass]
public partial class TicketingApiTests : ApiTestBase
{
    // Object [AG][R]

    [TestMethod]
    public async Task VenueQueryAnswers()
    {
        var response = await Client.GetAsync("/venues?orderBy=Id&descending=true");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task VenueQueryRefusesAnAnonymousCaller()
    {
        Unauthenticate();
        var response = await Client.GetAsync("/venues");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UnknownVenueIsNotFound()
    {
        var response = await Client.GetAsync($"/venues/{UnknownId}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task ShowQueryAnswers()
    {
        var response = await Client.GetAsync("/shows?orderBy=Id&descending=true");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task ShowQueryRefusesAnAnonymousCaller()
    {
        Unauthenticate();
        var response = await Client.GetAsync("/shows");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UnknownShowIsNotFound()
    {
        var response = await Client.GetAsync($"/shows/{UnknownId}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task BookingQueryAnswers()
    {
        var response = await Client.GetAsync("/bookings?orderBy=Id&descending=true");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    [TestMethod]
    public async Task BookingQueryRefusesAnAnonymousCaller()
    {
        Unauthenticate();
        var response = await Client.GetAsync("/bookings");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task UnknownBookingIsNotFound()
    {
        var response = await Client.GetAsync($"/bookings/{UnknownId}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode, await response.Content.ReadAsStringAsync());
    }

}