using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace HRIA.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<HriaWebAppFactory>
{
    private readonly HriaWebAppFactory _factory;

    public AuthEndpointsTests(HriaWebAppFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Health_ReturnsHealthy()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/health");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithDemoAdmin_ReturnsToken()
    {
        var client = _factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@hria.local", password = "Demo1234!" });

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("user").GetProperty("role").GetInt32().Should().Be(1); // Admin
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = _factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "admin@hria.local", password = "incorrecta" });

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/api/auth/me");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsCurrentUser()
    {
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "empleado@hria.local", password = "Demo1234!" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("token").GetString();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var me = await client.GetAsync("/api/auth/me");

        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await me.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("email").GetString().Should().Be("empleado@hria.local");
        body.GetProperty("role").GetInt32().Should().Be(2); // Employee
    }
}
