namespace Light.Mediator
{
    public interface IQuery<out TResponse> : IRequest<TResponse>
    { }

    public interface IQueryHandler<in TRequest, TResponse>
        : IRequestHandler<TRequest, TResponse>
        where TRequest : IQuery<TResponse>
    { }
}