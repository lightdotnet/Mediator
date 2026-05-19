using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Mediator.Wrappers
{
    internal interface IHandlerWrapper<TResponse>
    {
        Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider sp, CancellationToken ct);
    }

    internal class HandlerWrapper<TRequest, TResponse> : IHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider sp, CancellationToken ct)
        {
            var handler = sp.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
            return handler.Handle((TRequest)request, ct);
        }
    }
}
