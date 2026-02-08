using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Observability_WebApi_Blazor.Backend.Hubs;
using Observability_WebApi_Blazor.Backend.Models;

namespace Observability_WebApi_Blazor.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ChatController : ControllerBase
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<ChatController> _logger;

    public ChatController(IHubContext<ChatHub> hubContext, ILogger<ChatController> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] ChatMessageRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Chat message received via HTTP. User: {User}, MessageLength: {MessageLength}",
            request.User,
            request.Message?.Length ?? 0);

        await _hubContext.Clients.All.SendAsync(
            "ReceiveMessage",
            request.User ?? string.Empty,
            request.Message ?? string.Empty,
            cancellationToken);

        return Ok();
    }
}
