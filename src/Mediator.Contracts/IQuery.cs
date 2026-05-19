namespace Light.Mediator
{
    public interface IQuery<out TResponse> : IRequest<TResponse>
    { }
}