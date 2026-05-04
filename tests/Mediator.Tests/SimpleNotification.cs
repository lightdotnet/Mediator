using Light.Mediator;

namespace Mediator.Tests;

public record SimpleNotification(string Message) : INotification;