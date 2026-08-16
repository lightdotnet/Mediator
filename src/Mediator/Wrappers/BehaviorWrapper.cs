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
            IHandlerWrapper<TResponse> handler,
            CancellationToken ct);
    }

    internal class BehaviorWrapper<TRequest, TResponse> : IBehaviorWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> ExecutePipeline(
            IRequest<TResponse> request,
            IServiceProvider sp,
            IHandlerWrapper<TResponse> handler,
            CancellationToken ct)
        {
            var behaviors = sp.GetServices<IPipelineBehavior<TRequest, TResponse>>();

            var array = behaviors is IPipelineBehavior<TRequest, TResponse>[] a
                ? a
                : behaviors.ToArray();

            if (array.Length == 0)
                return handler.Handle(request, sp, ct);

            RequestHandlerDelegate<TResponse> pipeline = c => handler.Handle(request, sp, c);
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
