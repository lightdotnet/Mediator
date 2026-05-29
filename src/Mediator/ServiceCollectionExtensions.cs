using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (assemblies == null || assemblies.Length == 0)
                throw new ArgumentException(
                    "At least one assembly must be provided.", nameof(assemblies));

            // Register Mediator — idempotent via TryAdd
            services.TryAddTransient<Mediator>();
            services.TryAddTransient<IMediator>(sp => sp.GetRequiredService<Mediator>());
            services.TryAddTransient<ISender>(sp => sp.GetRequiredService<Mediator>());
            services.TryAddTransient<IPublisher>(sp => sp.GetRequiredService<Mediator>());

            // Scan assemblies — use safe type loading
            var concreteTypes = assemblies
                .SelectMany(GetLoadableTypes)
                .Where(t => !t.IsAbstract && !t.IsInterface);

            foreach (var type in concreteTypes)
            {
                foreach (var iface in type.GetInterfaces().Where(i => i.IsGenericType))
                {
                    var def = iface.GetGenericTypeDefinition();

                    if (def == typeof(IRequestHandler<,>))
                    {
                        // Single handler per request type — TryAdd prevents duplicate
                        services.TryAddTransient(iface, type);
                    }
                    else if (def == typeof(INotificationHandler<>))
                    {
                        // Multiple handlers per notification — allow all,
                        // but prevent exact duplicate (same interface + same implementation)
                        if (!services.Any(d => d.ServiceType == iface
                                            && d.ImplementationType == type))
                        {
                            services.AddTransient(iface, type);
                        }
                    }
                }
            }

            return services;
        }

        public static IServiceCollection AddBehaviors(
            this IServiceCollection services,
            params Type[] behaviorTypes)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            if (behaviorTypes == null || behaviorTypes.Length == 0)
                return services;

            foreach (var behaviorType in behaviorTypes)
            {
                if (behaviorType == null)
                    throw new ArgumentNullException(
                        nameof(behaviorTypes), "Behavior type cannot be null.");

                if (behaviorType.IsGenericTypeDefinition)
                {
                    services.Add(new ServiceDescriptor(
                        typeof(IPipelineBehavior<,>),
                        behaviorType,
                        ServiceLifetime.Transient));
                }
                else
                {
                    var closedInterface = behaviorType
                        .GetInterfaces()
                        .FirstOrDefault(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                        ?? throw new ArgumentException(
                            $"{behaviorType.Name} does not implement IPipelineBehavior<,>.");

                    services.Add(new ServiceDescriptor(
                        closedInterface,
                        behaviorType,
                        ServiceLifetime.Transient));
                }
            }

            return services;
        }

        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.OfType<Type>().ToArray();
            }
        }
    }
}
