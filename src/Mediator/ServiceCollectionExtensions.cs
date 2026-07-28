using Light.Mediator.Wrappers;
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

            services.TryAddTransient<Mediator>();
            services.TryAddTransient<IMediator>(sp => sp.GetRequiredService<Mediator>());
            services.TryAddTransient<ISender>(sp => sp.GetRequiredService<Mediator>());
            services.TryAddTransient<IPublisher>(sp => sp.GetRequiredService<Mediator>());

            var concreteTypes = assemblies
                .SelectMany(GetLoadableTypes)
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition);

            foreach (var type in concreteTypes)
            {
                foreach (var iface in type.GetInterfaces().Where(i => i.IsGenericType))
                {
                    var def = iface.GetGenericTypeDefinition();

                    if (def == typeof(IRequestHandler<,>))
                    {
                        services.TryAddTransient(iface, type);
                    }
                    else if (def == typeof(IRequestHandler<>))
                    {
                        services.TryAddTransient(iface, type);

                        var requestType = iface.GetGenericArguments()[0];
                        var adapterInterface = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(Unit));
                        var adapterImpl = typeof(VoidRequestHandlerAdapter<>).MakeGenericType(requestType);
                        services.TryAddTransient(adapterInterface, adapterImpl);
                    }
                    else if (def == typeof(INotificationHandler<>))
                    {
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
                    if (!services.Any(d => d.ServiceType == typeof(IPipelineBehavior<,>)
                                        && d.ImplementationType == behaviorType))
                    {
                        services.Add(new ServiceDescriptor(
                            typeof(IPipelineBehavior<,>),
                            behaviorType,
                            ServiceLifetime.Transient));
                    }
                }
                else
                {
                    var closedInterfaces = behaviorType
                        .GetInterfaces()
                        .Where(i => i.IsGenericType &&
                            i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
                        .ToArray();

                    if (closedInterfaces.Length == 0)
                        throw new ArgumentException(
                            $"{behaviorType.Name} does not implement IPipelineBehavior<,>.");

                    foreach (var closedInterface in closedInterfaces)
                    {
                        if (!services.Any(d => d.ServiceType == closedInterface
                                            && d.ImplementationType == behaviorType))
                        {
                            services.Add(new ServiceDescriptor(
                                closedInterface,
                                behaviorType,
                                ServiceLifetime.Transient));
                        }
                    }
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
