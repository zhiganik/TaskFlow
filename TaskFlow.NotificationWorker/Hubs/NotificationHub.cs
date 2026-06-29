using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TaskFlow.NotificationWorker.Hubs;

[Authorize]
public class NotificationHub(ILogger<NotificationHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        logger.LogInformation(
            "Connected user={UserId} conn={ConnectionId} server={Server}",
            Context.UserIdentifier, Context.ConnectionId, Environment.MachineName);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        logger.LogInformation(
            "Disconnected user={UserId} conn={ConnectionId} server={Server}",
            Context.UserIdentifier, Context.ConnectionId, Environment.MachineName);
        await base.OnDisconnectedAsync(exception);
    }
}
