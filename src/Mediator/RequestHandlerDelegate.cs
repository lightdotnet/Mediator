using System.Threading;
using System.Threading.Tasks;

namespace Light.Mediator
{
    public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);
}
