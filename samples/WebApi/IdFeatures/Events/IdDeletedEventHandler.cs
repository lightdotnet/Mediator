
namespace WebApi.IdFeatures.Events;

internal class IdDeletedEventHandler(
    ILogger<IdDeletedEventHandler> logger)
    : INotificationHandler<IdDeletedEvent>
{
    public async Task Handle(IdDeletedEvent notification, CancellationToken cancellationToken)
    {
        await Task.Delay(1000, cancellationToken); // Simulate some async work

        logger.LogInformation("ID deleted: {id}", notification.Id);
    }
}
