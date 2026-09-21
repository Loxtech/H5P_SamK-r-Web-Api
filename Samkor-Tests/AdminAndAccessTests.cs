using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SamKor.Api.Tests.Helpers;
using Xunit;

namespace SamKor.Api.Tests;

[Collection("SamKor API Collection")]
public class AdminAndAccessTests
{
    private readonly CustomWebApplicationFactory _factory;

    public AdminAndAccessTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Krav 8 - der skal automatisk findes en administrator, når
    // systemet starter. Bemærk: denne test forudsætter, at
    // SeedAdmin-sektionen i appsettings.json stadig indeholder
    // admin@samkor.dk / Admin1234! - ret testen, hvis du ændrer dem.
    [Fact]
    public async Task SeededAdministrator_CanLogIn_AndHasAdministratorRole()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "admin@samkor.dk",
            password = "Admin1234!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.Contains("Administrator", auth!.Roles);
    }

    // Krav 8 - administrator kan redigere en tur, selvom vedkommende
    // hverken er chaufføren, eller turen er fuldt booket - begge
    // restriktioner skal ignoreres netop for administratorer
    [Fact]
    public async Task UpdateTrip_ByAdministrator_BypassesOwnershipAndFullyBookedRestriction()
    {
        var (driver, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Chauffør", $"{Guid.NewGuid()}@test.dk");
        var (passenger, _) = await AuthHelper.RegisterAndAuthenticateAsync(
            _factory.CreateClient(), "Passager", $"{Guid.NewGuid()}@test.dk");

        var tripResponse = await driver.PostAsJsonAsync("/api/Trips", new
        {
            fromCity = "København",
            toCity = "Esbjerg",
            departureTime = DateTime.UtcNow.AddDays(1),
            availableSeats = 1,
            pricePerSeat = 90m,
            vehicleId = (Guid?)null
        });
        tripResponse.EnsureSuccessStatusCode();
        var trip = await tripResponse.Content.ReadFromJsonAsync<TripDto>();

        var bookingResponse = await passenger.PostAsJsonAsync("/api/Bookings", new { tripId = trip!.Id });
        bookingResponse.EnsureSuccessStatusCode();
        var booking = await bookingResponse.Content.ReadFromJsonAsync<BookingDto>();

        var acceptResponse = await driver.PutAsync($"/api/Bookings/{booking!.Id}/accept", null);
        acceptResponse.EnsureSuccessStatusCode(); // turen er nu fuldt booket

        var adminClient = _factory.CreateClient();
        var adminLogin = await adminClient.PostAsJsonAsync("/api/Auth/login", new
        {
            email = "admin@samkor.dk",
            password = "Admin1234!"
        });
        adminLogin.EnsureSuccessStatusCode();
        var adminAuth = await adminLogin.Content.ReadFromJsonAsync<AuthResponseDto>();
        adminClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", adminAuth!.Token);

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/Trips/{trip.Id}", new
        {
            fromCity = "København",
            toCity = "Esbjerg",
            departureTime = DateTime.UtcNow.AddDays(2),
            availableSeats = 0,
            pricePerSeat = 95m,
            vehicleId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);
    }
}
