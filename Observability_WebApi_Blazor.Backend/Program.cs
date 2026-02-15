using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Observability_WebApi_Blazor.Backend.Hubs;
using OpenTelemetry.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Serilog bootstrap logger
Log.Logger = new LoggerConfiguration()
    .CreateLogger();

builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.MinimumLevel.Information()
       .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
       .Enrich.FromLogContext();

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

builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: "Observability.Backend"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(
                    builder.Configuration["Jaeger:OtlpEndpoint"]
                    ?? "http://localhost:4317");
            });
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()   // http.server.request.duration, active_requests
            .AddHttpClientInstrumentation()    // http.client.request.duration
            .AddRuntimeInstrumentation()       // GC, threadpool, assemblies
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(
                    builder.Configuration["Jaeger:OtlpEndpoint"]
                    ?? "http://localhost:4317");
            });
    });

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSignalR();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            // SignalR requires credentials when using AllowAnyOrigin is not compatible with AllowCredentials.
            // For demo purposes we allow any origin via predicate.
            .SetIsOriginAllowed(_ => true)
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors();
app.UseAuthorization();

app.MapControllers();

app.MapHub<ChatHub>("/hubs/chat");

app.Run();
