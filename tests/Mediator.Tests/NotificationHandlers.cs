using Light.Mediator;

namespace Mediator.Tests;

public class TrackingNotificationHandler : INotificationHandler<SimpleNotification>
{
    public List<string> Messages { get; } = new();
    public Task Handle(SimpleNotification notification, CancellationToken cancellationToken)
    {
        Messages.Add(notification.Message);
        return Task.CompletedTask;
    }
}

public class ThrowingNotificationHandler : INotificationHandler<SimpleNotification>
{
    public Task Handle(SimpleNotification notification, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Handler failed");
}

public class CancellingNotificationHandler : INotificationHandler<SimpleNotification>
{
    public Task Handle(SimpleNotification notification, CancellationToken cancellationToken)
        => throw new OperationCanceledException("Cancelled");
}

public class TaskCancellingNotificationHandler : INotificationHandler<SimpleNotification>
{
    public Task Handle(SimpleNotification notification, CancellationToken cancellationToken)
        => throw new TaskCanceledException("Task was cancelled");
}
