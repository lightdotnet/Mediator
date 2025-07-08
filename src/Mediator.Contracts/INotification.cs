using System.Threading;
using System.Threading.Tasks;

namespace Light.Mediator
{
    public interface INotification
    { }

    public interface INotificationHandler<in TNotification>
        where TNotification : INotification
    {
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }
}