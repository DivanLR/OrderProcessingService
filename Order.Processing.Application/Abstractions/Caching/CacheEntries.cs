using Microsoft.Extensions.Caching.Hybrid;

namespace Order.Processing.Application.Abstractions.Caching;

internal static class CacheEntries
{
    internal const string OrdersTag = "orders";
    internal const string InventoryTag = "inventory";
    internal const string PaymentsTag = "payments";

    internal static readonly HybridCacheEntryOptions Options = new()
    {
        Expiration = TimeSpan.FromMinutes(2),
        LocalCacheExpiration = TimeSpan.FromMinutes(2)
    };
}
