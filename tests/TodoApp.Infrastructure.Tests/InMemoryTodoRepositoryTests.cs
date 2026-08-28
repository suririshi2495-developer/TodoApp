using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoApp.Core;

namespace TodoApp.Infrastructure.Tests
{
    public class InMemoryTodoRepositoryTests
    {
        private readonly InMemoryTodoRepository _repository = new();

        [Fact]
        public async Task AddAsync_ThenGetByIdAsync_ReturnsTheItem()
        {
            var item = TestTodo.Create("Buy milk");

            await _repository.AddAsync(item, CancellationToken.None);
            var found = await _repository.GetByIdAsync(item.Id, CancellationToken.None);

            Assert.NotNull(found);
            Assert.Equal(item.Id, found.Id);
            Assert.Equal("Buy milk", found.Title);
        }

        [Fact]
        public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
        {
            var found = await _repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None);

            Assert.Null(found);
        }

        [Fact]
        public async Task AddAsync_WithDuplicateId_ThrowsConflictException()
        {
            var id = Guid.NewGuid();
            await _repository.AddAsync(TestTodo.WithId(id), CancellationToken.None);

            await Assert.ThrowsAsync<ConflictException>(
                () => _repository.AddAsync(TestTodo.WithId(id), CancellationToken.None));
        }

        [Fact]
        public async Task AddAsync_WithNullItem_ThrowsArgumentNullException()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _repository.AddAsync(null!, CancellationToken.None));
        }

        [Fact]
        public async Task AddAsync_StoresACopy_SoLaterCallerChangesDoNotReachTheStore()
        {
            var item = TestTodo.Create();
            await _repository.AddAsync(item, CancellationToken.None);

            item.MarkComplete();

            var stored = await _repository.GetByIdAsync(item.Id, CancellationToken.None);
            Assert.NotNull(stored);
            Assert.False(stored.IsCompleted);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsACopy_SoCallerChangesDoNotReachTheStore()
        {
            var item = TestTodo.Create();
            await _repository.AddAsync(item, CancellationToken.None);

            var first = await _repository.GetByIdAsync(item.Id, CancellationToken.None);
            Assert.NotNull(first);
            first.MarkComplete();

            var second = await _repository.GetByIdAsync(item.Id, CancellationToken.None);
            Assert.NotNull(second);
            Assert.False(second.IsCompleted);
        }

        [Fact]
        public async Task GetAllAsync_WithNoItems_ReturnsEmptyList()
        {
            var all = await _repository.GetAllAsync(CancellationToken.None);

            Assert.Empty(all);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEveryStoredItem()
        {
            await _repository.AddAsync(TestTodo.Create("First"), CancellationToken.None);
            await _repository.AddAsync(TestTodo.Create("Second"), CancellationToken.None);

            var all = await _repository.GetAllAsync(CancellationToken.None);

            Assert.Equal(2, all.Count);
            Assert.Contains(all, x => x.Title == "First");
            Assert.Contains(all, x => x.Title == "Second");
        }

        [Fact]
        public async Task UpdateAsync_WithKnownId_ReturnsTrueAndSavesTheChange()
        {
            var item = TestTodo.Create("Buy milk");
            await _repository.AddAsync(item, CancellationToken.None);

            item.UpdateDetails("Buy bread", null, null);
            var updated = await _repository.UpdateAsync(item, CancellationToken.None);

            Assert.True(updated);

            var stored = await _repository.GetByIdAsync(item.Id, CancellationToken.None);
            Assert.NotNull(stored);
            Assert.Equal("Buy bread", stored.Title);
        }

        [Fact]
        public async Task UpdateAsync_WithUnknownId_ReturnsFalse()
        {
            var updated = await _repository.UpdateAsync(TestTodo.Create(), CancellationToken.None);

            Assert.False(updated);
        }

        [Fact]
        public async Task DeleteAsync_WithKnownId_ReturnsTrueAndRemovesTheItem()
        {
            var item = TestTodo.Create();
            await _repository.AddAsync(item, CancellationToken.None);

            var deleted = await _repository.DeleteAsync(item.Id, CancellationToken.None);

            Assert.True(deleted);
            Assert.Null(await _repository.GetByIdAsync(item.Id, CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsync_WithUnknownId_ReturnsFalse()
        {
            var deleted = await _repository.DeleteAsync(Guid.NewGuid(), CancellationToken.None);

            Assert.False(deleted);
        }
    }
}
