global using Light.Mediator;
using System.Reflection;
using WebApi.Behaviors;
using WebApi.IdFeatures.Add;
using WebApi.IdFeatures.Delete;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddMediatorFromAssemblies(Assembly.GetExecutingAssembly());
builder.Services.AddBehaviors(typeof(LoggingBehavior<,>));

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

app.Run();
