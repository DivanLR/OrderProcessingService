using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Order.Processing.Api.Extensions;
using Order.Processing.Api.Middleware;
using Order.Processing.Application;
using Order.Processing.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture));

// Add services to the container.

builder.Services
    .AddControllers(options => options.Filters.Add<ResultActionFilter>())
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters
        .Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHybridCache();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.InitialiseDatabaseAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
app.UseHttpsRedirection();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();
