using System.Diagnostics;

namespace Observability_WebAPI_Blazor.Client;

public class TraceContextHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            // ???????? W3C Trace Context headers
            request.Headers.Add("traceparent", activity.Id);
            
            if (!string.IsNullOrEmpty(activity.TraceStateString))
            {
                request.Headers.Add("tracestate", activity.TraceStateString);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
