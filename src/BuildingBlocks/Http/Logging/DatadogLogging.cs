using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Datadog.Logs;

namespace BuildingBlocks.Http.Logging;

public static class DatadogLogging
{
    public static void Configure(HostBuilderContext context, IServiceProvider _, LoggerConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configuration);

        configuration
            .ReadFrom.Configuration(context.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console(new RenderedCompactJsonFormatter());

        TryAddDatadogSink(context.Configuration, configuration);
    }

    public static void TryAddDatadogSink(IConfiguration configuration, LoggerConfiguration loggerConfiguration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(loggerConfiguration);

        var apiKey = configuration["DD_API_KEY"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            Log.Information("DD_API_KEY ausente; sink Datadog de logs não será registrado");
            return;
        }

        var service = configuration["DD_SERVICE"];
        if (string.IsNullOrWhiteSpace(service))
        {
            Log.Warning("DD_SERVICE ausente; sink Datadog de logs não será registrado");
            return;
        }

        var env = configuration["DD_ENV"];
        if (string.IsNullOrWhiteSpace(env))
        {
            Log.Warning("DD_ENV ausente; sink Datadog de logs não será registrado");
            return;
        }

        var intakeUrl = ResolveLogsIntakeUrl(configuration["DD_SITE"]);
        if (intakeUrl is null)
        {
            Log.Warning("DD_SITE ausente; sink Datadog de logs não será registrado");
            return;
        }

        loggerConfiguration.WriteTo.DatadogLogs(
            apiKey,
            source: "csharp",
            service: service,
            host: Environment.MachineName,
            tags: [$"env:{env}"],
            configuration: new DatadogConfiguration { Url = intakeUrl });
    }

    public static string? ResolveLogsIntakeUrl(string? ddSite)
    {
        if (string.IsNullOrWhiteSpace(ddSite))
        {
            return null;
        }

        return $"https://http-intake.logs.{ddSite.Trim()}";
    }
}
