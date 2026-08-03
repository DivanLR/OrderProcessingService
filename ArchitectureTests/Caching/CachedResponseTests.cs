using System.Text.Json;
using Order.Processing.Application.Features.InventoryItems.GetInventoryAvailability;
using Order.Processing.Application.Features.Orders.GetAllOrders;
using Order.Processing.Application.Features.Orders.GetOrder;
using Order.Processing.Application.Features.Payments.GetPaymentStatus;
using Order.Processing.Domain.Entities;

namespace ArchitectureTests.Caching;

public class CachedResponseTests
{
    private static readonly DateTime Timestamp = new(2026, 8, 4, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void OrderResponse_ShouldRoundTrip_ThroughTheCacheSerializer()
    {
        var response = new OrderResponse(
            Guid.NewGuid(),
            "CUST-001",
            [new OrderItemResponse("PRD-001", 2, 49.99m)],
            99.98m,
            Status.Pending,
            Timestamp,
            Timestamp);

        OrderResponse? roundTrip = RoundTrip(response);

        Assert.NotNull(roundTrip);
        Assert.Equal(response.OrderId, roundTrip.OrderId);
        Assert.Equal(response.CustomerId, roundTrip.CustomerId);
        Assert.Equal(response.Items, roundTrip.Items);
        Assert.Equal(response.TotalAmount, roundTrip.TotalAmount);
        Assert.Equal(response.Status, roundTrip.Status);
        Assert.Equal(response.UpdatedAt, roundTrip.UpdatedAt);
    }

    [Fact]
    public void OrdersPageResponse_ShouldRoundTrip_ThroughTheCacheSerializer()
    {
        OrderResponse[] orders =
        [
            new OrderResponse(
                Guid.NewGuid(),
                "CUST-002",
                [new OrderItemResponse("PRD-002", 1, 10m)],
                10m,
                Status.Confirmed,
                Timestamp,
                Timestamp)
        ];

        var response = new OrdersPageResponse(orders, 1, 20, 1);

        OrdersPageResponse? roundTrip = RoundTrip(response);

        Assert.NotNull(roundTrip);
        Assert.Equal(response.Page, roundTrip.Page);
        Assert.Equal(response.PageSize, roundTrip.PageSize);
        Assert.Equal(response.TotalCount, roundTrip.TotalCount);

        OrderResponse roundTripOrder = Assert.Single(roundTrip.Orders);

        Assert.Equal(orders[0].OrderId, roundTripOrder.OrderId);
        Assert.Equal(orders[0].CustomerId, roundTripOrder.CustomerId);
        Assert.Equal(orders[0].Items, roundTripOrder.Items);
        Assert.Equal(orders[0].TotalAmount, roundTripOrder.TotalAmount);
        Assert.Equal(orders[0].Status, roundTripOrder.Status);
    }

    [Fact]
    public void InventoryItemAvailabilityResponse_ShouldRoundTrip_ThroughTheCacheSerializer()
    {
        var response = new InventoryItemAvailabilityResponse("PRD-001", 98, 2);

        Assert.Equal(response, RoundTrip(response));
    }

    [Fact]
    public void PaymentStatusResponse_ShouldRoundTrip_ThroughTheCacheSerializer()
    {
        var response = new PaymentStatusResponse(
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            99.98m,
            PaymentStatus.Completed,
            Timestamp);

        Assert.Equal(response, RoundTrip(response));
    }

    private static T? RoundTrip<T>(T value) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value));
}
