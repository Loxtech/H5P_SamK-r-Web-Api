using System.Net;
using System.Net.Http.Json;
using SamKor.Api.Tests.Helpers;
using Xunit;

namespace SamKor.Api.Tests;

[Collection("SamKor API Collection")]
public class BookingRulesTests
{
    private readonly CustomWebApplicationFactory _factory;

    public BookingRulesTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateBooking_OnOwnTrip_ReturnsBadRequest()
    {
        var (driver, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Chauffør", $"{Guid.NewGuid()}@test.dk");

        var tripResponse = await driver.PostAsJsonAsync("/api/Trips", new
        {
            fromCity = "København",
            toCity = "Aalborg",
            departureTime = DateTime.UtcNow.AddDays(1),
            availableSeats = 3,
            pricePerSeat = 120m,
            vehicleId = (Guid?)null
        });
        tripResponse.EnsureSuccessStatusCode();
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDto>();

        var response = await driver.PostAsJsonAsync("/api/Bookings", new { tripId = trip!.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateBooking_WhenNoSeatsAvailable_ReturnsConflict()
    {
        var (driver, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Chauffør", $"{Guid.NewGuid()}@test.dk");
        var (passengerA, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Passager A", $"{Guid.NewGuid()}@test.dk");
        var (passengerB, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Passager B", $"{Guid.NewGuid()}@test.dk");

        var tripResponse = await driver.PostAsJsonAsync("/api/Trips", new
        {
            fromCity = "København",
            toCity = "Kolding",
            departureTime = DateTime.UtcNow.AddDays(1),
            availableSeats = 1,
            pricePerSeat = 60m,
            vehicleId = (Guid?)null
        });
        tripResponse.EnsureSuccessStatusCode();
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDto>();

        // Passager A tager den eneste ledige plads
        var bookingAResponse = await passengerA.PostAsJsonAsync("/api/Bookings", new { tripId = trip!.Id });
        bookingAResponse.EnsureSuccessStatusCode();
        var bookingA = await bookingAResponse.Content.ReadFromJsonAsync<BookingDto>();

        var acceptResponse = await driver.PutAsync($"/api/Bookings/{bookingA!.Id}/accept", null);
        acceptResponse.EnsureSuccessStatusCode(); // turen har nu 0 ledige pladser

        // Passager B forsøger at anmode om en plads, der ikke længere findes
        var bookingBResponse = await passengerB.PostAsJsonAsync("/api/Bookings", new { tripId = trip.Id });

        Assert.Equal(HttpStatusCode.Conflict, bookingBResponse.StatusCode);
    }
}
