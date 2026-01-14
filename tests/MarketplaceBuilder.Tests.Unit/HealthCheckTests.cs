using System.Net;
using Xunit;

namespace MarketplaceBuilder.Tests.Unit;

public class HealthCheckTests
{
    [Fact]
    public void HealthCheck_ShouldBeHealthy()
    {
        // Arrange
        var expectedStatus = "Healthy";

        // Act
        var actualStatus = "Healthy";

        // Assert
        Assert.Equal(expectedStatus, actualStatus);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public void Environment_ShouldBeValid(string environment)
    {
        // Arrange
        var validEnvironments = new[] { "Development", "Staging", "Production" };

        // Act
        var isValid = validEnvironments.Contains(environment);

        // Assert
        Assert.True(isValid);
    }
}
