using BuildingBlocks.Http.ProblemDetails;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace UnitTests.Http.ProblemDetails;

public class ProblemDetailsServiceCollectionExtensionsTests
{
    [Fact]
    public void AddBuildingBlocksProblemDetails_RegistersFilterAndMappings()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBuildingBlocksProblemDetails(options =>
            options.Map<InvalidDataException>(404, "Not Found"));
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.NotNull(provider.GetRequiredService<ProblemDetailsExceptionFilter>());
        Assert.True(
            provider.GetRequiredService<IOptions<ExceptionProblemDetailsOptions>>()
                .Value.Mappings.ContainsKey(typeof(InvalidDataException)));
    }

    [Fact]
    public void AddBuildingBlocksProblemDetails_WithoutConfigure_RegistersEmptyOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBuildingBlocksProblemDetails();
        using var provider = services.BuildServiceProvider();

        // Assert
        Assert.Empty(
            provider.GetRequiredService<IOptions<ExceptionProblemDetailsOptions>>().Value.Mappings);
    }
}
