using Light.Mediator;
using MediatorClass = Light.Mediator.Mediator;

namespace Mediator.Tests;

[TestFixture]
public class MediatorSendTests
{
    [Test]
    public async Task Send_ShouldRouteToCorrectHandler_AndReturnResponse()
    {
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<PingRequest, PongResponse>>(new PingHandler());
        var mediator = new MediatorClass(provider);
        var response = await mediator.Send(new PingRequest("Hello"));
        Assert.ShouldBe(response.Reply, "Pong: Hello");
    }

    [Test]
    public void Send_ShouldThrow_WhenNoHandlerRegistered()
    {
        var provider = new FakeServiceProvider();
        var mediator = new MediatorClass(provider);
        Assert.ShouldThrowAsync<InvalidOperationException>(() => mediator.Send(new PingRequest("test")));
    }

    [Test]
    public void Send_NullRequest_ShouldThrowArgumentNullException()
    {
        var provider = new FakeServiceProvider();
        var mediator = new MediatorClass(provider);
        var ex = Assert.ShouldThrowAsync<ArgumentNullException>(() => mediator.Send<PongResponse>(null!));
        Assert.ShouldBe(ex.ParamName, "request");
    }

    [Test]
    public async Task Send_VoidCommand_ShouldReturnUnit()
    {
        var handler = new DeleteOrderHandler();
        var provider = new FakeServiceProvider()
            .RegisterVoidHandler<DeleteOrder>(handler);
        var mediator = new MediatorClass(provider);
        var result = await mediator.Send(new DeleteOrder(1));
        Assert.ShouldBe(result, Unit.Value);
        Assert.ShouldBeTrue(handler.Executed);
    }

    [Test]
    public async Task Send_CommandWithResponse_ShouldReturnResponse()
    {
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<CreateOrder, int>>(new CreateOrderHandler());
        var mediator = new MediatorClass(provider);
        var result = await mediator.Send(new CreateOrder("Test"));
        Assert.ShouldBe(result, 42);
    }

    [Test]
    public async Task Send_Query_ShouldReturnResponse()
    {
        var provider = new FakeServiceProvider()
            .Register<IRequestHandler<GetOrderById, OrderDto>>(new GetOrderByIdHandler());
        var mediator = new MediatorClass(provider);
        var result = await mediator.Send(new GetOrderById(7));
        Assert.ShouldBe(result.Id, 7);
        Assert.ShouldBe(result.Name, "Test Order");
    }
}
