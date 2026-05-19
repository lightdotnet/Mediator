namespace Light.Mediator
{
    public interface ICommand<out TResponse> : IRequest<TResponse>
    { }
}