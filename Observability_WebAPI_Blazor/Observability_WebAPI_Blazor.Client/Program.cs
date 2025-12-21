using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => 
{ 
    var url = builder.Configuration["BackendUrl"];

    if (string.IsNullOrEmpty(url))
    {
        throw new InvalidOperationException("BackendUrl configuration is missing.");
    }
    
    var httpClient = new HttpClient();
    httpClient.BaseAddress = new Uri(url);
    return httpClient;
});

await builder.Build().RunAsync();
