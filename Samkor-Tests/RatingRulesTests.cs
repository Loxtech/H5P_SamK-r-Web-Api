using System.Net;
using System.Net.Http.Json;
using H5P_Samkør_Web_Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SamKor.Api.Tests.Helpers;
using Xunit;

namespace SamKor.Api.Tests;

[Collection("SamKor API Collection")]
public class RatingRulesTests
{
    private readonly CustomWebApplicationFactory _factory;

    public RatingRulesTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Bedømmelse kræver en gennemført tur, men API'et forhindrer med
    // vilje oprettelse af ture med et afgangstidspunkt i fortiden
    // (Krav 2). Til testens arrange-del går vi derfor udenom API'et og
    // sætter turen direkte i databasen - selve testen af
    // bedømmelseslogikken sker stadig gennem det rigtige endpoint.
    private async Task MarkTripAsCompletedAsync(Guid tripId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var trip = await db.Trips.FirstAsync(t => t.Id == tripId);
        trip.DepartureTime = DateTime.UtcNow.AddDays(-1);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateRating_OnSelf_ReturnsBadRequest()
    {
        var (driver, driverId) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Chauffør", $"{Guid.NewGuid()}@test.dk");

        var tripResponse = await driver.PostAsJsonAsync("/api/Trips", new
        {
            fromCity = "København",
            toCity = "Aarhus",
            departureTime = DateTime.UtcNow.AddDays(1),
            availableSeats = 3,
            pricePerSeat = 100m,
            vehicleId = (Guid?)null
        });
        tripResponse.EnsureSuccessStatusCode();
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDto>();

        await MarkTripAsCompletedAsync(trip!.Id);

        var response = await driver.PostAsJsonAsync("/api/Ratings", new
        {
            tripId = trip.Id,
            rateeId = driverId,
            stars = 5,
            comment = (string?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateRating_Twice_ReturnsConflict()
    {
        var (driver, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Chauffør", $"{Guid.NewGuid()}@test.dk");
        var (passenger, passengerId) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Passager", $"{Guid.NewGuid()}@test.dk");

        var tripResponse = await driver.PostAsJsonAsync("/api/Trips", new
        {
            fromCity = "København",
            toCity = "Odense",
            departureTime = DateTime.UtcNow.AddDays(1),
            availableSeats = 2,
            pricePerSeat = 80m,
            vehicleId = (Guid?)null
        });
        tripResponse.EnsureSuccessStatusCode();
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDto>();

        var bookingResponse = await passenger.PostAsJsonAsync("/api/Bookings", new { tripId = trip!.Id });
        bookingResponse.EnsureSuccessStatusCode();
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingDto>();

        var acceptResponse = await driver.PutAsync($"/api/Bookings/{booking!.Id}/accept", null);
        acceptResponse.EnsureSuccessStatusCode();

        await MarkTripAsCompletedAsync(trip.Id);

        var first = await driver.PostAsJsonAsync("/api/Ratings", new
        {
            tripId = trip.Id,
            rateeId = passengerId,
            stars = 5,
            comment = (string?)null
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await driver.PostAsJsonAsync("/api/Ratings", new
        {
            tripId = trip.Id,
            rateeId = passengerId,
            stars = 4,
            comment = (string?)null
        });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }
}
