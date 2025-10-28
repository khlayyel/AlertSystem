using Microsoft.AspNetCore.SignalR;

namespace AlertSystem.Infrastructure.Hubs;

public class NotificationHub : Hub
{
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveGroup(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task JoinUserGroup(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public async Task LeaveUserGroup(string userId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
    }

    public async Task SendToUser(string userId, string message, object data)
    {
        await Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", message, data);
    }

    public async Task SendToAll(string message, object data)
    {
        await Clients.All.SendAsync("ReceiveNotification", message, data);
    }
}


