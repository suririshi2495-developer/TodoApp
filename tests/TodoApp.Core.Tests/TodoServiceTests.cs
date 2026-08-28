using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoApp.Infrastructure;

namespace TodoApp.Core.Tests
{
    public class TodoServiceTests
    {
        private static readonly DateTime Now = new(2026, 3, 10, 8, 30, 0, DateTimeKind.Utc);

        private readonly InMemoryTodoRepository _repository = new();
        private readonly TodoService _service;

        public TodoServiceTests()
        {
            _service = new TodoService(_repository, new FixedClock(Now));
        }

        [Fact]
        public void Constructor_WithNullRepository_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TodoService(null!, new FixedClock(Now)));
        }

        [Fact]
        public void Constructor_WithNullClock_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TodoService(_repository, null!));
        }

        [Fact]
        public async Task CreateAsync_StoresTheItemAndReturnsIt()
        {
            var dueDate = new DateOnly(2026, 4, 1);

            var created = await _service.CreateAsync("Buy milk", "Two litres", dueDate, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, created.Id);
            Assert.Equal("Buy milk", created.Title);
            Assert.Equal("Two litres", created.Description);
            Assert.Equal(dueDate, created.DueDate);
            Assert.False(created.IsCompleted);

            var stored = await _repository.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.NotNull(stored);
            Assert.Equal("Buy milk", stored.Title);
        }

        [Fact]
        public async Task CreateAsync_SetsCreatedAtFromTheClock()
        {
            var created = await _service.CreateAsync("Buy milk", null, null, CancellationToken.None);

            Assert.Equal(Now, created.CreatedAt);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateAsync_WithMissingTitle_ThrowsValidationException(string? title)
        {
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.CreateAsync(title, null, null, CancellationToken.None));

            Assert.Empty(await _repository.GetAllAsync(CancellationToken.None));
        }

        [Fact]
        public async Task GetAllAsync_WithNoItems_ReturnsEmptyList()
        {
            Assert.Empty(await _service.GetAllAsync(CancellationToken.None));
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEveryStoredItem()
        {
            await _service.CreateAsync("First", null, null, CancellationToken.None);
            await _service.CreateAsync("Second", null, null, CancellationToken.None);

            var all = await _service.GetAllAsync(CancellationToken.None);

            Assert.Equal(2, all.Count);
        }

        [Fact]
        public async Task GetByIdAsync_WithKnownId_ReturnsTheItem()
        {
            var created = await _service.CreateAsync("Buy milk", null, null, CancellationToken.None);

            var found = await _service.GetByIdAsync(created.Id, CancellationToken.None);

            Assert.Equal(created.Id, found.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WithUnknownId_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task GetByIdAsync_WithEmptyId_ThrowsValidationException()
        {
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.GetByIdAsync(Guid.Empty, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateAsync_WithKnownId_SavesTheChange()
        {
            var created = await _service.CreateAsync("Buy milk", "Two litres", null, CancellationToken.None);

            await _service.UpdateAsync(created.Id, "Buy bread", null, new DateOnly(2026, 5, 1), CancellationToken.None);

            var stored = await _repository.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.NotNull(stored);
            Assert.Equal("Buy bread", stored.Title);
            Assert.Null(stored.Description);
            Assert.Equal(new DateOnly(2026, 5, 1), stored.DueDate);
        }

        [Fact]
        public async Task UpdateAsync_WithUnknownId_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.UpdateAsync(Guid.NewGuid(), "Buy bread", null, null, CancellationToken.None));
        }

        [Fact]
        public async Task UpdateAsync_WithMissingTitle_ThrowsAndLeavesTheStoreUnchanged()
        {
            var created = await _service.CreateAsync("Buy milk", null, null, CancellationToken.None);

            await Assert.ThrowsAsync<ValidationException>(
                () => _service.UpdateAsync(created.Id, "   ", null, null, CancellationToken.None));

            var stored = await _repository.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.NotNull(stored);
            Assert.Equal("Buy milk", stored.Title);
        }

        [Fact]
        public async Task UpdateAsync_WhenTheItemIsDeletedBeforeTheSave_ThrowsNotFoundException()
        {
            var item = new TodoItem(Guid.NewGuid(), "Buy milk", null, null, Now);
            var service = new TodoService(new VanishingTodoRepository(item), new FixedClock(Now));

            await Assert.ThrowsAsync<ValidationException>(
                () => service.UpdateAsync(item.Id, "Buy bread", null, null, CancellationToken.None));
        }

        [Fact]
        public async Task CompleteAsync_MarksTheItemComplete()
        {
            var created = await _service.CreateAsync("Buy milk", null, null, CancellationToken.None);

            var completed = await _service.CompleteAsync(created.Id, CancellationToken.None);

            Assert.True(completed.IsCompleted);

            var stored = await _repository.GetByIdAsync(created.Id, CancellationToken.None);
            Assert.NotNull(stored);
            Assert.True(stored.IsCompleted);
        }

        [Fact]
        public async Task CompleteAsync_WhenAlreadyComplete_ThrowsConflictException()
        {
            var created = await _service.CreateAsync("Buy milk", null, null, CancellationToken.None);
            await _service.CompleteAsync(created.Id, CancellationToken.None);

            await Assert.ThrowsAsync<ConflictException>(
                () => _service.CompleteAsync(created.Id, CancellationToken.None));
        }

        [Fact]
        public async Task IncompleteAsync_MarksTheItemIncomplete()
        {
            var created = await _service.CreateAsync("Buy milk", null, null, CancellationToken.None);
            await _service.CompleteAsync(created.Id, CancellationToken.None);

            var reopened = await _service.IncompleteAsync(created.Id, CancellationToken.None);

            Assert.False(reopened.IsCompleted);
        }

        [Fact]
        public async Task IncompleteAsync_WhenAlreadyIncomplete_ThrowsConflictException()
        {
            var created = await _service.CreateAsync("Buy milk", null, null, CancellationToken.None);

            await Assert.ThrowsAsync<ConflictException>(
                () => _service.IncompleteAsync(created.Id, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsync_WithKnownId_RemovesTheItem()
        {
            var created = await _service.CreateAsync("Buy milk", null, null, CancellationToken.None);

            await _service.DeleteAsync(created.Id, CancellationToken.None);

            Assert.Null(await _repository.GetByIdAsync(created.Id, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsync_WithUnknownId_ThrowsNotFoundException()
        {
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.DeleteAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsync_WithEmptyId_ThrowsValidationException()
        {
            await Assert.ThrowsAsync<ValidationException>(
                () => _service.DeleteAsync(Guid.Empty, CancellationToken.None));
        }
    }
}
