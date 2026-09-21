using System.Net;
using System.Net.Http.Json;
using SamKor.Api.Tests.Helpers;
using Xunit;

namespace SamKor.Api.Tests;

[Collection("SamKor API Collection")]
public class TripAuthorizationTests
{
    private readonly CustomWebApplicationFactory _factory;

    public TripAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateTrip_ByNonOwner_ReturnsForbidden()
    {
        var (driver, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Chauffør", $"{Guid.NewGuid()}@test.dk");
        var (otherUser, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "En Anden", $"{Guid.NewGuid()}@test.dk");

        var tripResponse = await driver.PostAsJsonAsync("/api/Trips", new
        {
            fromCity = "København",
            toCity = "Vejle",
            departureTime = DateTime.UtcNow.AddDays(1),
            availableSeats = 3,
            pricePerSeat = 100m,
            vehicleId = (Guid?)null
        });
        tripResponse.EnsureSuccessStatusCode();
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDto>();

        var response = await otherUser.PutAsJsonAsync($"/api/Trips/{trip!.Id}", new
        {
            fromCity = "København",
            toCity = "Vejle",
            departureTime = DateTime.UtcNow.AddDays(2),
            availableSeats = 3,
            pricePerSeat = 110m,
            vehicleId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTrip_WhenFullyBooked_ReturnsConflict()
    {
        var (driver, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Chauffør", $"{Guid.NewGuid()}@test.dk");
        var (passenger, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Passager", $"{Guid.NewGuid()}@test.dk");

        var tripResponse = await driver.PostAsJsonAsync("/api/Trips", new
        {
            fromCity = "København",
            toCity = "Roskilde",
            departureTime = DateTime.UtcNow.AddDays(1),
            availableSeats = 1,
            pricePerSeat = 40m,
            vehicleId = (Guid?)null
        });
        tripResponse.EnsureSuccessStatusCode();
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDto>();

        var bookingResponse = await passenger.PostAsJsonAsync("/api/Bookings", new { tripId = trip!.Id });
        bookingResponse.EnsureSuccessStatusCode();
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingDto>();

        var acceptResponse = await driver.PutAsync($"/api/Bookings/{booking!.Id}/accept", null);
        acceptResponse.EnsureSuccessStatusCode(); // turen er nu fuldt booket

        var updateResponse = await driver.PutAsJsonAsync($"/api/Trips/{trip.Id}", new
        {
            fromCity = "København",
            toCity = "Roskilde",
            departureTime = DateTime.UtcNow.AddDays(2),
            availableSeats = 1,
            pricePerSeat = 45m,
            vehicleId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
    }
}
