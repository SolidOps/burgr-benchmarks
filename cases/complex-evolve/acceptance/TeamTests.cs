using System.Net;
using Benchmarks.Acceptance;

namespace Benchmarks.ComplexEvolve;

public abstract class UserManagementTest : AcceptanceTest
{
    protected static readonly string Admin = TokenFor("admin", "*");

    protected UserManagementTest() => Token = Admin;

    protected async Task<string> NewUserAsync()
    {
        var email = $"{Guid.NewGuid():N}"[..20] + "@example.com";
        await CreateAsync("/users", new { email, provider = "local", technicalUser = false });
        return email;
    }

    protected static object Member(string userEmail, bool isOwner = false, Guid? id = null) => new { id, userEmail, isOwner };

    protected static object NewTeam(string? name = null, string? description = null, params object[] members) => new
    {
        name = name ?? Unique("Team"),
        description,
        members,
    };
}

[TestClass]
public sealed class TeamTests : UserManagementTest
{
    [TestMethod]
    public async Task TeamGoesThroughTheWholeCrudWithItsMembers()
    {
        var ada = await NewUserAsync();
        var bob = await NewUserAsync();
        var created = await CreateAsync("/teams", NewTeam(description: "The core team", members: [Member(ada, isOwner: true), Member(bob)]));
        var id = Id(created);

        var read = await GetObjectAsync($"/teams/{id}");
        Assert.AreEqual("The core team", Str(read, "description"));
        var members = Arr(read, "members");
        Assert.HasCount(2, members);
        Assert.IsTrue(Bool(members.Single(m => Str(m!, "userEmail") == ada)!, "isOwner"));

        var page = await GetObjectAsync("/teams?pageSize=200");
        Assert.IsTrue(Arr(page, "items").Any(t => Id(t!) == id), "the team is listed");

        var renamed = Unique("Renamed");
        await ExpectAsync(HttpStatusCode.OK, await PutAsync($"/teams/{id}", NewTeam(name: renamed)));
        var emptied = await GetObjectAsync($"/teams/{id}");
        Assert.AreEqual(renamed, Str(emptied, "name"));
        Assert.HasCount(0, Arr(emptied, "members"));

        await ExpectAsync(HttpStatusCode.NoContent, await DeleteAsync($"/teams/{id}"));
        await ExpectAsync(HttpStatusCode.NotFound, await GetAsync($"/teams/{id}"));
    }

    [TestMethod]
    public async Task UpdateSynchronizesTheMembers()
    {
        var ada = await NewUserAsync();
        var bob = await NewUserAsync();
        var eve = await NewUserAsync();
        var team = await CreateAsync("/teams", NewTeam(members: [Member(ada, isOwner: true), Member(bob)]));
        var adaMember = Id(Arr(team, "members").Single(m => Str(m!, "userEmail") == ada)!);

        await ExpectAsync(HttpStatusCode.OK, await PutAsync($"/teams/{Id(team)}", NewTeam(name: Str(team, "name"), members: [Member(ada, isOwner: false, id: adaMember), Member(eve, isOwner: true)])));

        var members = Arr(await GetObjectAsync($"/teams/{Id(team)}"), "members");
        CollectionAssert.AreEquivalent(new[] { ada, eve }, members.Select(m => Str(m!, "userEmail")).ToArray());
        Assert.IsTrue(Bool(members.Single(m => Str(m!, "userEmail") == eve)!, "isOwner"));
        Assert.AreEqual(adaMember, Id(members.Single(m => Str(m!, "userEmail") == ada)!), "a member sent with its id is kept");
    }

