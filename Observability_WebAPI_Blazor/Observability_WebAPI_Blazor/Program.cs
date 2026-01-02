using Microsoft.Extensions.Options;
using Observability_WebAPI_Blazor.Components;
using Serilog;
using Serilog.Events;

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
})
.ConfigurePrimaryHttpMessageHandler(() =>
{
    var handler = new HttpClientHandler();
    return handler;
})
.ConfigureHttpClient((sp, client) =>
{
    client.DefaultRequestHeaders.Add("User-Agent", "Blazor-Server");
});

builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient(Options.DefaultName);
});

// Serilog bootstrap logger (for startup errors)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Warning()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .CreateLogger();

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.MinimumLevel.Information()
       .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
       .Enrich.FromLogContext()
       .Enrich.WithProperty("Application", "Blazor-Server");

    // Use minimal format for Development, JSON for Production
    if (ctx.HostingEnvironment.IsDevelopment())
    {
        cfg.WriteTo.Console(
            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] [{TraceId}] {Message:lj}{NewLine}{Exception}");
    }
    else
    {
        cfg.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
    }

    cfg.WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341");
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

app.UseSerilogRequestLogging(options =>
{
    // ????????? ??????? ? ??????????? ?????? WebAssembly
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/_framework") ||
            httpContext.Request.Path.StartsWithSegments("/_content"))
        {
            return LogEventLevel.Verbose; // ?? ???????? ? ???????
        }

        if (ex != null || httpContext.Response.StatusCode >= 500)
        {
            return LogEventLevel.Error;
        }

        if (httpContext.Response.StatusCode >= 400)
        {
            return LogEventLevel.Warning;
        }

        return LogEventLevel.Information;
    };

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
        diagnosticContext.Set("RequestPath", httpContext.Request.Path);
    };
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Observability_WebAPI_Blazor.Client._Imports).Assembly);

app.Run();
