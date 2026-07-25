using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace HRIA.Tests.Integration;

public class EmployeesEndpointsTests : IClassFixture<HriaWebAppFactory>
{
    private readonly HriaWebAppFactory _factory;

    public EmployeesEndpointsTests(HriaWebAppFactory factory) => _factory = factory;

    private async Task<string> LoginAsync(HttpClient client, string email)
    {
        var res = await client.PostAsJsonAsync("/api/auth/login",
            new { email, password = "Demo1234!" });
        res.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("token").GetString()!;
    }

    [Fact]
    public async Task Employee_CannotListEmployees_Returns403()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "empleado@hria.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/employees");

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_CanListEmployees_Returns200WithItems()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "admin@hria.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await client.GetAsync("/api/employees?page=1&pageSize=5");

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("total").GetInt32().Should().BeGreaterThan(0);
        body.GetProperty("items").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Admin_CanCreateEmployee_Returns201()
    {
        var client = _factory.CreateClient();
        var token = await LoginAsync(client, "admin@hria.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var depts = await (await client.GetAsync("/api/departments")).Content.ReadFromJsonAsync<JsonElement>();
        var deptId = depts[0].GetProperty("id").GetInt32();

        var create = await client.PostAsJsonAsync("/api/employees", new
        {
            firstName = "Test",
            lastName = "Integration",
            email = "test.integration@hria.local",
            departmentId = deptId,
            position = "Tester",
            hireDate = "2024-01-15",
            role = 2,
            initialPassword = "Secret123"
        });

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        created.GetProperty("email").GetString().Should().Be("test.integration@hria.local");
    }
}