    [TestMethod]
    public async Task ATeamWithMembersHasExactlyOneOwner()
    {
        var ada = await NewUserAsync();
        var bob = await NewUserAsync();

        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/teams", NewTeam(members: [Member(ada), Member(bob)])), "no owner");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/teams", NewTeam(members: [Member(ada, isOwner: true), Member(bob, isOwner: true)])), "two owners");

        var team = await CreateAsync("/teams", NewTeam(members: [Member(ada, isOwner: true)]));
        await ExpectAsync(HttpStatusCode.BadRequest, await PutAsync($"/teams/{Id(team)}", NewTeam(name: Str(team, "name"), members: [Member(bob)])), "no owner on update");
    }

    [TestMethod]
    public async Task MembersAreExistingUsers()
    {
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/teams", NewTeam(members: [Member("nobody@example.com", isOwner: true)])));
    }

    [TestMethod]
    public async Task InvalidTeamsAreRejected()
    {
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/teams", NewTeam(name: "")), "name missing");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/teams", NewTeam(name: new string('t', 51))), "name too long");
        await ExpectAsync(HttpStatusCode.BadRequest, await PostAsync("/teams", NewTeam(description: new string('d', 201))), "description too long");
    }

    [TestMethod]
    public async Task TeamNameIsUnique()
    {
        var name = Unique("Team");
        await CreateAsync("/teams", NewTeam(name: name));

        await ExpectAsync(HttpStatusCode.Conflict, await PostAsync("/teams", NewTeam(name: name)));
    }

    [TestMethod]
    public async Task ATeamWithMembersCannotBeDeleted()
    {
        var team = await CreateAsync("/teams", NewTeam(members: [Member(await NewUserAsync(), isOwner: true)]));

        await ExpectAsync(HttpStatusCode.Conflict, await DeleteAsync($"/teams/{Id(team)}"));
        await ExpectAsync(HttpStatusCode.OK, await GetAsync($"/teams/{Id(team)}"));
    }

    [TestMethod]
    public async Task UnknownTeamIsNotFound()
    {
        await ExpectAsync(HttpStatusCode.NotFound, await GetAsync($"/teams/{Guid.NewGuid()}"));
        await ExpectAsync(HttpStatusCode.NotFound, await PutAsync($"/teams/{Guid.NewGuid()}", NewTeam()));
        await ExpectAsync(HttpStatusCode.NotFound, await DeleteAsync($"/teams/{Guid.NewGuid()}"));
    }
}

[TestClass]
public sealed class TeamSecurityTests : UserManagementTest
{
    [TestMethod]
    public async Task AnonymousCallersAreRejected()
    {
        Token = null;
        await ExpectAsync(HttpStatusCode.Unauthorized, await GetAsync("/teams"));
        await ExpectAsync(HttpStatusCode.Unauthorized, await PostAsync("/teams", NewTeam()));
    }

    [TestMethod]
    public async Task AnyAuthenticatedUserReadsTeams()
    {
        var team = await CreateAsync("/teams", NewTeam());
        Token = TokenFor("reader");

        await ExpectAsync(HttpStatusCode.OK, await GetAsync("/teams"));
        await ExpectAsync(HttpStatusCode.OK, await GetAsync($"/teams/{Id(team)}"));
    }

    [TestMethod]
    public async Task WritingTeamsRequiresManageTeams()
    {
        var team = await CreateAsync("/teams", NewTeam());
        Token = TokenFor("user manager", "ManageUsers");

        await ExpectAsync(HttpStatusCode.Forbidden, await PostAsync("/teams", NewTeam()));
        await ExpectAsync(HttpStatusCode.Forbidden, await PutAsync($"/teams/{Id(team)}", NewTeam()));
        await ExpectAsync(HttpStatusCode.Forbidden, await DeleteAsync($"/teams/{Id(team)}"));

        Token = TokenFor("team manager", "ManageTeams");
        await CreateAsync("/teams", NewTeam());
    }
}

[TestClass]
public sealed class RegressionTests : UserManagementTest
{
    [TestMethod]
    public async Task UsersStillGoThroughTheWholeCrud()
    {
        var email = await NewUserAsync();
        var page = await GetObjectAsync("/users?pageSize=200");
        var user = Arr(page, "items").Single(u => Str(u!, "email") == email)!;

        await ExpectAsync(HttpStatusCode.OK, await PutAsync($"/users/{Id(user)}", new { email, provider = "google", technicalUser = false }));
        Assert.AreEqual("google", Str(await GetObjectAsync($"/users/{Id(user)}"), "provider"));

        await ExpectAsync(HttpStatusCode.NoContent, await DeleteAsync($"/users/{Id(user)}"));
    }

    [TestMethod]
    public async Task WritingUsersStillRequiresManageUsers()
    {
        Token = TokenFor("team manager", "ManageTeams");

        await ExpectAsync(HttpStatusCode.Forbidden, await PostAsync("/users", new { email = "x@example.com", provider = "local" }));
    }

    [TestMethod]
    public async Task InvitesStillAnswer()
    {
        await ExpectAsync(HttpStatusCode.OK, await GetAsync("/invites"));
    }
}
