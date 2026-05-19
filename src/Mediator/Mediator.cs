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
            _serviceProvider = serviceProvider;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var requestType = request.GetType();

            var handlerWrapper = (IHandlerWrapper<TResponse>)_handlerWrappers.GetOrAdd(requestType, t =>
                Activator.CreateInstance(
                    typeof(HandlerWrapper<,>).MakeGenericType(t, typeof(TResponse)))!);

            var behaviorWrapper = (IBehaviorWrapper<TResponse>)_behaviorWrappers.GetOrAdd(requestType, t =>
                Activator.CreateInstance(
                    typeof(BehaviorWrapper<,>).MakeGenericType(t, typeof(TResponse)))!);

            return behaviorWrapper.ExecutePipeline(
                request,
                _serviceProvider,
                FinalHandler,
                cancellationToken);

            Task<TResponse> FinalHandler(CancellationToken ct) =>
                handlerWrapper.Handle(request, _serviceProvider, ct);
        }

        public Task Publish(INotification notification, CancellationToken cancellationToken = default)
        {
            var wrapper = _notificationWrappers.GetOrAdd(notification.GetType(), t =>
                (INotificationHandlerWrapper)Activator.CreateInstance(typeof(NotificationHandlerWrapper<>).MakeGenericType(t))!);

            return wrapper.Publish(notification, _serviceProvider, cancellationToken);
        }
    }
}
