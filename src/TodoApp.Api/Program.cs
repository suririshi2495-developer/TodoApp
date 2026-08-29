using TodoApp.Core;
using TodoApp.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Missing configuration should stop the app at startup, not surface later as an
// empty store nobody notices.
var storageFilePath = builder.Configuration["Storage:FilePath"]
    ?? throw new InvalidOperationException("Configuration value 'Storage:FilePath' is required.");

// Register the in-memory todo repository
builder.Services.AddSingleton<ITodoRepository>(services =>
    new JsonTodoRepository(
        storageFilePath,
        services.GetRequiredService<ILogger<JsonTodoRepository>>()));

// Register the system clock
builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddScoped<TodoService>();

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