using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

namespace Light.Mediator
{
    public static class InvokeExtesions
    {
        public static MethodInfo GetHandleMethod(this Type type)
        {
            return type.GetMethod("Handle")
                ?? throw new InvalidOperationException($"Handle method not found on {type}.");
        }

        public static object InvokeHandleMethod(this Type type,
            IServiceProvider serviceProvider,
            object[] parameters)
        {
            var handler = serviceProvider.GetRequiredService(type);

            // Find Handle method on the resolved handler
            var handleMethod = type.GetHandleMethod();

            return handleMethod.Invoke(handler, parameters)
                ?? throw new InvalidOperationException($"Handle method on {type.FullName} returned null.");
        }

        public static object InvokeHandleMethod(this Type type,
            object obj,
            object[] parameters)
        {
            // Find Handle method on the resolved handler
            var handleMethod = type.GetHandleMethod();

            return handleMethod.Invoke(obj, parameters)
                ?? throw new InvalidOperationException($"Handle method on {type.FullName} returned null.");
        }
    }
}
