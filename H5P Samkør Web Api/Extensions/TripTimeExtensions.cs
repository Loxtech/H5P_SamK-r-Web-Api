using H5P_Samkør_Web_Api.Models;

namespace H5P_Samkør_Web_Api.Extensions;

// Trip.DepartureTime gemmes som den lokale (danske) tid, brugeren
// indtastede i formularen, uden tidszone-information. Al sammenligning
// mod "nu" skal derfor ske mod dansk tid, ikke DateTime.UtcNow direkte,
// ellers bliver resultatet 1-2 timer forkert afhængigt af sommer-/vintertid.
public static class TripTimeExtensions
{
    private static readonly TimeZoneInfo DanishTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Europe/Copenhagen");

    // Hvor lang tid efter afgang en tur regnes som gennemført,
    // så deltagerne kan nå at bedømme hinanden
    private static readonly TimeSpan CompletionBuffer = TimeSpan.FromMinutes(1);

    public static DateTime NowInDenmark() =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, DanishTimeZone);

    // Bruges hvor en tur skal afvises, fordi afgangstidspunktet allerede
    // er passeret (oprettelse, redigering, booking, søgning)
    public static bool HasDeparted(this Trip trip) =>
        trip.DepartureTime <= NowInDenmark();

    // Bruges til at afgøre om en tur kan bedømmes / vises som "gennemført"
    public static bool IsCompleted(this Trip trip) =>
        trip.DepartureTime.Add(CompletionBuffer) < NowInDenmark();
}