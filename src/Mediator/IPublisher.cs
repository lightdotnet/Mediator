using System.Threading;
using System.Threading.Tasks;

namespace Light.Mediator
{
    public interface IPublisher
    {
        Task Publish(INotification notification, CancellationToken cancellationToken = default);
    }
}
