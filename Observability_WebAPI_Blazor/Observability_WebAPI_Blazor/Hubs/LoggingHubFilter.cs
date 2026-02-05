using Microsoft.AspNetCore.SignalR;

namespace Observability_WebAPI_Blazor.Hubs;

public sealed class LoggingHubFilter : IHubFilter
{
    private readonly ILogger<LoggingHubFilter> _logger;

    public LoggingHubFilter(ILogger<LoggingHubFilter> logger)
    {
        _logger = logger;
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object?>
        {
            ["Hub"] = invocationContext.Hub.GetType().Name,
            ["Method"] = invocationContext.HubMethodName,
            ["ConnectionId"] = invocationContext.Context.ConnectionId,
            ["UserIdentifier"] = invocationContext.Context.UserIdentifier
        });

        _logger.LogInformation("SignalR hub method invoked.");

        try
        {
            var result = await next(invocationContext);
            _logger.LogInformation("SignalR hub method completed.");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR hub method failed.");
            throw;
        }
    }

    public async Task OnConnectedAsync(
        HubLifetimeContext context,
        Func<HubLifetimeContext, Task> next)
    {
        _logger.LogInformation(
            "SignalR connection opened. ConnectionId: {ConnectionId}, UserIdentifier: {UserIdentifier}",
            context.Context.ConnectionId,
            context.Context.UserIdentifier);

        await next(context);
    }

    public async Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        if (exception is null)
        {
            _logger.LogInformation(
                "SignalR connection closed. ConnectionId: {ConnectionId}, UserIdentifier: {UserIdentifier}",
                context.Context.ConnectionId,
                context.Context.UserIdentifier);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "SignalR connection closed with error. ConnectionId: {ConnectionId}, UserIdentifier: {UserIdentifier}",
                context.Context.ConnectionId,
                context.Context.UserIdentifier);
        }

        await next(context, exception);
    }
}
