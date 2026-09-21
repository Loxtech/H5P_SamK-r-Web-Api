using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using H5P_Samkør_Web_Api.Data;
using H5P_Samkør_Web_Api.Extensions;
using H5P_Samkør_Web_Api.Models;

namespace H5P_Samkør_Web_Api.Hubs;

// Krav 6 - Chat tilknyttet den enkelte tur. Kun chaufføren eller
// passagerer med en godkendt booking må tilslutte sig og sende beskeder.
[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;

    public ChatHub(AppDbContext db)
    {
        _db = db;
    }

    public async Task JoinTrip(Guid tripId)
    {
        var trip = await _db.Trips.FindAsync(tripId);
        if (trip is null)
            throw new HubException("Turen findes ikke.");

        var userId = Context.User!.GetUserId();
        if (!await _db.IsParticipant(trip, userId))
            throw new HubException("Du har ikke adgang til denne turs chat.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(tripId));
    }

    public async Task SendMessage(Guid tripId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new HubException("Beskeden må ikke være tom.");

        var trip = await _db.Trips.FindAsync(tripId);
        if (trip is null)
            throw new HubException("Turen findes ikke.");

        var userId = Context.User!.GetUserId();
        if (!await _db.IsParticipant(trip, userId))
            throw new HubException("Du har ikke adgang til denne turs chat.");

        var sender = await _db.Users.FindAsync(userId);

        var message = new Message
        {
            Id = Guid.NewGuid(),
            TripId = tripId,
            SenderId = userId,
            Content = content,
            SentAt = DateTime.UtcNow
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync();

        await Clients.Group(GroupName(tripId)).SendAsync("ReceiveMessage", new
        {
            message.Id,
            message.TripId,
            SenderId = userId,
            SenderFullName = sender!.FullName,
            message.Content,
            message.SentAt
        });
    }

    private static string GroupName(Guid tripId) => $"trip-{tripId}";
}
