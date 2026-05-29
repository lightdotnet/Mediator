using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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
            List<Exception>? exceptions = null;

            foreach (var handler in handlers)
            {
                try
                {
                    await handler.Handle((TNotification)notification, ct).ConfigureAwait(false);
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                    exceptions ??= new List<Exception>();
                    exceptions.Add(ex);
                }
            }

            if (exceptions != null && exceptions.Count > 0)
                throw new AggregateException(
                    "One or more notification handlers failed.", exceptions);
        }
    }
}
