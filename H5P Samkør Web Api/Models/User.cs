namespace H5P_Samkør_Web_Api.Models;

public class User
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Phone { get; set; }

    // "User" eller "Administrator",se 2.6 i kravspecifikationen
    public string Role { get; set; } = "User";

    // Cachet gennemsnitlig rating, opdateres når en ny Rating oprettes (Krav 7)
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
