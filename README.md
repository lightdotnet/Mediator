# Mediator
Simple mediator pattern

### Register services from assemblies
```
builder.Services.AddMediatorFromAssemblies(Assembly.GetExecutingAssembly());
```

### Add custom behaviors
```
builder.Services.AddBehaviors(
	typeof(LoggingBehavior<,>),
	typeof(ValidationBehavior<,>)
	);
```
