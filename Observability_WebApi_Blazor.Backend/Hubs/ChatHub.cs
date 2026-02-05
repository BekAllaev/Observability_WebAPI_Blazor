using Microsoft.AspNetCore.SignalR;

namespace Observability_WebApi_Blazor.Backend.Hubs;

public sealed class ChatHub : Hub
{
    public Task SendMessage(string user, string message)
    {
        return Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}
