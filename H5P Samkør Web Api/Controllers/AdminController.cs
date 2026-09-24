using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using H5P_Samkør_Web_Api.Data;
using H5P_Samkør_Web_Api.Models;
using H5P_Samkør_Web_Api.Models.DTOs;

namespace H5P_Samkør_Web_Api.Controllers;

// Krav 8 - administrator-dashboard. Hele controlleren kræver
// Administrator-rollen, så der er ingen grund til at gentage tjekket
// i hver enkelt action.
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<User> _userManager;

    public AdminController(AppDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("users")]
    public async Task<ActionResult<IEnumerable<AdminUserResponse>>> GetUsers()
    {
        var users = await _db.Users.OrderBy(u => u.FullName).ToListAsync();

        var result = new List<AdminUserResponse>();
        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var isLockedOut = await _userManager.IsLockedOutAsync(user);
            result.Add(new AdminUserResponse(user.Id, user.FullName, user.Email!, roles, isLockedOut));
        }

        return Ok(result);
    }

    // Deaktiverer en bruger via Identity's lockout-mekanisme, i stedet
    // for reel sletning. En hård sletning ville fejle på de fleste
    // brugere alligevel, da Trip/Booking/Message/Rating peger på User
    // med DeleteBehavior.Restrict (se AppDbContext) for at undgå flere
    // cascade-veje i SQL Server.
    [HttpPut("users/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return NotFound();

        await _userManager.SetLockoutEnabledAsync(user, true);
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        return NoContent();
    }

    [HttpPut("users/{id:guid}/reactivate")]
    public async Task<IActionResult> ReactivateUser(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user is null)
            return NotFound();

        await _userManager.SetLockoutEndDateAsync(user, null);

        return NoContent();
    }

    // Fuldt overblik til admin - i modsætning til det almindelige
    // søge-endpoint (GET /api/Trips) vises også fortidige/gennemførte
    // ture her, da administratoren skal kunne se og moderere alt
    [HttpGet("trips")]
    public async Task<ActionResult<IEnumerable<TripResponse>>> GetAllTrips()
    {
        var trips = await _db.Trips
            .Include(t => t.Driver)
            .OrderByDescending(t => t.DepartureTime)
            .ToListAsync();

        return Ok(trips.Select(t => new TripResponse(
            t.Id, t.DriverId, t.Driver.FullName, t.FromCity, t.ToCity,
            t.DepartureTime, t.AvailableSeats, t.PricePerSeat, t.VehicleId,
            t.Driver.AverageRating, t.Driver.RatingCount)));
    }

    [HttpGet("bookings")]
    public async Task<ActionResult<IEnumerable<BookingResponse>>> GetAllBookings()
    {
        var bookings = await _db.Bookings
            .Include(b => b.Trip)
            .Include(b => b.Passenger)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        return Ok(bookings.Select(b => new BookingResponse(
            b.Id, b.TripId, b.Trip.FromCity, b.Trip.ToCity, b.Trip.DepartureTime,
            b.PassengerId, b.Passenger.FullName, b.Status.ToString(), b.CreatedAt)));
    }
}   