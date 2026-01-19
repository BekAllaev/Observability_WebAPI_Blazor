using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Observability_WebAPI_Blazor.Client;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddHttpClient("Backend", (sp, client) =>
{
    var url = builder.Configuration["BackendUrl"];

    if (string.IsNullOrEmpty(url))
    {
        throw new InvalidOperationException("BackendUrl configuration is missing.");
    }

    client.BaseAddress = new Uri(url);
});

builder.Services.AddScoped(sp => 
{ 
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient("Backend");
});

await builder.Build().RunAsync();
