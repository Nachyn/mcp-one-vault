using System.Net.Http.Json;
using System.Text.Json;
using AutoFixture;
using Microsoft.AspNetCore.Mvc.Testing;
using OneVault.Integration.Tests.Shared;

namespace OneVault.Integration.Tests;

public class ApiTests
{
    [Theory]
    [AutoData]
    public async Task CreatedSecretCanBeRetrieved(string text, IFixture fixture)
    {
        await using var factory = fixture.Create<WebApplicationFactory<Program>>();
        using var client = factory.CreateClient();

        using var created =
            await client.PostAsJsonAsync("/v1/secrets", new {text, ttlSeconds = 3600});
        created.EnsureSuccessStatusCode();
        var body = await created.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("token").GetString();

        var retrieved = await client.GetFromJsonAsync<JsonElement>($"/v1/secrets/{token}");

        retrieved.GetProperty("text").GetString().Should().Be(text);
    }
}