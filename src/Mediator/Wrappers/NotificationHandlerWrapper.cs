using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Mediator.Wrappers
{
    internal interface INotificationHandlerWrapper
    {
        Task Publish(INotification notification, IServiceProvider sp, CancellationToken ct);
    }

    internal class NotificationHandlerWrapper<TNotification> : INotificationHandlerWrapper
        where TNotification : INotification
    {
        public async Task Publish(INotification notification, IServiceProvider sp, CancellationToken ct)
        {
            var handlers = sp.GetServices<INotificationHandler<TNotification>>();

            foreach (var handler in handlers)
            {
                await handler.Handle((TNotification)notification, ct);
            }
        }
    }
}
