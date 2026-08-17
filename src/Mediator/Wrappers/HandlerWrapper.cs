using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Mediator.Wrappers
{
    internal interface IHandlerWrapper<TResponse>
    {
        Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    internal class HandlerWrapper<TRequest, TResponse> : IHandlerWrapper<TResponse>
        where TRequest : IRequest<TResponse>
    {
        public Task<TResponse> Handle(IRequest<TResponse> request, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var handler = serviceProvider.GetRequiredService<IRequestHandler<TRequest, TResponse>>();
            return handler.Handle((TRequest)request, cancellationToken);
        }
    }
}
