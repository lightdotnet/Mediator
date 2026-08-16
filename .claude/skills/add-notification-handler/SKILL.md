---
name: add-notification-handler
description: Scaffold a new INotification event and its INotificationHandler<T> handler(s) for Light.Mediator's Publish/fan-out path, following this repo's conventions. Use when the user asks to add a domain event, notification, or something that should react to a Publish() call (as opposed to a request/command/query handled by Send()).
---

# Add a notification handler

Notifications are Light.Mediator's fan-out mechanism (`IPublisher.Publish`), distinct from `Send()` requests: one notification can have **zero, one, or many** handlers, all of which run, versus a request which has exactly one handler. Use this when the user describes something event-like ("when X happens, do Y and Z") rather than a command/query with a single owner.

## 1. Define the notification

Implement the zero-dependency `INotification` marker from `Mediator.Contracts`:

```csharp
public record IdDeletedEvent(int Id) : INotification;
```

## 2. Write one handler per reaction — not one handler with multiple responsibilities

Each independent reaction gets its own `INotificationHandler<TNotification>`, so failures/behavior in one don't couple to another:

```csharp
internal class IdDeletedEventHandler(ILogger<IdDeletedEventHandler> logger)
    : INotificationHandler<IdDeletedEvent>
{
    public async Task Handle(IdDeletedEvent notification, CancellationToken cancellationToken)
    {
        await Task.Yield();
        logger.LogInformation("ID deleted: {id}", notification.Id);
    }
}
```

Multiple handlers for the same notification are normal and expected (see `samples/WebApi/IdFeatures/Events/IdDeletedEventHandler.cs`, which has two handlers — one logs, one "emails" — for the same event).

## 3. Registration

Nothing manual: `AddMediatorFromAssemblies` scans for `INotificationHandler<>` implementations the same way it does request handlers, with duplicate-registration protection (a type implementing the same notification interface twice, only possible via inheritance, is registered once).

## 4. Publish it

```csharp
await publisher.Publish(new IdDeletedEvent(id), cancellationToken);
```

`IPublisher` (or `IMediator`, which includes it) is injected the same way as the request-dispatch side.

## 5. Know the fan-out failure semantics before relying on them

`NotificationHandlerWrapper` runs all registered handlers for a notification **sequentially**, not in parallel. It collects any non-cancellation exceptions from all handlers and throws them together as a single `AggregateException` after every handler has run — one handler throwing does **not** stop the others from executing. `OperationCanceledException`/`TaskCanceledException`, however, propagate **immediately**, skipping any remaining handlers. Don't assume all-or-nothing or fail-fast behavior when writing a handler that depends on side effects from another handler of the same notification — there is no ordering guarantee to rely on beyond registration order, and no isolation beyond "runs in the same sequential loop."

## 6. Tests (this repo only)

Mirror `tests/Mediator.Tests/NotificationHandlers.cs` + `MediatorPublishTests.cs`:
- Define small tracking/throwing/cancelling handler variants as plain classes (e.g. `TrackingNotificationHandler` appends to a `List<string> Messages`) rather than a mocking library.
- Wire them through `FakeServiceProvider` (register multiple `INotificationHandler<TNotification>` instances to exercise fan-out) instead of a real DI container.
- Assert with the repo's `Assert` helper (`Assert.ShouldBe`, `Assert.ShouldThrowAsync`, ...), and when testing the `AggregateException` fan-out behavior, assert on `.InnerExceptions` rather than assuming a single exception type propagates directly.
