using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using H5P_Samkør_Web_Api.Data;
using H5P_Samkør_Web_Api.Extensions;
using H5P_Samkør_Web_Api.Models;
using H5P_Samkør_Web_Api.Models.DTOs;

namespace H5P_Samkør_Web_Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RatingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public RatingsController(AppDbContext db)
    {
        _db = db;
    }

    // Bedømmelse af medrejsende - afgiv en bedømmelse efter en gennemført tur
    [HttpPost]
    public async Task<ActionResult<RatingResponse>> CreateRating(CreateRatingRequest request)
    {
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId);
        if (trip is null)
            return NotFound("Turen findes ikke.");

        // Genbruger samme "gennemført"-definition som Krav 7-oversigten
        if (!trip.IsCompleted())
            return BadRequest("Turen er endnu ikke gennemført og kan ikke bedømmes.");

        var currentUserId = User.GetUserId();

        if (request.RateeId == currentUserId)
            return BadRequest("Du kan ikke bedømme dig selv.");

        if (!await _db.IsParticipant(trip, currentUserId))
            return Forbid();

        if (!await _db.IsParticipant(trip, request.RateeId))
            return BadRequest("Den valgte bruger deltog ikke i denne tur.");

        var alreadyRated = await _db.Ratings.AnyAsync(r =>
            r.TripId == trip.Id && r.RaterId == currentUserId && r.RateeId == request.RateeId);

        if (alreadyRated)
            return Conflict("Du har allerede bedømt denne medrejsende for denne tur.");

        var rating = new Rating
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            RaterId = currentUserId,
            RateeId = request.RateeId,
            Stars = request.Stars,
            Comment = request.Comment,
            CreatedAt = DateTime.UtcNow
        };

        _db.Ratings.Add(rating);

        // Opdater modtagerens cachede gennemsnit løbende, så vi undgår
        // at skulle beregne det fra bunden af alle Ratings hver gang
        var ratee = await _db.Users.FirstAsync(u => u.Id == request.RateeId);
        var newCount = ratee.RatingCount + 1;
        ratee.AverageRating = ((ratee.AverageRating * ratee.RatingCount) + request.Stars) / newCount;
        ratee.RatingCount = newCount;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Databasens unikke indeks (TripId, RaterId, RateeId) er sidste
            // sikkerhedsnet, hvis to identiske requests rammer samtidigt.
            return Conflict("Du har allerede bedømt denne medrejsende for denne tur.");
        }

        await _db.Entry(rating).Reference(r => r.Rater).LoadAsync();
        await _db.Entry(rating).Reference(r => r.Ratee).LoadAsync();

        return CreatedAtAction(nameof(GetRating), new { id = rating.Id }, ToResponse(rating));
    }

    // Så frontend kan vise, hvilke medrejsende brugeren allerede har
    // bedømt for en given tur, og undgå at vise formularen for dem igen
    [HttpGet("trip/{tripId:guid}/mine")]
    public async Task<ActionResult<IEnumerable<RatingResponse>>> GetMyRatingsForTrip(Guid tripId)
    {
        var currentUserId = User.GetUserId();

        var ratings = await _db.Ratings
            .Include(r => r.Rater)
            .Include(r => r.Ratee)
            .Where(r => r.TripId == tripId && r.RaterId == currentUserId)
            .ToListAsync();

        return Ok(ratings.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<RatingResponse>> GetRating(Guid id)
    {
        var rating = await _db.Ratings
            .Include(r => r.Rater)
            .Include(r => r.Ratee)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rating is null)
            return NotFound();

        return Ok(ToResponse(rating));
    }

    // Så andre brugere kan se en bruges samlede rating, fx før en
    // booking-anmodning godkendes
    [HttpGet("user/{userId:guid}/summary")]
    [AllowAnonymous]
    public async Task<ActionResult<UserRatingSummary>> GetUserRatingSummary(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user is null)
            return NotFound();

        return Ok(new UserRatingSummary(user.Id, user.FullName, Math.Round(user.AverageRating, 2), user.RatingCount));
    }

    private static RatingResponse ToResponse(Rating rating) => new(
        rating.Id,
        rating.TripId,
        rating.RaterId,
        rating.Rater.FullName,
        rating.RateeId,
        rating.Ratee.FullName,
        rating.Stars,
        rating.Comment,
        rating.CreatedAt
    );
}