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
        [FromQuery] DateTime? departureAfter)
    {
        var query = _db.Trips.Include(t => t.Driver).AsQueryable();

        // Søgning viser kun kommende ture - en tur, der allerede er
        // kørt, er ikke relevant at finde eller booke sig på.
        // "now" beregnes i dansk tid, da DepartureTime er gemt som
        // lokal (dansk) tid uden UTC-offset - se TripTimeExtensions.
        var now = TripTimeExtensions.NowInDenmark();
        query = query.Where(t => t.DepartureTime > now);

        if (!string.IsNullOrWhiteSpace(from))
            query = query.Where(t => t.FromCity.Contains(from));

        if (!string.IsNullOrWhiteSpace(to))
            query = query.Where(t => t.ToCity.Contains(to));

        // Finder ture, der afgår på eller efter det valgte tidspunkt,
        // i stedet for kun ture på én bestemt dag - så man fx kan søge
        // "tidligst kl. 14" og få alle relevante ture fra da af
        if (departureAfter.HasValue)
            query = query.Where(t => t.DepartureTime >= departureAfter.Value);

        var trips = await query
            .OrderBy(t => t.DepartureTime)
            .ToListAsync();

        return Ok(trips.Select(ToResponse));
    }

    // Krav 7 - Oversigt over planlagte og gennemførte ture, både som
    // chauffør og som passager. Placeres før "{id:guid}", så "mine"
    // ikke fejlagtigt forsøges parset som et GUID.
    [HttpGet("mine")]
    [Authorize]
    public async Task<ActionResult<TripOverviewResponse>> GetMyTripsOverview()
    {
        var currentUserId = User.GetUserId();

        var driverTrips = await _db.Trips
            .Where(t => t.DriverId == currentUserId)
            .ToListAsync();

        var passengerBookings = await _db.Bookings
            .Include(b => b.Trip)
            .Where(b => b.PassengerId == currentUserId &&
                        (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Accepted))
            .ToListAsync();

        var items = new List<TripOverviewItem>();

        items.AddRange(driverTrips.Select(t => new TripOverviewItem(
            t.Id, t.FromCity, t.ToCity, t.DepartureTime,
            "Chauffør", "Oprettet", t.IsCompleted())));

        items.AddRange(passengerBookings.Select(b => new TripOverviewItem(
            b.TripId, b.Trip.FromCity, b.Trip.ToCity, b.Trip.DepartureTime,
            "Passager", b.Status.ToString(), b.Trip.IsCompleted())));

        var ordered = items.OrderBy(i => i.DepartureTime).ToList();

        return Ok(new TripOverviewResponse(
            Planned: ordered.Where(i => !i.IsCompleted),
            Completed: ordered.Where(i => i.IsCompleted)
        ));
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

    // Bedømmelse af medrejsende - liste over hvem der var med på en tur
    // (chauffør + passagerer med godkendt booking), så frontend kan vise
    // hvem der kan bedømmes. Kun tilgængelig for deltagere på turen selv.
    [HttpGet("{id:guid}/participants")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<ParticipantResponse>>> GetParticipants(Guid id)
    {
        var trip = await _db.Trips.Include(t => t.Driver).FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound();

        var currentUserId = User.GetUserId();
        var isAdmin = User.IsInRole("Administrator");

        if (!await _db.IsParticipant(trip, currentUserId) && !isAdmin)
            return Forbid();

        var participants = new List<ParticipantResponse>
        {
            new(trip.DriverId, trip.Driver.FullName, "Chauffør")
        };

        var passengers = await _db.Bookings
            .Include(b => b.Passenger)
            .Where(b => b.TripId == trip.Id && b.Status == BookingStatus.Accepted)
            .Select(b => new ParticipantResponse(b.PassengerId, b.Passenger.FullName, "Passager"))
            .ToListAsync();

        participants.AddRange(passengers);

        return Ok(participants);
    }

    // Krav 2 - Oprettelse af tur
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<TripResponse>> CreateTrip(CreateTripRequest request)
    {
        if (request.DepartureTime <= TripTimeExtensions.NowInDenmark())
            return BadRequest("Afgangstidspunktet skal ligge i fremtiden.");

        var trip = new Trip
        {
            Id = Guid.NewGuid(),
            DriverId = User.GetUserId(),
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

    // Redigering - kun chaufføren selv eller en administrator,
    // og kun så længe turen ikke er fuldt booket
    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> UpdateTrip(Guid id, UpdateTripRequest request)
    {
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound();

        var isAdmin = User.IsInRole("Administrator");
        if (trip.DriverId != User.GetUserId() && !isAdmin)
            return Forbid();

        if (trip.AvailableSeats == 0 && !isAdmin)
            return Conflict("Turen er fuldt booket og kan ikke længere redigeres.");

        if (request.DepartureTime <= TripTimeExtensions.NowInDenmark())
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

    // Aflysning - samme regler som redigering (Krav 2 + Krav 8)
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteTrip(Guid id)
    {
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == id);
        if (trip is null)
            return NotFound();

        var isAdmin = User.IsInRole("Administrator");
        if (trip.DriverId != User.GetUserId() && !isAdmin)
            return Forbid();

        if (trip.AvailableSeats == 0 && !isAdmin)
            return Conflict("Turen er fuldt booket og kan ikke længere aflyses.");

        _db.Trips.Remove(trip);
        await _db.SaveChangesAsync();
        return NoContent();
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
        trip.VehicleId,
        trip.Driver.AverageRating,
        trip.Driver.RatingCount
    );
}