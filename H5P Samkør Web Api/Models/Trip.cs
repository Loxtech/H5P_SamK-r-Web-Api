namespace H5P_Samkør_Web_Api.Models;

public class Trip
{
    public Guid Id { get; set; }

    public Guid DriverId { get; set; }
    public User Driver { get; set; } = null!;

    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public string FromCity { get; set; } = string.Empty;
    public string ToCity { get; set; } = string.Empty;
    public DateTime DepartureTime { get; set; }
    public int AvailableSeats { get; set; }
    public decimal PricePerSeat { get; set; }

    // Concurrency token, forhindrer at to samtidige godkendelser
    // begge kan nedjustere AvailableSeats på samme tid (Krav 5)
    public byte[]? RowVersion { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
