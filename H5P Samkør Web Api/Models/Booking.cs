namespace H5P_Samkør_Web_Api.Models;

public class Booking
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public Guid PassengerId { get; set; }
    public User Passenger { get; set; } = null!;

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
