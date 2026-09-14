namespace H5P_Samkør_Web_Api.Models;

// Bedømmelse af medrejsende, oprettes når en gennemført
// tur bedømmes af enten chaufføren eller en passager
public class Rating
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    // Brugeren der afgiver bedømmelsen
    public Guid RaterId { get; set; }
    public User Rater { get; set; } = null!;

    // Brugeren der bliver bedømt
    public Guid RateeId { get; set; }
    public User Ratee { get; set; } = null!;

    public int Stars { get; set; } // 1-5
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
