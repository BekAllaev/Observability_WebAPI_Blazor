using Microsoft.Extensions.Options;
using Observability_WebAPI_Blazor.Client.Pages;
using Observability_WebAPI_Blazor.Components;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

// --- Serilog ---
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341") // Replace with prod Seq URL
    .CreateLogger();

builder.Host.UseSerilog();

// Bind OpenTelemetry config section
var otelConfig = builder.Configuration.GetSection("OpenTelemetry");
var serviceName = otelConfig.GetValue<string>("Service:Name") ?? "MyService";
var environmentName = otelConfig.GetValue<string>("Service:Environment") ?? builder.Environment.EnvironmentName;
var tracingConfig = otelConfig.GetSection("Tracing");
var sampler = tracingConfig.GetValue<string>("Sampler") ?? "ParentBasedTraceIdRatio";
var traceIdRatio = tracingConfig.GetValue<double>("TraceIdRatio");
var recordException = tracingConfig.GetValue<bool>("RecordException");
var filterOutPaths = tracingConfig.GetSection("Instrumentation:AspNetCore:FilterOutPaths").Get<string[]>() ?? Array.Empty<string>();
var otlpEndpoint = tracingConfig.GetValue<string>("Exporter:Otlp:Endpoint") ?? "http://localhost:4317";
var otlpEnabled = tracingConfig.GetValue<bool>("Exporter:Otlp:Enabled");
var consoleExporterEnabled = tracingConfig.GetValue<bool>("Exporter:Console:Enabled");

// --- OpenTelemetry ---
// Add OpenTelemetry tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProvider =>
{
    tracerProvider
        .SetResourceBuilder(ResourceBuilder.CreateDefault()
            .AddService(serviceName)
            .AddAttributes(new[] { new KeyValuePair<string, object>("deployment.environment", environmentName) }));

    if (tracingConfig.GetValue<bool>("Instrumentation:AspNetCore:Enabled"))
    {
        tracerProvider.AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = recordException;
            options.Filter = httpContext =>
            {
                var path = httpContext.Request.Path.Value ?? string.Empty;
                return !filterOutPaths.Any(f => path.StartsWith(f, StringComparison.OrdinalIgnoreCase));
            };
        });
    }

    if (tracingConfig.GetValue<bool>("Instrumentation:HttpClient:Enabled"))
    {
        tracerProvider.AddHttpClientInstrumentation(options =>
        {
            options.RecordException = tracingConfig.GetValue<bool>("Instrumentation:HttpClient:RecordException");
        });
    }

    if (otlpEnabled)
    {
        tracerProvider.AddOtlpExporter(otlpOptions =>
        {
            otlpOptions.Endpoint = new Uri(otlpEndpoint);
            // Protocol can be set if needed, default is gRPC
        });
    }

    if (consoleExporterEnabled)
    {
        tracerProvider.AddConsoleExporter();
    }
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
