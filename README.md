# Order Processing API

An ASP.NET Core 10 order processing service built on Clean Architecture, with a hand-rolled
CQRS dispatch layer, the Result pattern for error handling, and HybridCache on the read paths.

- Target framework: `net10.0`
- API style: MVC controllers returning `Result` and `Result<T>`
- Persistence: Entity Framework Core with Sqlite
- Logging: Serilog with a correlation identifier per request
- Documentation: built-in OpenAPI plus the Scalar reference UI

## Contents

1. [Solution layout](#solution-layout)
2. [Getting started](#getting-started)
3. [Configuration](#configuration)
4. [Seed data](#seed-data)
5. [API reference with examples](#api-reference-with-examples)
6. [Error handling](#error-handling)
7. [Caching](#caching)
8. [Adding a new slice](#adding-a-new-slice)
9. [Testing](#testing)
10. [Known limitations](#known-limitations)

## Solution layout

```
OrderProcessing.slnx
├── Order.Processing.Domain           Entities, Result, Error, no outward dependencies
├── Order.Processing.Application      CQRS abstractions, decorators, feature slices
├── Order.Processing.Infrastructure   DbContext, EF configurations, seeding, DI
├── OrderProcessing                   API project (assembly Order.Processing.Api)
├── ArchitectureTests                 Layer rules plus cache serialisation tests
└── EndpointTests                     HTTP tests over WebApplicationFactory
```

Dependency direction, enforced by `ArchitectureTests/Layers/LayerTests.cs`:

```
Api  ->  Infrastructure  ->  Application  ->  Domain
```

The Domain project depends on nothing else in the solution. The Application project never
references Infrastructure or the API; it declares `IApplicationDbContext` and Infrastructure
implements it.

### How a request flows

```
HTTP request
  -> Controller action (injects a handler interface, not a mediator)
    -> LoggingDecorator          logs the command or query name and its outcome
      -> ValidationDecorator     runs FluentValidation validators, if any are registered
        -> Handler               returns Result or Result<T>
  -> ResultActionFilter          success -> 200 or 204, failure -> ProblemDetails
```

`ResultActionFilter` is registered globally in `Program.cs`, therefore controller actions
return the `Result` type directly and never construct an `IActionResult`.

## Getting started

### Prerequisites

- .NET SDK 10.0.302 or later
- No database server is required. Sqlite creates a local file on first run.

### Run

```bash
git clone <repository-url>
cd OrderProcessing
dotnet restore
dotnet run --project OrderProcessing --launch-profile http
```

The application listens on `http://localhost:5081`. The `https` profile listens on
`https://localhost:7148` in addition.

In the Development environment the host creates the schema and seeds inventory on startup,
by way of `InitialiseDatabaseAsync` in `Order.Processing.Infrastructure/DependencyInjection.cs`.

### Documentation endpoints

| Route | Purpose |
|---|---|
| `http://localhost:5081/scalar/v1` | Scalar reference UI, Development only |
| `http://localhost:5081/openapi/v1.json` | OpenAPI 3.1 document, Development only |

### Common commands

```bash
dotnet build                                  # build every project, warnings are errors
dotnet test                                   # 31 tests, no Docker required
dotnet test --nologo --filter "FullyQualifiedName~OrdersEndpointsTests"
dotnet run --project OrderProcessing --launch-profile http
```

## Configuration

`OrderProcessing/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Database": "Data Source=orderprocessing.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

`AddInfrastructure` throws `InvalidOperationException` at startup when
`ConnectionStrings:Database` is absent, which fails fast rather than at the first request.

To move to another provider, change the one call in
`Order.Processing.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connectionString));
```

No handler, controller, or entity configuration changes are required, because every handler
depends on `IApplicationDbContext` rather than on a concrete context.

## Seed data

`ApplicationDbSeeder` inserts four inventory items on first run, and skips seeding whenever
any inventory row already exists.

| Product | Available | Reserved |
|---|---|---|
| `PRD-001` | 100 | 0 |
| `PRD-002` | 50 | 0 |
| `PRD-003` | 25 | 0 |
| `PRD-004` | 0 | 0 |

## API reference with examples

Every example below is a real request and response captured against a running instance.
Enumerations serialise as camelCase strings, configured once through
`JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` in `Program.cs`.

### Orders

#### POST /api/orders

Creates an order. The total is computed on the server as the sum of `quantity * unitPrice`
across the lines, therefore a client-supplied total is neither accepted nor trusted.

```bash
curl -s -X POST http://localhost:5081/api/orders \
  -H 'Content-Type: application/json' \
  -d '{
        "customerId": "CUST-E2E",
        "items": [
          { "productId": "PRD-001", "quantity": 2, "unitPrice": 49.99 },
          { "productId": "PRD-002", "quantity": 1, "unitPrice": 10.02 }
        ]
      }'
```

`200 OK`

```json
{
  "orderId": "b0c3fccf-bb1f-47f0-b7ea-5898bc8a2d2e",
  "totalAmount": 110.00,
  "status": "pending",
  "createdAt": "2026-08-03T22:40:23.787532Z"
}
```

Rejected requests return `400 Bad Request`: an empty `items` array, a blank `customerId`,
a `quantity` of zero or less, or a negative `unitPrice`.

```bash
curl -s -X POST http://localhost:5081/api/orders \
  -H 'Content-Type: application/json' \
  -d '{ "customerId": "CUST-NEG", "items": [] }'
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation Error",
  "status": 400,
  "detail": "An order requires at least one item.",
  "errorCode": "validation_error"
}
```

#### GET /api/orders/{id}

```bash
curl -s http://localhost:5081/api/orders/b0c3fccf-bb1f-47f0-b7ea-5898bc8a2d2e
```

`200 OK`

```json
{
  "orderId": "b0c3fccf-bb1f-47f0-b7ea-5898bc8a2d2e",
  "customerId": "CUST-E2E",
  "items": [
    { "productId": "PRD-001", "quantity": 2, "unitPrice": 49.99 },
    { "productId": "PRD-002", "quantity": 1, "unitPrice": 10.02 }
  ],
  "totalAmount": 110.0,
  "status": "confirmed",
  "createdAt": "2026-08-03T22:40:23.787532",
  "updatedAt": "2026-08-03T22:40:24.9222687"
}
```

An unknown identifier returns `404 Not Found` with `errorCode: order.not_found`.

#### GET /api/orders

Paginated. `page` defaults to 1 and `pageSize` defaults to 20. `pageSize` is clamped to a
maximum of 50 and a minimum of 1, and `page` is clamped to a minimum of 1, so a hostile
`pageSize=500` cannot force a full table read.

```bash
curl -s "http://localhost:5081/api/orders?page=1&pageSize=500"
```

`200 OK`, note the clamped `pageSize`:

```json
{
  "orders": [
    {
      "orderId": "83ad233c-eee2-44eb-8688-b68d5fa410c8",
      "customerId": "CUST-NEG",
      "items": [ { "productId": "PRD-002", "quantity": 1, "unitPrice": 5.0 } ],
      "totalAmount": 5.0,
      "status": "pending",
      "createdAt": "2026-08-03T22:41:07.4704881",
      "updatedAt": "2026-08-03T22:41:07.4704883"
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalCount": 2
}
```

Orders are ordered by `createdAt` descending, then by `orderId` to keep paging stable when
two orders share a timestamp.

#### PUT /api/orders/{id}/status

```bash
curl -s -X PUT http://localhost:5081/api/orders/b0c3fccf-bb1f-47f0-b7ea-5898bc8a2d2e/status \
  -H 'Content-Type: application/json' \
  -d '{ "status": "confirmed" }'
```

`204 No Content`.

The status value is parsed case-insensitively, so `confirmed` and `Confirmed` are both
accepted. Permitted transitions are enforced by `Order.UpdateStatus`:

| From | Allowed to |
|---|---|
| `pending` | `confirmed`, `cancelled` |
| `confirmed` | `shipped`, `cancelled` |
| `cancelled` | nothing |
| `shipped` | nothing |

Setting a status to its current value is a no-op and returns `204 No Content`.

A forbidden transition returns `409 Conflict`:

```bash
curl -s -X PUT http://localhost:5081/api/orders/{pendingOrderId}/status \
  -H 'Content-Type: application/json' -d '{ "status": "shipped" }'
```

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "An order cannot move from Pending to Shipped.",
  "errorCode": "order.invalid_status_transition"
}
```

An unrecognised value returns `400 Bad Request` and lists the valid values:

```json
{
  "title": "Validation Error",
  "status": 400,
  "detail": "'refunded' is not a valid order status. Valid values: Pending, Confirmed, Cancelled, Shipped.",
  "errorCode": "validation_error"
}
```

### Inventory

#### GET /api/inventory/{productId}

```bash
curl -s http://localhost:5081/api/inventory/PRD-001
```

`200 OK`

```json
{ "productId": "PRD-001", "availableQuantity": 100, "reservedQuantity": 0 }
```

An unknown product returns `404 Not Found` with `errorCode: inventory.not_found`.

#### POST /api/inventory/reserve

The product identifier travels in the body rather than the route.

```bash
curl -s -X POST http://localhost:5081/api/inventory/reserve \
  -H 'Content-Type: application/json' \
  -d '{ "productId": "PRD-001", "quantity": 4 }'
```

`204 No Content`. The following read reflects the move immediately, because the write
invalidates the `inventory` cache tag:

```json
{ "productId": "PRD-001", "availableQuantity": 96, "reservedQuantity": 4 }
```

#### POST /api/inventory/release

```bash
curl -s -X POST http://localhost:5081/api/inventory/release \
  -H 'Content-Type: application/json' \
  -d '{ "productId": "PRD-001", "quantity": 4 }'
```

`204 No Content`, and availability returns to `100 / 0`.

### Payments

#### POST /api/payments/process

The order must exist. `orderId` is a string on the wire and is parsed as a GUID.

```bash
curl -s -X POST http://localhost:5081/api/payments/process \
  -H 'Content-Type: application/json' \
  -d '{ "orderId": "b0c3fccf-bb1f-47f0-b7ea-5898bc8a2d2e", "amount": 110.00 }'
```

`200 OK`

```json
{
  "transactionId": "e51ee4be-ccfe-4a86-81a0-93703c301818",
  "status": "completed",
  "processedAt": "2026-08-03T22:40:39.2664233Z"
}
```

| Condition | Response |
|---|---|
| `amount` of zero or less | `400`, `Amount must be greater than zero.` |
| `orderId` is not a GUID | `400`, `'not-a-guid' is not a valid order id.` |
| Order does not exist | `404`, `errorCode: order.not_found` |

#### GET /api/payments/{transactionId}

```bash
curl -s http://localhost:5081/api/payments/e51ee4be-ccfe-4a86-81a0-93703c301818
```

`200 OK`

```json
{
  "transactionId": "e51ee4be-ccfe-4a86-81a0-93703c301818",
  "orderId": "b0c3fccf-bb1f-47f0-b7ea-5898bc8a2d2e",
  "amount": 110.0,
  "status": "completed",
  "processedAt": "2026-08-03T22:40:39.2664233"
}
```

An unknown transaction returns `404 Not Found` with `errorCode: payment.not_found`.

### Correlation identifier

`CorrelationIdMiddleware` reads `X-Correlation-Id`, generates one when the header is absent,
echoes it on the response, and pushes it into the Serilog log context for the whole request.

```bash
curl -s -D - -o /dev/null -H 'X-Correlation-Id: e2e-check' \
  http://localhost:5081/api/inventory/PRD-002 | grep -i correlation
```

```
X-Correlation-Id: e2e-check
```

## Error handling

Handlers never throw for expected failures. They return `Result.Failure(Error)` and
`ResultExtensions.ToProblemDetails` maps the error type onto a status code:

| `ErrorType` | Status | Title |
|---|---|---|
| `Validation` | 400 | Validation Error |
| `NotFound` | 404 | Not Found |
| `Conflict` | 409 | Conflict |
| `Failure` | 500 | Server Error |

Every failure body is an RFC 9457 problem document carrying an extra `errorCode` member,
for example `order.not_found` or `order.invalid_status_transition`, which gives clients a
stable identifier to branch on rather than parsing prose.

Successful results are translated by `ResultActionFilter`: a `Result<T>` becomes `200 OK`
with the value, and a plain `Result` becomes `204 No Content`.

## Caching

The four read paths use `HybridCache`, registered by `builder.Services.AddHybridCache()`.
No distributed cache is configured, therefore the cache is currently in-process only.
Registering an `IDistributedCache` promotes it to two levels without touching handler code.

| Query | Cache key | Tag |
|---|---|---|
| `GetOrderQuery` | `orders:{orderId}` | `orders` |
| `GetAllOrdersQuery` | `orders:page:{page}:{pageSize}` | `orders` |
| `GetInventoryItemsAvailabilityQuery` | `inventory:{productId}` | `inventory` |
| `GetPaymentStatusQuery` | `payments:{transactionId}` | `payments` |

Entries expire after two minutes, locally and distributed, from
`Order.Processing.Application/Abstractions/Caching/CacheEntries.cs`.

Writes invalidate by tag after `SaveChangesAsync` succeeds:

| Command | Tag cleared |
|---|---|
| `CreateOrderCommand`, `UpdateOrderStatusCommand` | `orders` |
| `ReserveInventoryItemCommand`, `ReleaseInventoryItemCommand` | `inventory` |
| `ProcessPaymentCommand` | `payments` |

Two design notes worth knowing before changing this code:

1. Handlers cache the response record, not the `Result` wrapper. `Result<T>` has no public
   parameterless constructor, so it serialises but cannot be deserialised, which would fail
   on the first cache hit. `ArchitectureTests/Caching/CachedResponseTests.cs` guards the
   round-trip for every cached record.
2. A miss is cached as well, because `HybridCache` always stores whatever the factory
   returns. Tag invalidation on every write is what keeps a negative entry from outliving
   the next change.

Invalidation is deliberately by tag rather than by key. Paged keys cannot be enumerated,
so per-key eviction would leave stale list pages behind.

## Adding a new slice

Slices live under `Order.Processing.Application/Features/{Aggregate}/{UseCase}/`. Scrutor
registers every handler by convention in `AddApplication`, so no manual registration is
needed.

A query, with its response record beside it:

```csharp
public sealed record GetOrderQuery(Guid OrderId) : IQuery<Result<OrderResponse>>;

public sealed record OrderResponse(Guid OrderId, string CustomerId, decimal TotalAmount);
```

Its handler, using explicit constructor injection into a readonly field:

```csharp
public sealed class GetOrderQueryHandler : IQueryHandler<GetOrderQuery, Result<OrderResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetOrderQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<OrderResponse>> HandleAsync(
        GetOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Id == query.OrderId)
            .FirstOrDefaultAsync(cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderResponse>(Error.NotFound(
                "order.not_found",
                $"Order with Id {query.OrderId} not found."));
        }

        return Result.Success(new OrderResponse(order.Id, order.CustomerId, order.TotalAmount));
    }
}
```

The controller action injects the handler interface and returns the result unchanged:

```csharp
[HttpGet("{id:guid}")]
[ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
public async Task<Result<OrderResponse>> Get(Guid id, CancellationToken cancellationToken)
{
    var response = await _getOrder.HandleAsync(new GetOrderQuery(id), cancellationToken);

    return response;
}
```

Conventions to follow when extending the code:

1. Commands implement `ICommand` when they return no value, or `ICommand<Result<T>>` when
   they do. Queries implement `IQuery<Result<T>>`.
2. Handlers are `sealed`, take dependencies through an explicit constructor into readonly
   fields, and accept a `CancellationToken` that is passed to every asynchronous call.
3. Entity mapping belongs in `IEntityTypeConfiguration<T>` under
   `Order.Processing.Infrastructure/Persistence/Configurations/`. Entities carry no data
   annotations.
4. Invariants belong on the entity, as `Order.UpdateStatus` shows, not in the handler.

## Testing

```bash
dotnet test
```

31 tests, no Docker and no database server required.

`ArchitectureTests` (10 tests)

- `Layers/LayerTests.cs`: six ArchUnitNET rules asserting the dependency direction.
- `Caching/CachedResponseTests.cs`: four System.Text.Json round-trip tests over the cached
  response records, which fail if a record stops being deserialisable and would therefore
  break on the first cache hit.

`EndpointTests` (21 tests)

HTTP tests through `WebApplicationFactory<Program>`. `ApiTestFactory` replaces the
registered `DbContextOptions` with a Sqlite in-memory connection held open for the lifetime
of the fixture, creates the schema, and reuses the production `ApplicationDbSeeder`, so
tests run against the real controllers, filters, middleware, decorators, cache, and EF
mappings.

| Test class | Tests | Covers |
|---|---|---|
| `OrdersEndpointsTests` | 9 | creation and computed total, validation failures, read, pagination clamp, status transitions, 404 and 409 |
| `InventoryEndpointsTests` | 5 | availability, reserve, release, cache invalidation across reads, 404 |
| `PaymentsEndpointsTests` | 6 | processing, malformed and unknown input, status retrieval, 404 |

The reserve and release tests read availability before and after the mutation, so they fail
if cache invalidation stops working.

## Known limitations

Recorded here rather than in code comments, because they are decisions rather than defects
in the current scope.

1. **Schema creation uses `EnsureCreated`, not migrations.** There is no migration in the
   repository yet, so a schema change requires deleting `orderprocessing.db`. Generate the
   first migration with
   `dotnet ef migrations add InitialCreate --project Order.Processing.Infrastructure --startup-project OrderProcessing`
   and switch `InitialiseDatabaseAsync` to `MigrateAsync` before any deployment.
2. **Payments complete without a gateway.** `ProcessPaymentCommandHandler` records the
   transaction as `completed` on creation. A real integration would call the provider and
   derive the status from its outcome, leaving `pending` while in flight and `failed` on
   decline.
3. **Order creation does not reserve stock.** An order can be created for quantities that
   inventory cannot satisfy. Reserving within a transaction at creation time is the fix.
4. **`InventoryItem.Reserve` and `Release` cannot fail.** They return `Result` yet always
   succeed, so availability can go negative. The missing invariant belongs on the entity.
5. **No authentication or authorisation.** `UseAuthorization` is in the pipeline but no
   scheme is registered and no endpoint requires a policy.
6. **No FluentValidation validators exist.** `ValidationDecorator` is wired but receives an
   empty validator collection, so guards currently live inside the handlers. Registering
   validators with `AddValidatorsFromAssembly` activates the decorator.
7. **Inventory routes are documented with a capital letter.** `InventoryController` uses
   `[Route("api/[controller]")]`, so the OpenAPI document lists `/api/Inventory/...` while
   the order and payment controllers use explicit lowercase routes. Routing itself is
   case-insensitive, therefore `/api/inventory/PRD-001` works, but the documented casing is
   inconsistent.
