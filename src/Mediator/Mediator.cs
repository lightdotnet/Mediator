using Light.Mediator.Wrappers;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Mediator
{
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        private static readonly ConcurrentDictionary<Type, object> _handlerWrappers
            = new ConcurrentDictionary<Type, object>();

        private static readonly ConcurrentDictionary<Type, object> _behaviorWrappers
            = new ConcurrentDictionary<Type, object>();

        private static readonly ConcurrentDictionary<Type, INotificationHandlerWrapper> _notificationWrappers
            = new ConcurrentDictionary<Type, INotificationHandlerWrapper>();

        public Mediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider
                ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();

            // TryGetValue first — avoids lambda allocation on cache hit (hot path)
            if (!_handlerWrappers.TryGetValue(requestType, out var cachedHandler))
            {
                cachedHandler = _handlerWrappers.GetOrAdd(requestType,
                    t => CreateWrapper<object>(typeof(HandlerWrapper<,>), t, typeof(TResponse)));
            }
            var handlerWrapper = (IHandlerWrapper<TResponse>)cachedHandler;

            if (!_behaviorWrappers.TryGetValue(requestType, out var cachedBehavior))
            {
                cachedBehavior = _behaviorWrappers.GetOrAdd(requestType,
                    t => CreateWrapper<object>(typeof(BehaviorWrapper<,>), t, typeof(TResponse)));
            }
            var behaviorWrapper = (IBehaviorWrapper<TResponse>)cachedBehavior;

            return behaviorWrapper.ExecutePipeline(
                request,
                _serviceProvider,
                FinalHandler,
                cancellationToken);

            Task<TResponse> FinalHandler(CancellationToken ct) =>
                handlerWrapper.Handle(request, _serviceProvider, ct);
        }

        public Task Publish(
            INotification notification,
            CancellationToken cancellationToken = default)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            var notificationType = notification.GetType();

            // TryGetValue first — avoids lambda allocation on cache hit (hot path)
            if (!_notificationWrappers.TryGetValue(notificationType, out var wrapper))
            {
                wrapper = _notificationWrappers.GetOrAdd(notificationType,
                    t => CreateWrapper<INotificationHandlerWrapper>(
                        typeof(NotificationHandlerWrapper<>), t));
            }

            return wrapper.Publish(notification, _serviceProvider, cancellationToken);
        }

        private static T CreateWrapper<T>(Type openGenericType, params Type[] typeArgs)
        {
            var closedType = openGenericType.MakeGenericType(typeArgs);
            var instance = Activator.CreateInstance(closedType);
            if (instance == null)
                throw new InvalidOperationException(
                    $"Failed to create wrapper instance of {closedType.FullName}.");
            return (T)instance;
        }
    }
}
