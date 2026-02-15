using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Observability_WebApi_Blazor.Backend.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries =
        [
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        ];

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly IHostEnvironment _hostEnvironment;

        public WeatherForecastController(ILogger<WeatherForecastController> logger, IHostEnvironment hostEnvironment)
        {
            _logger = logger;
            _hostEnvironment = hostEnvironment;
        }

        [HttpGet(Name = "GetWeatherForecast")]
        public IEnumerable<WeatherForecast> Get()
        {
            var traceId = _hostEnvironment.IsProduction() ? $"TraceId: {GetTraceId()}" : string.Empty;
            _logger.LogInformation($"Getting weather forecast. {traceId}");

            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        }

        private string GetTraceId()
        {
            if (Activity.Current is null)
            {
                return "Activity.Current is null.";
            }
            else
            {
                return Activity.Current.TraceId.ToString();
            }
        }
    }
}
