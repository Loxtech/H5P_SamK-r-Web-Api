using Microsoft.AspNetCore.Identity;

namespace H5P_Samkør_Web_Api.Models;

// Arver Email, PasswordHash, PhoneNumber m.m. fra IdentityUser<Guid>.
// Rollerne "User" og "Administrator" håndteres af Identity's eget
// rollesystem (AspNetRoles/AspNetUserRoles), ikke som et felt her.
public class User : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    // Cachet gennemsnitlig rating, opdateres når en ny Rating oprettes
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Trip> TripsAsDriver { get; set; } = new List<Trip>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<Rating> RatingsGiven { get; set; } = new List<Rating>();
    public ICollection<Rating> RatingsReceived { get; set; } = new List<Rating>();
}