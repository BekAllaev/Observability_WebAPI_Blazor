using System.Net.Http.Json;

namespace Observability_WebAPI_Blazor.Client.Services;

public sealed class BackendChatApiClient
{
    private readonly HttpClient _httpClient;

    public BackendChatApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<HttpResponseMessage> SendAsync(string? user, string? message, CancellationToken cancellationToken = default)
    {
        return _httpClient.PostAsJsonAsync(
            "api/chat/send",
            new { User = user, Message = message },
            cancellationToken);
    }
}
