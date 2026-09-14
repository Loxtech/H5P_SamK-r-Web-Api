namespace H5P_Samkør_Web_Api.Models;

public class Vehicle
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;

    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Seats { get; set; }

    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
