namespace WebApi.IdFeatures.Events;

public record IdDeletedEvent(string Id) : INotification;
