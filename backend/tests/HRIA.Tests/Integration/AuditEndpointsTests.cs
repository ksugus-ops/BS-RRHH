using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace HRIA.Tests.Integration;

public class AuditEndpointsTests : IClassFixture<HriaWebAppFactory>
{
    private readonly HriaWebAppFactory _factory;

    public AuditEndpointsTests(HriaWebAppFactory factory) => _factory = factory;

    private async Task<string> LoginAsync(HttpClient client, string email)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "Demo1234!" });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task Audit_AsEmployee_Returns403()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "empleado@hria.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/audit");
        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Audit_AsAdmin_ReturnsLog()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "admin@hria.local"); // el login genera un registro de auditoría
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/audit");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("total").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Audit_Unauthenticated_Returns401()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/audit");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
