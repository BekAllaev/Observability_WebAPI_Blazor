using Microsoft.AspNetCore.SignalR;

namespace Observability_WebAPI_Blazor.Hubs;

public sealed class ServerChatHub : Hub
{
    public Task SendMessage(string user, string message)
    {
        return Clients.All.SendAsync("ReceiveMessage", user, message);
    }
}
