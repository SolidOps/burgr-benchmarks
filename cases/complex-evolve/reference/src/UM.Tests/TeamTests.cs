using System.Net;
using System.Net.Http.Json;
using SolidOps.UM.Application;

namespace SolidOps.UM.Tests;

[TestClass]
public sealed class TeamTests : ApiTestBase
{
    private async Task<Guid> NewUserAsync(string email) =>
        (await PostAsync<UserDto>("/users", new UserInput { Email = email, Provider = "local" })).Id;

    [TestMethod]
    public async Task TeamIsSavedWithItsMembers()
    {
        var ada = await NewUserAsync("ada@example.com");
        var bob = await NewUserAsync("bob@example.com");

        var team = await PostAsync<TeamDto>("/teams", new { name = "Core", members = new[] { new { userEmail = "ada@example.com", isOwner = true }, new { userEmail = "bob@example.com", isOwner = false } } });

        var read = await Client.GetFromJsonAsync<TeamDto>($"/teams/{team.Id}");
        Assert.HasCount(2, read!.Members);
    }

    [TestMethod]
    public async Task ATeamWithMembersHasExactlyOneOwner()
    {
        var ada = await NewUserAsync("ada@example.com");

        var response = await Client.PostAsJsonAsync("/teams", new { name = "Core", members = new[] { new { userEmail = "ada@example.com", isOwner = false } } });

        await AssertInvalidAsync(response, "Members");
    }

    [TestMethod]
    public async Task ATeamWithMembersCannotBeDeleted()
    {
        var ada = await NewUserAsync("ada@example.com");
        var team = await PostAsync<TeamDto>("/teams", new { name = "Core", members = new[] { new { userEmail = "ada@example.com", isOwner = true } } });

        Assert.AreEqual(HttpStatusCode.Conflict, (await Client.DeleteAsync($"/teams/{team.Id}")).StatusCode);
    }

    [TestMethod]
    public async Task WritingTeamsRequiresManageTeams()
    {
        AuthenticateAs("ManageUsers");

        var response = await Client.PostAsJsonAsync("/teams", new { name = "Core" });

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
