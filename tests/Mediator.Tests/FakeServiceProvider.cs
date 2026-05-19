namespace Mediator.Tests;

// Minimal fake IServiceProvider for tests — no DI container needed
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

    public object? GetService(Type serviceType)
    {
        // Exact match trước
        if (_services.TryGetValue(serviceType, out var exact))
            return exact.Count == 1 ? exact[0] : exact;

        // Handle IEnumerable<T>
        if (serviceType.IsGenericType &&
            serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            var elementType = serviceType.GetGenericArguments()[0];

            if (_services.TryGetValue(elementType, out var list))
            {
                // Tạo typed array T[] để cast không bị lỗi
                var array = Array.CreateInstance(elementType, list.Count);
                for (int i = 0; i < list.Count; i++)
                    array.SetValue(list[i], i);
                return array;
            }

            // Không có registration → trả về empty array
            return Array.CreateInstance(elementType, 0);
        }

        return null;
    }
}