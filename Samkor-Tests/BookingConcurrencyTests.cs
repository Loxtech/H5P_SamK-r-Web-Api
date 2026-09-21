using System.Net;
using System.Net.Http.Json;
using SamKor.Api.Tests.Helpers;
using Xunit;

namespace SamKor.Api.Tests;

[Collection("SamKor API Collection")]
public class BookingConcurrencyTests
{
    private readonly CustomWebApplicationFactory _factory;

    public BookingConcurrencyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Krav 5 - beviser at to samtidige godkendelser af den sidste ledige
    // plads ikke begge kan lykkes. Bruger tre rigtige, registrerede
    // brugere og sender to reelle, parallelle HTTP-kald mod samme
    // endpoint for at genskabe den faktiske race condition, i stedet for
    // at simulere den - det er derfor testen kræver en rigtig database
    // (RowVersion/optimistic concurrency virker ikke med EF Core's
    // In-Memory-provider).
    [Fact]
    public async Task AcceptBooking_TwoSimultaneousRequestsForLastSeat_OnlyOneSucceeds()
    {
        var (driver, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Chauffør", $"{Guid.NewGuid()}@test.dk");
        var (passengerA, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Passager A", $"{Guid.NewGuid()}@test.dk");
        var (passengerB, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Passager B", $"{Guid.NewGuid()}@test.dk");

        // Chaufføren opretter en tur med kun én ledig plads
        var tripResponse = await driver.PostAsJsonAsync("/api/Trips", new
        {
            fromCity = "København",
            toCity = "Aarhus",
            departureTime = DateTime.UtcNow.AddDays(1),
            availableSeats = 1,
            pricePerSeat = 100m,
            vehicleId = (Guid?)null
        });
        tripResponse.EnsureSuccessStatusCode();
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDto>();

        // Begge passagerer anmoder om samme (eneste) ledige plads
        var bookingAResponse = await passengerA.PostAsJsonAsync("/api/Bookings", new { tripId = trip!.Id });
        bookingAResponse.EnsureSuccessStatusCode();
        var bookingA = await bookingAResponse.Content.ReadFromJsonAsync<BookingDto>();

        var bookingBResponse = await passengerB.PostAsJsonAsync("/api/Bookings", new { tripId = trip.Id });
        bookingBResponse.EnsureSuccessStatusCode();
        var bookingB = await bookingBResponse.Content.ReadFromJsonAsync<BookingDto>();

        // To separate, autentificerede clients (samme chauffør), så
        // begge requests reelt kan afsendes samtidig via Task.WhenAll
        var driverClientA = _factory.CreateClient();
        driverClientA.DefaultRequestHeaders.Authorization = driver.DefaultRequestHeaders.Authorization;
        var driverClientB = _factory.CreateClient();
        driverClientB.DefaultRequestHeaders.Authorization = driver.DefaultRequestHeaders.Authorization;

        var acceptATask = driverClientA.PutAsync($"/api/Bookings/{bookingA!.Id}/accept", null);
        var acceptBTask = driverClientB.PutAsync($"/api/Bookings/{bookingB!.Id}/accept", null);

        await Task.WhenAll(acceptATask, acceptBTask);

        var statusCodes = new[] { acceptATask.Result.StatusCode, acceptBTask.Result.StatusCode };

        // Præcis én af de to skal lykkes (204), den anden skal afvises (409) -
        // aldrig begge, og aldrig ingen af dem
        Assert.Contains(HttpStatusCode.NoContent, statusCodes);
        Assert.Contains(HttpStatusCode.Conflict, statusCodes);
    }
}
