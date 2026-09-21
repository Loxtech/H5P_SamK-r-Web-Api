using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using H5P_Samkør_Web_Api.Data;
using H5P_Samkør_Web_Api.Extensions;
using H5P_Samkør_Web_Api.Models.DTOs;

namespace H5P_Samkør_Web_Api.Controllers;

[ApiController]
[Route("api/trips/{tripId:guid}/messages")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly AppDbContext _db;

    public MessagesController(AppDbContext db)
    {
        _db = db;
    }

    // Beskedhistorik for en tur - bruges når man åbner en chat, SignalR
    // dækker kun beskeder der sendes, mens man er forbundet
    [HttpGet]
    public async Task<ActionResult<IEnumerable<MessageResponse>>> GetMessages(Guid tripId)
    {
        var trip = await _db.Trips.FindAsync(tripId);
        if (trip is null)
            return NotFound();

        var isAdmin = User.IsInRole("Administrator");
        if (!isAdmin && !await _db.IsParticipant(trip, User.GetUserId()))
            return Forbid();

        var messages = await _db.Messages
            .Include(m => m.Sender)
            .Where(m => m.TripId == tripId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return Ok(messages.Select(m => new MessageResponse(
            m.Id, m.TripId, m.SenderId, m.Sender.FullName, m.Content, m.SentAt)));
    }
}
