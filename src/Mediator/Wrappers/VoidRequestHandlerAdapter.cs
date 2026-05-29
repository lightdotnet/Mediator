using System;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Mediator.Wrappers
{
    internal class VoidRequestHandlerAdapter<TRequest> : IRequestHandler<TRequest, Unit>
        where TRequest : IRequest<Unit>
    {
        private readonly IRequestHandler<TRequest> _inner;

        public VoidRequestHandlerAdapter(IRequestHandler<TRequest> inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public Task<Unit> Handle(TRequest request, CancellationToken cancellationToken)
        {
            var task = _inner.Handle(request, cancellationToken);

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
