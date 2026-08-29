using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TodoApp.Core;
using TodoApp.Infrastructure;

namespace TodoApp.Api.Tests
{
    // Boots the real application, then replaces exactly one registration. Everything
    // else the tests exercise — routing, model binding, the controller, the service,
    // the error handler — is the production wiring.
    public sealed class TodoApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ITodoRepository>();
                services.AddSingleton<ITodoRepository, InMemoryTodoRepository>();
            });
        }
    }
}
