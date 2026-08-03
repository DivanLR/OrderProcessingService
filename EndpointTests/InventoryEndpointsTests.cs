using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Order.Processing.Api.Tests;

public class InventoryEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public InventoryEndpointsTests(ApiTestFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAvailability_Should_ReturnSeededQuantities()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/inventory/PRD-002");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AvailabilityDto? availability = await response.Content.ReadFromJsonAsync<AvailabilityDto>(JsonSerializerOptions.Web);

        Assert.NotNull(availability);
        Assert.Equal("PRD-002", availability.ProductId);
        Assert.Equal(50, availability.AvailableQuantity);
        Assert.Equal(0, availability.ReservedQuantity);
    }

    [Fact]
    public async Task GetAvailability_Should_ReturnNotFound_WhenProductDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/inventory/PRD-UNKNOWN");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Reserve_Should_MoveQuantity_AndInvalidateTheCachedRead()
    {
        // Arrange: the first read populates the cache, so a stale entry would fail the assert below.
        AvailabilityDto before = await GetAvailabilityAsync("PRD-001");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/inventory/reserve",
            new { productId = "PRD-001", quantity = 4 });

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        AvailabilityDto after = await GetAvailabilityAsync("PRD-001");

        Assert.Equal(before.AvailableQuantity - 4, after.AvailableQuantity);
        Assert.Equal(before.ReservedQuantity + 4, after.ReservedQuantity);
    }

    [Fact]
    public async Task Release_Should_ReturnQuantityToAvailable()
    {
        // Arrange
        AvailabilityDto before = await GetAvailabilityAsync("PRD-003");

        HttpResponseMessage reserveResponse = await _client.PostAsJsonAsync(
            "/api/inventory/reserve",
            new { productId = "PRD-003", quantity = 5 });

        reserveResponse.EnsureSuccessStatusCode();

        // Act
        HttpResponseMessage releaseResponse = await _client.PostAsJsonAsync(
            "/api/inventory/release",
            new { productId = "PRD-003", quantity = 5 });

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, releaseResponse.StatusCode);

        AvailabilityDto after = await GetAvailabilityAsync("PRD-003");

        Assert.Equal(before.AvailableQuantity, after.AvailableQuantity);
        Assert.Equal(before.ReservedQuantity, after.ReservedQuantity);
    }

    [Fact]
    public async Task Reserve_Should_ReturnNotFound_WhenProductDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/inventory/reserve",
            new { productId = "PRD-UNKNOWN", quantity = 1 });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<AvailabilityDto> GetAvailabilityAsync(string productId)
    {
        AvailabilityDto? availability = await _client.GetFromJsonAsync<AvailabilityDto>(
            $"/api/inventory/{productId}",
            JsonSerializerOptions.Web);

        Assert.NotNull(availability);

        return availability;
    }

    private sealed record AvailabilityDto(string ProductId, int AvailableQuantity, int ReservedQuantity);
}
