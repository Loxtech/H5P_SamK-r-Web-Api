using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SamKor.Api.Tests;

[Collection("SamKor API Collection")]
public class AuthTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/Auth/register", new
        {
            fullName = "Test Testesen",
            email = $"{Guid.NewGuid()}@test.dk",
            password = "Test1234!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid()}@test.dk";

        var first = await client.PostAsJsonAsync("/api/Auth/register", new
        {
            fullName = "Første Bruger",
            email,
            password = "Test1234!"
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/Auth/register", new
        {
            fullName = "Anden Bruger",
            email,
            password = "Test1234!"
        });

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var email = $"{Guid.NewGuid()}@test.dk";

        await client.PostAsJsonAsync("/api/Auth/register", new
        {
            fullName = "Test Testesen",
            email,
            password = "Test1234!"
        });

        var response = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email,
            password = "ForkertKodeord1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // Krav 2 og alle andre beskyttede endpoints - dokumenterer at
    // autentificering rent faktisk håndhæves, ikke kun tilbydes
    [Fact]
    public async Task CreateTrip_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/Trips", new
        {
            fromCity = "København",
            toCity = "Aarhus",
            departureTime = DateTime.UtcNow.AddDays(1),
            availableSeats = 3,
            pricePerSeat = 100m,
            vehicleId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}