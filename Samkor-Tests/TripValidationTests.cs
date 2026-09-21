using System.Net;
using System.Net.Http.Json;
using SamKor.Api.Tests.Helpers;
using Xunit;

namespace SamKor.Api.Tests;

[Collection("SamKor API Collection")]
public class TripValidationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public TripValidationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateTrip_WithPastDepartureTime_ReturnsBadRequest()
    {
        var (client, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Chauffør Test", $"{Guid.NewGuid()}@test.dk");

        var response = await client.PostAsJsonAsync("/api/Trips", new
        {
            fromCity = "København",
            toCity = "Aarhus",
            departureTime = DateTime.UtcNow.AddDays(-1),
            availableSeats = 3,
            pricePerSeat = 100m,
            vehicleId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTrip_WithValidData_ReturnsCreated()
    {
        var (client, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Chauffør Test", $"{Guid.NewGuid()}@test.dk");

        var response = await client.PostAsJsonAsync("/api/Trips", new
        {
            fromCity = "København",
            toCity = "Odense",
            departureTime = DateTime.UtcNow.AddDays(1),
            availableSeats = 2,
            pricePerSeat = 75m,
            vehicleId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}