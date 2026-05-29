using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Mediator.Wrappers
{
    internal interface IBehaviorWrapper<TResponse>
    {
        Task<TResponse> ExecutePipeline(
            IRequest<TResponse> request,
            IServiceProvider sp,
            RequestHandlerDelegate<TResponse> finalHandler,
            CancellationToken ct);
    }

    internal class BehaviorWrapper<TRequest, TResponse> : IBehaviorWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> ExecutePipeline(
            IRequest<TResponse> request,
            IServiceProvider sp,
            RequestHandlerDelegate<TResponse> finalHandler,
            CancellationToken ct)
        {
            var behaviors = sp.GetServices<IPipelineBehavior<TRequest, TResponse>>();

            var array = behaviors is IPipelineBehavior<TRequest, TResponse>[] a
                ? a
                : behaviors.ToArray();

            if (array.Length == 0)
                return finalHandler(ct);

            RequestHandlerDelegate<TResponse> pipeline = finalHandler;
            for (int i = array.Length - 1; i >= 0; i--)
            {
                var behavior = array[i];
                var next = pipeline;
                pipeline = c => behavior.Handle((TRequest)request, next, c);
            }

            return pipeline(ct);
        }
    }
}
