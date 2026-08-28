using TodoApp.Core;
using TodoApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the in-memory todo repository
builder.Services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();
// Register the system clock
builder.Services.AddSingleton<IClock, SystemClock>();

var app = builder.Build();

// Swagger is a development tool, not a production surface: it describes every
// endpoint and payload shape, which is free reconnaissance for an attacker.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();