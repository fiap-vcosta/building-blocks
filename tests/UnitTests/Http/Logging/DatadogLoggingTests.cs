using BuildingBlocks.Http.Logging;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace UnitTests.Http.Logging;

public class DatadogLoggingTests
{
    [Theory]
    [InlineData("datadoghq.com", "https://http-intake.logs.datadoghq.com")]
    [InlineData("us5.datadoghq.com", "https://http-intake.logs.us5.datadoghq.com")]
    [InlineData("datadoghq.eu", "https://http-intake.logs.datadoghq.eu")]
    [InlineData("  us5.datadoghq.com  ", "https://http-intake.logs.us5.datadoghq.com")]
    public void ResolveLogsIntakeUrl_DerivesIntakeFromDdSite(string ddSite, string expected)
    {
        // Arrange / Act
        var intakeUrl = DatadogLogging.ResolveLogsIntakeUrl(ddSite);

        // Assert
        Assert.Equal(expected, intakeUrl);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveLogsIntakeUrl_ReturnsNull_WhenDdSiteMissing(string? ddSite)
    {
        // Arrange / Act
        var intakeUrl = DatadogLogging.ResolveLogsIntakeUrl(ddSite);

        // Assert
        Assert.Null(intakeUrl);
    }

    [Fact]
    public void TryAddDatadogSink_WithoutApiKey_DoesNotThrow()
    {
        // Arrange
        var configuration = new ConfigurationBuilder().Build();
        var loggerConfiguration = new LoggerConfiguration();

        // Act
        DatadogLogging.TryAddDatadogSink(configuration, loggerConfiguration);

        // Assert
        Assert.NotNull(loggerConfiguration.CreateLogger());
    }

    [Fact]
    public void TryAddDatadogSink_WithCompleteSettings_RegistersSink()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DD_API_KEY"] = "demo-key",
                ["DD_SERVICE"] = "building-blocks",
                ["DD_ENV"] = "test",
                ["DD_SITE"] = "datadoghq.com"
            })
            .Build();
        var loggerConfiguration = new LoggerConfiguration();

        // Act
        DatadogLogging.TryAddDatadogSink(configuration, loggerConfiguration);

        // Assert
        using var logger = loggerConfiguration.CreateLogger();
        Assert.NotNull(logger);
    }

    [Theory]
    [InlineData("DD_SERVICE")]
    [InlineData("DD_ENV")]
    [InlineData("DD_SITE")]
    public void TryAddDatadogSink_WhenRequiredSettingMissing_DoesNotThrow(string missingKey)
    {
        // Arrange
        var values = new Dictionary<string, string?>
        {
            ["DD_API_KEY"] = "demo-key",
            ["DD_SERVICE"] = "building-blocks",
            ["DD_ENV"] = "test",
            ["DD_SITE"] = "datadoghq.com"
        };
        values[missingKey] = " ";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var loggerConfiguration = new LoggerConfiguration();

        // Act
        DatadogLogging.TryAddDatadogSink(configuration, loggerConfiguration);

        // Assert
        using var logger = loggerConfiguration.CreateLogger();
        Assert.NotNull(logger);
    }
}
