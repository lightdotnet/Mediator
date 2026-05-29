global using Light.Mediator;
using System.Reflection;
using WebApi.Behaviors;
using WebApi.IdFeatures.Add;
using WebApi.IdFeatures.Delete;
using WebApi.IdFeatures.Get;
using WebApi.IdFeatures.Update;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddMediatorFromAssemblies(Assembly.GetExecutingAssembly());
builder.Services.AddBehaviors(typeof(LoggingBehavior<,>));
builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(TestBehavior<,>));
//builder.Services.AddScoped<IPipelineBehavior<DeleteByIdCommand, Unit>, DeleteByIdBehavior>();
builder.Services.AddBehaviors(typeof(DeleteByIdBehavior));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/id/get", (IMediator mediator) =>
{
    return mediator.Send(new AddNewId.Command());
})
.WithName("GetNewId");

app.MapGet("/id/delete", (IMediator mediator) =>
{
    return mediator.Send(new DeleteByIdCommand(Guid.NewGuid().ToString()));
})
.WithName("DeleteId");

app.MapGet("/flows", async (IMediator mediator) =>
{
    var id = await mediator.Send(new AddNewId.Command());
    var update = await mediator.Send(new UpdateByIdCommand(id, "UpdatedValue"));
    var get = await mediator.Send(new GetById.Query(id));

    return get;
})
.WithName("Flows");

app.Run();
