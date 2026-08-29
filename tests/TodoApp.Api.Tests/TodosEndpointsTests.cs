using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Json;
using TodoApp.Api.Contracts;

namespace TodoApp.Api.Tests
{
    public class TodosEndpointsTests : IDisposable
    {
        // A fresh factory per test, so the in-memory store starts empty every time and
        // no test can pass because of a previous one.
        private readonly TodoApiFactory _factory = new();
        private readonly HttpClient _client;

        public TodosEndpointsTests()
        {
            _client = _factory.CreateClient();
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        private async Task<Guid> CreateAsync(string title = "Buy milk")
        {
            var response = await _client.PostAsJsonAsync("/api/todos", new { title });
            response.EnsureSuccessStatusCode();

            var created = await response.Content.ReadFromJsonAsync<TodoResponse>();
            Assert.NotNull(created);

            return created.Id;
        }

        [Fact]
        public async Task Post_WithValidRequest_Returns201WithAWorkingLocationHeader()
        {
            var response = await _client.PostAsJsonAsync("/api/todos", new
            {
                title = "Buy milk",
                description = "Two litres",
                dueDate = "2026-04-01"
            });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.NotNull(response.Headers.Location);

            var created = await response.Content.ReadFromJsonAsync<TodoResponse>();
            Assert.NotNull(created);
            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal("Buy milk", created.Title);
            Assert.Equal("Two litres", created.Description);
            Assert.Equal(new DateOnly(2026, 4, 1), created.DueDate);
            Assert.False(created.IsCompleted);

            var followed = await _client.GetAsync(response.Headers.Location);
            Assert.Equal(HttpStatusCode.OK, followed.StatusCode);
        }

        [Fact]
        public async Task Post_WithBlankTitle_Returns400ProblemJson()
        {
            var response = await _client.PostAsJsonAsync("/api/todos", new { title = "   " });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Post_WithTitleOverTheMaximum_Returns400()
        {
            var response = await _client.PostAsJsonAsync("/api/todos", new
            {
                title = new string('a', 201)
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Get_WithNoItems_ReturnsAnEmptyArray()
        {
            var items = await _client.GetFromJsonAsync<List<TodoResponse>>("/api/todos");

            Assert.NotNull(items);
            Assert.Empty(items);
        }

        [Fact]
        public async Task Get_ReturnsEveryCreatedItem()
        {
            await CreateAsync("First");
            await CreateAsync("Second");

            var items = await _client.GetFromJsonAsync<List<TodoResponse>>("/api/todos");

            Assert.NotNull(items);
            Assert.Equal(2, items.Count);
        }

        [Fact]
        public async Task Get_WithUnknownId_Returns404ProblemJson()
        {
            var response = await _client.GetAsync($"/api/todos/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.NotNull(problem);
            Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
        }

        [Fact]
        public async Task Get_WithMalformedId_Returns400()
        {
            var response = await _client.GetAsync("/api/todos/not-a-guid");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Get_WithEmptyId_Returns400()
        {
            var response = await _client.GetAsync($"/api/todos/{Guid.Empty}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Put_WithKnownId_Returns200AndTheUpdatedItem()
        {
            var id = await CreateAsync("Buy milk");

            var response = await _client.PutAsJsonAsync($"/api/todos/{id}", new
            {
                title = "Buy bread",
                dueDate = "2026-05-01"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var updated = await response.Content.ReadFromJsonAsync<TodoResponse>();
            Assert.NotNull(updated);
            Assert.Equal("Buy bread", updated.Title);
            Assert.Equal(new DateOnly(2026, 5, 1), updated.DueDate);
        }

        [Fact]
        public async Task Put_WithUnknownId_Returns404()
        {
            var response = await _client.PutAsJsonAsync($"/api/todos/{Guid.NewGuid()}", new { title = "Buy bread" });

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Put_WithBlankTitle_Returns400()
        {
            var id = await CreateAsync();

            var response = await _client.PutAsJsonAsync($"/api/todos/{id}", new { title = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Complete_Returns200AndMarksTheItemComplete()
        {
            var id = await CreateAsync();

            var response = await _client.PostAsync($"/api/todos/{id}/complete", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var completed = await response.Content.ReadFromJsonAsync<TodoResponse>();
            Assert.NotNull(completed);
            Assert.True(completed.IsCompleted);
        }

        [Fact]
        public async Task Complete_Twice_Returns409()
        {
            var id = await CreateAsync();
            await _client.PostAsync($"/api/todos/{id}/complete", null);

            var response = await _client.PostAsync($"/api/todos/{id}/complete", null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Incomplete_OnANewItem_Returns409()
        {
            var id = await CreateAsync();

            var response = await _client.PostAsync($"/api/todos/{id}/incomplete", null);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Incomplete_AfterComplete_Returns200AndReopensTheItem()
        {
            var id = await CreateAsync();
            await _client.PostAsync($"/api/todos/{id}/complete", null);

            var response = await _client.PostAsync($"/api/todos/{id}/incomplete", null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var reopened = await response.Content.ReadFromJsonAsync<TodoResponse>();
            Assert.NotNull(reopened);
            Assert.False(reopened.IsCompleted);
        }

        [Fact]
        public async Task Delete_WithKnownId_Returns204AndTheItemIsGone()
        {
            var id = await CreateAsync();

            var response = await _client.DeleteAsync($"/api/todos/{id}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            var afterwards = await _client.GetAsync($"/api/todos/{id}");
            Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
        }

        [Fact]
        public async Task Delete_WithUnknownId_Returns404()
        {
            var response = await _client.DeleteAsync($"/api/todos/{Guid.NewGuid()}");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Get_WithAnUnknownFilter_Returns400ProblemJson()
        {
            var response = await _client.GetAsync("/api/todos?filter=done");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Get_WithAnUnknownSort_Returns400()
        {
            var response = await _client.GetAsync("/api/todos?sort=name");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Get_WithAFilterAndASort_ReturnsTheMatchingItemsInOrder()
        {
            var first = await CreateAsync("zebra");
            await CreateAsync("mango");
            await CreateAsync("apple");

            await _client.PostAsync($"/api/todos/{first}/complete", null);

            var items = await _client.GetFromJsonAsync<List<TodoResponse>>(
                "/api/todos?filter=incomplete&sort=title");

            Assert.NotNull(items);
            Assert.Equal(new[] { "apple", "mango" }, items.Select(item => item.Title));
        }
    }
}
