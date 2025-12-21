using Microsoft.Extensions.Options;
using Observability_WebAPI_Blazor.Client.Pages;
using Observability_WebAPI_Blazor.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddHttpClient(Options.DefaultName, (sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var backendUrl = configuration["BackendUrl"]
        ?? throw new InvalidOperationException("BackendUrl configuration is missing.");

    client.BaseAddress = new Uri(backendUrl);
});

builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient(Options.DefaultName);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Observability_WebAPI_Blazor.Client._Imports).Assembly);

app.Run();
