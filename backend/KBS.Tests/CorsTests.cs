using Microsoft.AspNetCore.Hosting;

namespace KBS.Tests;

public class CorsTests
{
    [Theory]
    [InlineData("http://localhost:5173", true)]
    [InlineData("http://localhost:5174", true)]
    [InlineData("http://untrusted.example", false)]
    public async Task Development_login_preflight_allows_only_configured_origins(string origin, bool allowed)
    {
        await using var factory = new TestApiFactory();
        await using var development = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = development.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        using var response = await client.SendAsync(request);

        var hasOrigin = response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins);
        Assert.Equal(allowed, hasOrigin);
        if (allowed) Assert.Equal(origin, Assert.Single(origins!));
    }
}
