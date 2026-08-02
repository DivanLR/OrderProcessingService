using Microsoft.Extensions.DependencyInjection;
using Order.Processing.Application.Abstractions.Behaviors;
using Order.Processing.Application.Abstractions.Messaging;

namespace Order.Processing.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.Scan(scan => scan.FromAssembliesOf(typeof(DependencyInjection))
            .AddClasses(
                classes => classes
                    .AssignableTo(typeof(IQueryHandler<,>))
                    .Where(type => !type.IsGenericTypeDefinition),
                publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(
                classes => classes
                    .AssignableTo(typeof(ICommandHandler<>))
                    .Where(type => !type.IsGenericTypeDefinition),
                publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime()
            .AddClasses(
                classes => classes
                    .AssignableTo(typeof(ICommandHandler<,>))
                    .Where(type => !type.IsGenericTypeDefinition),
                publicOnly: false)
                .AsImplementedInterfaces()
                .WithScopedLifetime());

        services.TryDecorate(typeof(ICommandHandler<>), typeof(ValidationDecorator.CommandBaseHandler<>));

        services.TryDecorate(typeof(IQueryHandler<,>), typeof(LoggingDecorator.QueryHandler<,>));
        services.TryDecorate(typeof(ICommandHandler<>), typeof(LoggingDecorator.CommandBaseHandler<>));

        return services;
    }
}
