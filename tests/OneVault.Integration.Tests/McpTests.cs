using System.Net.Http.Json;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OneVault.Integration.Tests.Shared;

namespace OneVault.Integration.Tests;

public class McpTests(McpFixture fixture) : IClassFixture<McpFixture>
{
    [Fact]
    public async Task McpResponsesMustNotBeCached()
    {
        using var http = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/mcp");
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        request.Content = JsonContent.Create(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-11-25",
                capabilities = new { },
                clientInfo = new {name = "cache-test", version = "1.0"}
            }
        });

        using var response = await http.SendAsync(request);

        response.EnsureSuccessStatusCode();
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
    }

    [Theory]
    [InlineData("2025-11-25")]
    [InlineData("2026-07-28")]
    public async Task ToolsCanBeDiscoveredAndShareSecretsWithJsonApi(string protocolVersion)
    {
        using var http = fixture.CreateClient();
        await using var transport = CreateTransport(http);
        await using var client = await McpClient.CreateAsync(transport,
            new McpClientOptions {ProtocolVersion = protocolVersion});

        var tools = await client.ListToolsAsync();
        tools.Select(tool => tool.Name).Should().BeEquivalentTo("create_secret", "get_secret");
        tools.Single(tool => tool.Name == "get_secret").JsonSchema
            .GetProperty("properties").EnumerateObject().Select(property => property.Name)
            .Should().Equal("token");

        const string text = "Секрет 🔑";
        var created = await client.CallToolAsync("create_secret",
            new Dictionary<string, object?> {["text"] = text, ["ttlSeconds"] = 60});
        created.IsError.Should().NotBe(true);
        var body = Body(created);
        var token = body.GetProperty("token").GetString()!;

        var retrieved = await client.CallToolAsync("get_secret",
            new Dictionary<string, object?> {["token"] = token});
        retrieved.IsError.Should().NotBe(true);
        Body(retrieved).GetProperty("text").GetString().Should().Be(text);
        Body(retrieved).GetProperty("expiresAt").GetDateTimeOffset()
            .Should().Be(body.GetProperty("expiresAt").GetDateTimeOffset());

        using var response = await http.GetAsync($"/v1/secrets/{token}");
        response.EnsureSuccessStatusCode();
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("text").GetString().Should().Be(text);

        using var jsonCreated =
            await http.PostAsJsonAsync("/v1/secrets", new {text, ttlSeconds = 60});
        jsonCreated.EnsureSuccessStatusCode();
        var jsonBody = await jsonCreated.Content.ReadFromJsonAsync<JsonElement>();
        var fromJson = await client.CallToolAsync("get_secret",
            new Dictionary<string, object?>
                {["token"] = jsonBody.GetProperty("token").GetString()});
        fromJson.IsError.Should().NotBe(true);
        Body(fromJson).GetProperty("text").GetString().Should().Be(text);
    }

    [Theory]
    [InlineData("", 60)]
    [InlineData(" ", 60)]
    [InlineData("secret", 0)]
    [InlineData("secret", 86401)]
    public async Task InvalidCreationReturnsToolError(string text, int ttlSeconds)
    {
        using var http = fixture.CreateClient();
        await using var transport = CreateTransport(http);
        await using var client = await McpClient.CreateAsync(transport);

        var result = await client.CallToolAsync("create_secret",
            new Dictionary<string, object?> {["text"] = text, ["ttlSeconds"] = ttlSeconds});

        result.IsError.Should().BeTrue();
        ErrorText(result).Should().Contain("InvalidInput");
    }

    [Theory]
    [InlineData("invalid", "InvalidInput")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "NotFound")]
    public async Task UnavailableSecretReturnsToolError(string token, string error)
    {
        using var http = fixture.CreateClient();
        await using var transport = CreateTransport(http);
        await using var client = await McpClient.CreateAsync(transport);

        var result = await client.CallToolAsync("get_secret",
            new Dictionary<string, object?> {["token"] = token});

        result.IsError.Should().BeTrue();
        ErrorText(result).Should().Contain(error).And.NotContain(token);
    }

    [Fact]
    public async Task ExpiredSecretReturnsSameErrorAsMissingSecret()
    {
        var time = fixture.Time;
        using var http = fixture.CreateClient();
        await using var transport = CreateTransport(http);
        await using var client = await McpClient.CreateAsync(transport);
        var created = await client.CallToolAsync("create_secret",
            new Dictionary<string, object?> {["text"] = "temporary", ["ttlSeconds"] = 1});
        created.IsError.Should().NotBe(true);
        var token = Body(created).GetProperty("token").GetString();
        Body(created).GetProperty("expiresAt").GetDateTimeOffset()
            .Should().Be(time.GetUtcNow().AddSeconds(1));

        time.Advance(TimeSpan.FromSeconds(1));
        var expired = await client.CallToolAsync("get_secret",
            new Dictionary<string, object?> {["token"] = token});
        var missing = await client.CallToolAsync("get_secret",
            new Dictionary<string, object?> {["token"] = new string('A', 43)});

        expired.IsError.Should().BeTrue();
        missing.IsError.Should().BeTrue();
        ErrorText(expired).Should().Be(ErrorText(missing)).And.Contain("NotFound");
    }

    [Fact]
    public async Task FullStorageReturnsToolError()
    {
        using var http = fixture.CreateClient();
        await using var transport = CreateTransport(http);
        await using var client = await McpClient.CreateAsync(transport);

        var result = await client.CallToolAsync("create_secret",
            new Dictionary<string, object?>
            {
                ["text"] = new string('x', McpFixture.StorageCapacityBytes),
                ["ttlSeconds"] = 60
            });

        result.IsError.Should().BeTrue();
        ErrorText(result).Should().Contain("CapacityExceeded");
    }

    private static HttpClientTransport CreateTransport(HttpClient http)
    {
        return new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(http.BaseAddress!, "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp
        }, http, ownsHttpClient: false);
    }

    private static JsonElement Body(CallToolResult result)
    {
        result.StructuredContent.Should().NotBeNull();
        return JsonSerializer.SerializeToElement(result.StructuredContent);
    }

    private static string ErrorText(CallToolResult result)
    {
        return string.Join(" ",
            result.Content.OfType<TextContentBlock>().Select(content => content.Text));
    }
}
