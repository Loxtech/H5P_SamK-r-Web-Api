using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace SamKor.Api.Tests.Helpers;

public static class AuthHelper
{
    // Registrerer en ny, unik bruger og sætter JWT-token'et som
    // Authorization-header på den givne HttpClient, så den er klar til
    // at kalde beskyttede endpoints med det samme.
    public static async Task<(HttpClient Client, Guid UserId)> RegisterAndAuthenticateAsync(
        HttpClient client, string fullName, string email, string password = "Test1234!")
    {
        var response = await client.PostAsJsonAsync("/api/Auth/register", new
        {
            fullName,
            email,
            password
        });

        response.EnsureSuccessStatusCode();
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);

        return (client, auth.UserId);
    }

    private record AuthResponseDto(
        Guid UserId,
        string Token,
        DateTime ExpiresAt,
        string FullName,
        string Email,
        List<string> Roles
    );
}
