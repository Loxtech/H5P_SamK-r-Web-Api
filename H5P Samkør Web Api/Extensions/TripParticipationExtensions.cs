using Microsoft.EntityFrameworkCore;
using H5P_Samkør_Web_Api.Data;
using H5P_Samkør_Web_Api.Models;

namespace H5P_Samkør_Web_Api.Extensions;

public static class TripParticipationExtensions
{
    // En bruger regnes som deltager på en tur, hvis vedkommende enten
    // er chaufføren, eller har en godkendt booking på turen. Bruges af
    // Ratings, Chat og beskedhistorik, så adgangsreglen kun findes ét sted.
    public static async Task<bool> IsParticipant(this AppDbContext db, Trip trip, Guid userId)
    {
        if (trip.DriverId == userId)
            return true;

        return await db.Bookings.AnyAsync(b =>
            b.TripId == trip.Id &&
            b.PassengerId == userId &&
            b.Status == BookingStatus.Accepted);
    }
}
