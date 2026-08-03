using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Order.Processing.Api.Tests;

public class PaymentsEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public PaymentsEndpointsTests(ApiTestFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Process_Should_ReturnCompletedTransaction()
    {
        // Arrange
        Guid orderId = await CreateOrderAsync(25m);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/payments/process",
            new { orderId = orderId.ToString(), amount = 25m });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        ProcessedPaymentDto? payment = await response.Content.ReadFromJsonAsync<ProcessedPaymentDto>(JsonSerializerOptions.Web);

        Assert.NotNull(payment);
        Assert.NotEqual(Guid.Empty, payment.TransactionId);
        Assert.Equal("completed", payment.Status);
    }

    [Fact]
    public async Task Process_Should_ReturnNotFound_WhenOrderDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/payments/process",
            new { orderId = Guid.NewGuid().ToString(), amount = 10m });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Process_Should_ReturnBadRequest_WhenAmountIsNotPositive()
    {
        // Arrange
        Guid orderId = await CreateOrderAsync(15m);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/payments/process",
            new { orderId = orderId.ToString(), amount = 0m });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Process_Should_ReturnBadRequest_WhenOrderIdIsNotAGuid()
    {
        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/api/payments/process",
            new { orderId = "not-a-guid", amount = 10m });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetStatus_Should_ReturnTheProcessedTransaction()
    {
        // Arrange
        Guid orderId = await CreateOrderAsync(30m);

        ProcessedPaymentDto? payment = await (await _client.PostAsJsonAsync(
            "/api/payments/process",
            new { orderId = orderId.ToString(), amount = 30m }))
            .Content.ReadFromJsonAsync<ProcessedPaymentDto>(JsonSerializerOptions.Web);

        Assert.NotNull(payment);

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/payments/{payment.TransactionId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        PaymentStatusDto? status = await response.Content.ReadFromJsonAsync<PaymentStatusDto>(JsonSerializerOptions.Web);

        Assert.NotNull(status);
        Assert.Equal(payment.TransactionId, status.TransactionId);
        Assert.Equal(orderId.ToString(), status.OrderId);
        Assert.Equal(30m, status.Amount);
        Assert.Equal("completed", status.Status);
    }

    [Fact]
    public async Task GetStatus_Should_ReturnNotFound_WhenTransactionDoesNotExist()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/payments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<Guid> CreateOrderAsync(decimal unitPrice)
    {
        var request = new
        {
            customerId = "CUST-PAY",
            items = new[] { new { productId = "PRD-001", quantity = 1, unitPrice } }
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/orders", request);

        response.EnsureSuccessStatusCode();

        CreatedOrderDto? created = await response.Content.ReadFromJsonAsync<CreatedOrderDto>(JsonSerializerOptions.Web);

        Assert.NotNull(created);

        return created.OrderId;
    }

    private sealed record CreatedOrderDto(Guid OrderId, decimal TotalAmount, string Status, DateTime CreatedAt);

    private sealed record ProcessedPaymentDto(Guid TransactionId, string Status, DateTime ProcessedAt);

    private sealed record PaymentStatusDto(
        Guid TransactionId,
        string OrderId,
        decimal Amount,
        string Status,
        DateTime ProcessedAt);
}
