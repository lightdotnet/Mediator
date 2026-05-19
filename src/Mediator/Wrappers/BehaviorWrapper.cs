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
            Func<CancellationToken, Task<TResponse>> finalHandler,
            CancellationToken ct);
    }

    internal class BehaviorWrapper<TRequest, TResponse> : IBehaviorWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> ExecutePipeline(
            IRequest<TResponse> request,
            IServiceProvider sp,
            Func<CancellationToken, Task<TResponse>> finalHandler,
            CancellationToken ct)
        {
            var behaviors = sp.GetServices<IPipelineBehavior<TRequest, TResponse>>();

            Func<CancellationToken, Task<TResponse>> pipeline = finalHandler;

            foreach (var behavior in behaviors.Reverse())
            {
                var next = pipeline;
                pipeline = c => behavior.Handle((TRequest)request, next, c);
            }

            return pipeline(ct);
        }
    }
}
