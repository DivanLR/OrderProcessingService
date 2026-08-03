using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Order.Processing.Api.Tests;

public class OrdersEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public OrdersEndpointsTests(ApiTestFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_Should_ReturnOrder_WithServerComputedTotal()
    {
        var request = new
        {
            customerId = "CUST-001",
            items = new[]
            {
                new { productId = "PRD-001", quantity = 2, unitPrice = 49.99m },
                new { productId = "PRD-002", quantity = 1, unitPrice = 10.02m }
            }
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        CreatedOrderDto? created = await response.Content.ReadFromJsonAsync<CreatedOrderDto>(JsonSerializerOptions.Web);

        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.OrderId);
        Assert.Equal(110m, created.TotalAmount);
        Assert.Equal("pending", created.Status);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenNoItemsAreSupplied()
    {
        var request = new { customerId = "CUST-001", items = Array.Empty<object>() };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_Should_ReturnBadRequest_WhenQuantityIsNotPositive()
    {
        var request = new
        {
            customerId = "CUST-001",
            items = new[] { new { productId = "PRD-001", quantity = 0, unitPrice = 5m } }
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/orders", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_Should_ReturnOrderWithItems()
    {
        Guid orderId = await CreateOrderAsync("CUST-002", "PRD-003", 3, 7.50m);

        HttpResponseMessage response = await _client.GetAsync($"/api/orders/{orderId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        OrderDto? order = await response.Content.ReadFromJsonAsync<OrderDto>(JsonSerializerOptions.Web);

        Assert.NotNull(order);
        Assert.Equal("CUST-002", order.CustomerId);
        Assert.Equal(22.50m, order.TotalAmount);
        Assert.Equal("pending", order.Status);

        OrderItemDto item = Assert.Single(order.Items);

        Assert.Equal("PRD-003", item.ProductId);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(7.50m, item.UnitPrice);
    }

    [Fact]
    public async Task Get_Should_ReturnNotFound_WhenOrderDoesNotExist()
    {
        HttpResponseMessage response = await _client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Should_ClampPageSize_ToFifty()
    {
        await CreateOrderAsync("CUST-003", "PRD-001", 1, 1m);

        HttpResponseMessage response = await _client.GetAsync("/api/orders?page=1&pageSize=500");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        OrdersPageDto? page = await response.Content.ReadFromJsonAsync<OrdersPageDto>(JsonSerializerOptions.Web);

        Assert.NotNull(page);
        Assert.Equal(1, page.Page);
        Assert.Equal(50, page.PageSize);
        Assert.True(page.TotalCount >= 1);
        Assert.True(page.Orders.Length <= 50);
    }

    [Fact]
    public async Task UpdateStatus_Should_ReturnNoContent_AndBeVisibleOnTheNextGet()
    {
        Guid orderId = await CreateOrderAsync("CUST-004", "PRD-002", 1, 20m);

        HttpResponseMessage updateResponse = await _client.PutAsJsonAsync(
            $"/api/orders/{orderId}/status",
            new { status = "confirmed" });

        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        OrderDto? order = await _client.GetFromJsonAsync<OrderDto>(
            $"/api/orders/{orderId}",
            JsonSerializerOptions.Web);

        Assert.NotNull(order);
        Assert.Equal("confirmed", order.Status);
    }

    [Fact]
    public async Task UpdateStatus_Should_ReturnConflict_WhenTransitionIsNotAllowed()
    {
        Guid orderId = await CreateOrderAsync("CUST-005", "PRD-002", 1, 20m);

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/orders/{orderId}/status",
            new { status = "shipped" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_Should_ReturnBadRequest_WhenStatusIsNotRecognised()
    {
        Guid orderId = await CreateOrderAsync("CUST-006", "PRD-002", 1, 20m);

        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/orders/{orderId}/status",
            new { status = "refunded" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateStatus_Should_ReturnNotFound_WhenOrderDoesNotExist()
    {
        HttpResponseMessage response = await _client.PutAsJsonAsync(
            $"/api/orders/{Guid.NewGuid()}/status",
            new { status = "confirmed" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> CreateOrderAsync(string customerId, string productId, int quantity, decimal unitPrice)
    {
        var request = new
        {
            customerId,
            items = new[] { new { productId, quantity, unitPrice } }
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/orders", request);

        response.EnsureSuccessStatusCode();

        CreatedOrderDto? created = await response.Content.ReadFromJsonAsync<CreatedOrderDto>(JsonSerializerOptions.Web);

        Assert.NotNull(created);

        return created.OrderId;
    }

    private sealed record CreatedOrderDto(Guid OrderId, decimal TotalAmount, string Status, DateTime CreatedAt);

    private sealed record OrderDto(
        Guid OrderId,
        string CustomerId,
        OrderItemDto[] Items,
        decimal TotalAmount,
        string Status,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    private sealed record OrderItemDto(string ProductId, int Quantity, decimal UnitPrice);

    private sealed record OrdersPageDto(OrderDto[] Orders, int Page, int PageSize, int TotalCount);
}
