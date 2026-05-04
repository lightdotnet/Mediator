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

        Assert.That(response.Reply, Is.EqualTo("Pong: Hello"));
    }

    // Send — no handler registered should throw
    [Test]
    public void Send_ShouldThrow_WhenNoHandlerRegistered()
    {
        var provider = new FakeServiceProvider(); // nothing registered

        var mediator = new MediatorClass(provider);

        Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new PingRequest("test")));
    }
}