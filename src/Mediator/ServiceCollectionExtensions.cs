using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Light.Mediator
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMediatorFromAssemblies(
            this IServiceCollection services,
            params Assembly[] assemblies)
        {
            if (assemblies is null || assemblies.Length == 0)
                throw new ArgumentException("At least one assembly must be provided.", nameof(assemblies));

            services.AddTransient<Mediator>();
            services.AddTransient<IMediator>(sp => sp.GetRequiredService<Mediator>());
            services.AddTransient<ISender>(sp => sp.GetRequiredService<Mediator>());
            services.AddTransient<IPublisher>(sp => sp.GetRequiredService<Mediator>());

            // Scan assemblies once, filter by multiple handler interface types
            var handlerInterfaceTypes = new[]
            {
                typeof(IRequestHandler<,>),
                typeof(INotificationHandler<>),
            };

            var concreteTypes = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && !t.IsInterface);

            foreach (var type in concreteTypes)
            {
                foreach (var iface in type.GetInterfaces().Where(i => i.IsGenericType))
                {
                    var def = iface.GetGenericTypeDefinition();
                    if (Array.IndexOf(handlerInterfaceTypes, def) >= 0)
                        services.AddTransient(iface, type);
                }
            }

            return services;
        }

        public static IServiceCollection AddBehaviors(
            this IServiceCollection services,
            params Type[] behaviorTypes)
        {
            var lifetime = ServiceLifetime.Transient;

            foreach (var behaviorType in behaviorTypes)
            {
                if (behaviorType.IsGenericTypeDefinition)
                {
                    services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), behaviorType, lifetime));
                }
                else
                {
                    var closedInterface = behaviorType
                        .GetInterfaces()
                        .FirstOrDefault(i => i.IsGenericType &&
                                             i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                        ?? throw new ArgumentException($"{behaviorType.Name} does not implement IPipelineBehavior<,>");

                    services.Add(new ServiceDescriptor(closedInterface, behaviorType, lifetime));
                }
            }

            return services;
        }
    }
}
