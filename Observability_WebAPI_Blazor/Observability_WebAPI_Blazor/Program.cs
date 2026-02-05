using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;
using Observability_WebAPI_Blazor.Client;
using Observability_WebAPI_Blazor.Components;
using Observability_WebAPI_Blazor.Hubs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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
});

builder.Services.AddScoped(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return factory.CreateClient(Options.DefaultName);
});

builder.Services.AddSignalR();

builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

// Serilog bootstrap logger (for startup errors)
Log.Logger = new LoggerConfiguration()
    .CreateLogger();

builder.Host.UseSerilog((ctx, cfg) =>
{
    // If we uncomment these lines we get a lot of default noisy logs from ASP .NET Core
    // Like: Sending file. Request path: 'lib/bootstrap/dist/css/bootstrap.min
    // Or : Executing endpoint 'RazorComponentsEndpoint - /_blazor/components/{*pathInfo}'
    cfg.MinimumLevel.Information()
       .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
       .Enrich.FromLogContext();

    // Use minimal format for Development, JSON for Production
    if (ctx.HostingEnvironment.IsDevelopment())
    {
        // Here we configure EVERY message to console
        cfg.WriteTo.Console(
            outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] [{TraceId}] {Message:lj}{NewLine}{Exception}");
    }
    else
    {
        cfg.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
    }

    cfg.WriteTo.Seq(ctx.Configuration["Seq:Url"] ?? "http://localhost:5341");
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r=>r.AddService(serviceName: "Observability.Web"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                var endpoint =
                    builder.Configuration["OpenTelemetry:Tracing:Exporter:Otlp:Endpoint"]
                    ?? "http://localhost:4317";

                options.Endpoint = new Uri(endpoint);
            });
    });

var app = builder.Build();

app.UseResponseCompression();

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

app.MapHub<BlazorChatHub>("/blazorChatHub");

app.Run();
