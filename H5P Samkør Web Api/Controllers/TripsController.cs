using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using H5P_Samkør_Web_Api.Data;
using H5P_Samkør_Web_Api.Models;
using H5P_Samkør_Web_Api.Models.DTOs;

namespace H5P_Samkør_Web_Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TripsController(AppDbContext db)
    {
        _db = db;
    }

    // Krav 3 - Søgning efter ture. Åben for alle, også uden login,
    // så man kan browse ture før man opretter en profil.
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<TripResponse>>> GetTrips(
        [FromQuery] string? from,
        [FromQuery] string? to,
        [FromQuery] DateOnly? date)
    {
        var query = _db.Trips.Include(t => t.Driver).AsQueryable();

        if (!string.IsNullOrWhiteSpace(from))
            query = query.Where(t => t.FromCity.Contains(from));

        if (!string.IsNullOrWhiteSpace(to))
            query = query.Where(t => t.ToCity.Contains(to));

        if (date.HasValue)
        {
            var start = date.Value.ToDateTime(TimeOnly.MinValue);
            var end = start.AddDays(1);
            query = query.Where(t => t.DepartureTime >= start && t.DepartureTime < end);
        }

        var trips = await query
            .OrderBy(t => t.DepartureTime)
            .ToListAsync();

        return Ok(trips.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<TripResponse>> GetTrip(Guid id)
    {
        var trip = await _db.Trips.Include(t => t.Driver).FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound();

        return Ok(ToResponse(trip));
    }

    // Krav 2 - Oprettelse af tur
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TripResponse>> CreateTrip(CreateTripRequest request)
    {
        if (request.DepartureTime <= DateTime.UtcNow)
            return BadRequest("Afgangstidspunktet skal ligge i fremtiden.");

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            DriverId = GetCurrentUserId(),
            FromCity = request.FromCity,
            ToCity = request.ToCity,
            DepartureTime = request.DepartureTime,
            AvailableSeats = request.AvailableSeats,
            PricePerSeat = request.PricePerSeat,
            VehicleId = request.VehicleId
        };

        _db.Trips.Add(trip);
        await _db.SaveChangesAsync();
        await _db.Entry(trip).Reference(t => t.Driver).LoadAsync();

        return CreatedAtAction(nameof(GetTrip), new { id = trip.Id }, ToResponse(trip));
    }

    // Redigering kun chaufføren selv eller en administrator,
    // og kun så længe turen ikke er fuldt booket
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateTrip(Guid id, UpdateTripRequest request)
    {
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound();

        var isAdmin = User.IsInRole("Administrator");
        if (trip.DriverId != GetCurrentUserId() && !isAdmin)
            return Forbid();

        if (trip.AvailableSeats == 0 && !isAdmin)
            return Conflict("Turen er fuldt booket og kan ikke længere redigeres.");

        if (request.DepartureTime <= DateTime.UtcNow)
            return BadRequest("Afgangstidspunktet skal ligge i fremtiden.");

        trip.FromCity = request.FromCity;
        trip.ToCity = request.ToCity;
        trip.DepartureTime = request.DepartureTime;
        trip.AvailableSeats = request.AvailableSeats;
        trip.PricePerSeat = request.PricePerSeat;
        trip.VehicleId = request.VehicleId;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // Aflysning samme regler som redigering (Krav 2 + Krav 8)
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteTrip(Guid id)
    {
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound();

        var isAdmin = User.IsInRole("Administrator");
        if (trip.DriverId != GetCurrentUserId() && !isAdmin)
            return Forbid();

        if (trip.AvailableSeats == 0 && !isAdmin)
            return Conflict("Turen er fuldt booket og kan ikke længere aflyses.");

        _db.Trips.Remove(trip);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(idClaim!);
    }

    private static TripResponse ToResponse(Trip trip) => new(
        trip.Id,
        trip.DriverId,
        trip.Driver.FullName,
        trip.FromCity,
        trip.ToCity,
        trip.DepartureTime,
        trip.AvailableSeats,
        trip.PricePerSeat,
        trip.VehicleId
    );
}
