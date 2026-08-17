using System.Threading;
using System.Threading.Tasks;

namespace Light.Mediator
{
    public interface IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken);
    }

    public interface IPipelineBehavior<TRequest> : IPipelineBehavior<TRequest, Unit>
        where TRequest : IRequest<Unit>
    { }
}
