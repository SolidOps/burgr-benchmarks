using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

[assembly: DoNotParallelize]

namespace Benchmarks.Acceptance;

/// <summary>
/// Black-box checks of an API started by the judge at BENCH_BASE_URL. Property names are
/// matched ignoring case, an enum may be a number or its name: the contract is the behavior.
/// </summary>
public abstract class AcceptanceTest
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri(Environment.GetEnvironmentVariable("BENCH_BASE_URL") ?? "http://127.0.0.1:5080"),
        Timeout = TimeSpan.FromSeconds(30),
    };

    /// <summary>The bearer token sent with the requests; null sends none.</summary>
    protected string? Token { get; set; }

    protected static string Unique(string prefix, int maxLength = 50)
    {
        var value = $"{prefix}-{Guid.NewGuid():N}";
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    /// <summary>A JWT signed with BENCH_JWT_KEY (HS256), issuer and audience BENCH_JWT_ISSUER when set.</summary>
    protected static string TokenFor(string name, params string[] rights)
    {
        var secret = Environment.GetEnvironmentVariable("BENCH_JWT_KEY") ?? throw new InvalidOperationException("BENCH_JWT_KEY is not set");
        var issuer = Environment.GetEnvironmentVariable("BENCH_JWT_ISSUER");
        var claims = rights.Select(right => new Claim("right", right)).Prepend(new Claim(JwtRegisteredClaimNames.Name, name)).Prepend(new Claim(JwtRegisteredClaimNames.Sub, name));
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = string.IsNullOrEmpty(issuer) ? null : issuer,
            Audience = string.IsNullOrEmpty(issuer) ? null : issuer,
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret.PadRight(32))), SecurityAlgorithms.HmacSha256),
        });
    }

    protected Task<HttpResponseMessage> GetAsync(string url) => SendAsync(HttpMethod.Get, url, null);

    protected Task<HttpResponseMessage> PostAsync(string url, object body) => SendAsync(HttpMethod.Post, url, body);

    protected Task<HttpResponseMessage> PutAsync(string url, object body) => SendAsync(HttpMethod.Put, url, body);

    protected Task<HttpResponseMessage> DeleteAsync(string url) => SendAsync(HttpMethod.Delete, url, null);

    protected async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, object? body)
    {
        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        if (Token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }
        return await Http.SendAsync(request);
    }

    /// <summary>Asserts the status code, with the body in the message.</summary>
    protected static async Task ExpectAsync(HttpStatusCode expected, HttpResponseMessage response, string? because = null)
    {
        if (response.StatusCode != expected)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"{because ?? response.RequestMessage?.RequestUri?.ToString()}: expected {(int)expected}, got {(int)response.StatusCode} {Truncate(body)}");
        }
    }

    /// <summary>Asserts the status code and returns the JSON body.</summary>
    protected static async Task<JsonNode> ReadAsync(HttpStatusCode expected, HttpResponseMessage response)
    {
        await ExpectAsync(expected, response);
        var body = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(body) ?? throw new AssertFailedException($"empty body from {response.RequestMessage?.RequestUri}");
    }

    protected async Task<JsonObject> CreateAsync(string url, object body) => (await ReadAsync(HttpStatusCode.Created, await PostAsync(url, body))).AsObject();

    protected async Task<JsonObject> GetObjectAsync(string url) => (await ReadAsync(HttpStatusCode.OK, await GetAsync(url))).AsObject();

    protected static JsonNode? Prop(JsonNode node, string name)
    {
        foreach (var (key, value) in node.AsObject())
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }
        }
        Assert.Fail($"no property '{name}' in {Truncate(node.ToJsonString())}");
        return null;
    }

    protected static string? Str(JsonNode node, string name) => Prop(node, name)?.GetValue<string>();

    protected static Guid Id(JsonNode node, string name = "id") => Guid.Parse(Str(node, name)!);

    protected static int Int(JsonNode node, string name) => Prop(node, name)!.GetValue<int>();

    protected static decimal Dec(JsonNode node, string name) => Prop(node, name)!.GetValue<decimal>();

    protected static bool Bool(JsonNode node, string name) => Prop(node, name)!.GetValue<bool>();

    protected static JsonArray Arr(JsonNode node, string name) => Prop(node, name)?.AsArray() ?? [];

    /// <summary>An enum value, given as its number or its name.</summary>
    protected static void AssertEnum(JsonNode node, string name, int number, string label)
    {
        var value = Prop(node, name);
        var matches = value is JsonValue v && (v.TryGetValue<int>(out var n) ? n == number : string.Equals(v.ToString(), label, StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(matches, $"{name}: expected {number} or {label}, got {value?.ToJsonString()}");
    }

    private static string Truncate(string text) => text.Length <= 500 ? text : text[..500] + "…";
}
