using Light.Mediator;

namespace Mediator.Tests;

public class FakeServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, List<object>> _services = new();

    public FakeServiceProvider Register<T>(T instance) where T : notnull
    {
        var type = typeof(T);
        if (!_services.TryGetValue(type, out var list))
        {
            list = new List<object>();
            _services[type] = list;
        }
        list.Add(instance);
        return this;
    }

    public FakeServiceProvider RegisterVoidHandler<TRequest>(IRequestHandler<TRequest> handler)
        where TRequest : IRequest<Unit>
    {
        Register<IRequestHandler<TRequest>>(handler);
        Register<IRequestHandler<TRequest, Unit>>(new VoidAdapter<TRequest>(handler));
        return this;
    }

    public object? GetService(Type serviceType)
    {
        if (_services.TryGetValue(serviceType, out var exact))
            return exact.Count == 1 ? exact[0] : exact;

        if (serviceType.IsGenericType &&
            serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = serviceType.GetGenericArguments()[0];

            if (_services.TryGetValue(elementType, out var list))
            {
                var array = Array.CreateInstance(elementType, list.Count);
                for (int i = 0; i < list.Count; i++)
                    array.SetValue(list[i], i);
                return array;
            }

            return Array.CreateInstance(elementType, 0);
        }

        return null;
    }

    private class VoidAdapter<TRequest> : IRequestHandler<TRequest, Unit>
        where TRequest : IRequest<Unit>
    {
        private readonly IRequestHandler<TRequest> _inner;
        public VoidAdapter(IRequestHandler<TRequest> inner) => _inner = inner;

        public Task<Unit> Handle(TRequest request, CancellationToken ct)
        {
            var task = _inner.Handle(request, ct);
            if (task.IsCompletedSuccessfully)
                return Unit.Task;
            return Awaited(task);
        }

        private static async Task<Unit> Awaited(Task task)
        {
            await task.ConfigureAwait(false);
            return Unit.Value;
        }
    }
}
