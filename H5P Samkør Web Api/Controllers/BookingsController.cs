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
public class BookingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public BookingsController(AppDbContext db)
    {
        _db = db;
    }

    // Krav 4 - Anmode om booking
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> CreateBooking(CreateBookingRequest request)
    {
        var trip = await _db.Trips.FirstOrDefaultAsync(t => t.Id == request.TripId);
        if (trip is null)
            return NotFound("Turen findes ikke.");

        if (trip.HasDeparted())
            return BadRequest("Turen er allerede kørt.");

        var currentUserId = User.GetUserId();

        if (trip.DriverId == currentUserId)
            return BadRequest("Du kan ikke booke din egen tur.");

        if (trip.AvailableSeats <= 0)
            return Conflict("Der er ingen ledige pladser på denne tur.");

        var alreadyActive = await _db.Bookings.AnyAsync(b =>
            b.TripId == trip.Id &&
            b.PassengerId == currentUserId &&
            (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Accepted));

        if (alreadyActive)
            return Conflict("Du har allerede en aktiv anmodning eller booking på denne tur.");

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            TripId = trip.Id,
            PassengerId = currentUserId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        await _db.Entry(booking).Reference(b => b.Trip).LoadAsync();
        await _db.Entry(booking).Reference(b => b.Passenger).LoadAsync();

        return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, ToResponse(booking));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingResponse>> GetBooking(Guid id)
    {
        var booking = await _db.Bookings
            .Include(b => b.Trip)
            .Include(b => b.Passenger)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking is null)
            return NotFound();

        var currentUserId = User.GetUserId();
        var isAdmin = User.IsInRole("Administrator");

        if (booking.PassengerId != currentUserId && booking.Trip.DriverId != currentUserId && !isAdmin)
            return Forbid();

        return Ok(ToResponse(booking));
    }

    // Egne bookinger som passager
    [HttpGet("mine")]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetMyBookings()
    {
        var currentUserId = User.GetUserId();

        var bookings = await _db.Bookings
            .Include(b => b.Trip)
            .Include(b => b.Passenger)
            .Where(b => b.PassengerId == currentUserId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bookings.Select(ToResponse));
    }

    // Afventende/behandlede anmodninger på egne ture som chauffør
    [HttpGet("received")]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetReceivedBookings()
    {
        var currentUserId = User.GetUserId();

        var bookings = await _db.Bookings
            .Include(b => b.Trip)
            .Include(b => b.Passenger)
            .Where(b => b.Trip.DriverId == currentUserId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bookings.Select(ToResponse));
    }

    // Krav 5 - Godkende booking. Nedjusterer AvailableSeats atomisk via
    // Trip.RowVersion (concurrency token), så to samtidige godkendelser
    // ikke begge kan gå igennem på den sidste ledige plads.
    [HttpPut("{id:guid}/accept")]
    public async Task<IActionResult> AcceptBooking(Guid id)
    {
        var booking = await _db.Bookings.Include(b => b.Trip).FirstOrDefaultAsync(b => b.Id == id);
        if (booking is null)
            return NotFound();

        var isAdmin = User.IsInRole("Administrator");
        if (booking.Trip.DriverId != User.GetUserId() && !isAdmin)
            return Forbid();

        if (booking.Status != BookingStatus.Pending)
            return Conflict("Bookingen er allerede behandlet.");

        if (booking.Trip.AvailableSeats <= 0)
            return Conflict("Turen har ikke flere ledige pladser.");

        booking.Status = BookingStatus.Accepted;
        booking.Trip.AvailableSeats -= 1;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // En anden anmodning nåede at ændre turen først (fx en anden
            // godkendelse), siden vi hentede den. Klienten bør hente
            // turen på ny og evt. prøve igen.
            return Conflict("Turen blev opdateret samtidig af en anden anmodning. Prøv igen.");
        }

        return NoContent();
    }

    // Krav 5 - Afvise booking
    [HttpPut("{id:guid}/reject")]
    public async Task<IActionResult> RejectBooking(Guid id)
    {
        var booking = await _db.Bookings.Include(b => b.Trip).FirstOrDefaultAsync(b => b.Id == id);
        if (booking is null)
            return NotFound();

        var isAdmin = User.IsInRole("Administrator");
        if (booking.Trip.DriverId != User.GetUserId() && !isAdmin)
            return Forbid();

        if (booking.Status != BookingStatus.Pending)
            return Conflict("Bookingen er allerede behandlet.");

        booking.Status = BookingStatus.Rejected;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // Krav 8 - administrator kan fjerne en booking helt (til
    // moderation), i modsætning til afvis, som kun gælder afventende
    // anmodninger. Frigiver automatisk pladsen igen, hvis bookingen
    // var godkendt.
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> DeleteBooking(Guid id)
    {
        var booking = await _db.Bookings.Include(b => b.Trip).FirstOrDefaultAsync(b => b.Id == id);
        if (booking is null)
            return NotFound();

        if (booking.Status == BookingStatus.Accepted)
            booking.Trip.AvailableSeats += 1;

        _db.Bookings.Remove(booking);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static BookingResponse ToResponse(Booking booking) => new(
        booking.Id,
        booking.TripId,
        booking.Trip.FromCity,
        booking.Trip.ToCity,
        booking.Trip.DepartureTime,
        booking.PassengerId,
        booking.Passenger.FullName,
        booking.Status.ToString(),
        booking.CreatedAt,
        booking.Passenger.AverageRating,
        booking.Passenger.RatingCount
    );
}