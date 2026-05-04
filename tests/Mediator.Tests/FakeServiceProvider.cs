namespace Mediator.Tests;

// Minimal fake IServiceProvider for tests — no DI container needed
public class FakeServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = [];

    public FakeServiceProvider Register<T>(T instance) where T : notnull
    {
        _services[typeof(T)] = instance;
        return this;
    }

    public object? GetService(Type serviceType)
        => _services.TryGetValue(serviceType, out var svc) ? svc : null;
}