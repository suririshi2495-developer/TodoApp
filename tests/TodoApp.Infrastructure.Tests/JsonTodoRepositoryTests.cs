using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoApp.Core;

namespace TodoApp.Infrastructure.Tests
{
    public class JsonTodoRepositoryTests : IDisposable
    {
        private readonly string _directory;
        private readonly string _filePath;
        private readonly JsonTodoRepository _repository;

        public JsonTodoRepositoryTests()
        {
            _directory = Path.Combine(Path.GetTempPath(), "todoapp-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);

            _filePath = Path.Combine(_directory, "todos.json");
            _repository = NewRepository(_filePath);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }
        }

        private static JsonTodoRepository NewRepository(string path) =>
            new(path, NullLogger<JsonTodoRepository>.Instance);

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithBlankFilePath_ThrowsArgumentException(string? filePath)
        {
            Assert.Throws<ArgumentException>(() => NewRepository(filePath!));
        }

        [Fact]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new JsonTodoRepository(_filePath, null!));
        }

        [Fact]
        public async Task GetAllAsync_WhenTheFileDoesNotExist_ReturnsEmptyList()
        {
            Assert.Empty(await _repository.GetAllAsync(CancellationToken.None));
        }

        [Fact]
        public async Task AddAsync_CreatesTheFile()
        {
            await _repository.AddAsync(TestTodo.Create(), CancellationToken.None);

            Assert.True(File.Exists(_filePath));
        }

        [Fact]
        public async Task AddAsync_CreatesTheDirectoryWhenItIsMissing()
        {
            var nestedPath = Path.Combine(_directory, "nested", "todos.json");
            var repository = NewRepository(nestedPath);

            await repository.AddAsync(TestTodo.Create(), CancellationToken.None);

            Assert.True(File.Exists(nestedPath));
        }

        [Fact]
        public async Task AddAsync_LeavesNoTemporaryFileBehind()
        {
            await _repository.AddAsync(TestTodo.Create(), CancellationToken.None);

            Assert.False(File.Exists(_filePath + ".tmp"));
        }

        [Fact]
        public async Task EveryField_SurvivesARestart()
        {
            var item = new TodoItem(
                Guid.NewGuid(),
                "Buy milk",
                "Two litres",
                new DateOnly(2026, 4, 1),
                TestTodo.CreatedAt);

            await _repository.AddAsync(item, CancellationToken.None);

            var stored = await NewRepository(_filePath).GetByIdAsync(item.Id, CancellationToken.None);

            Assert.NotNull(stored);
            Assert.Equal(item.Id, stored.Id);
            Assert.Equal("Buy milk", stored.Title);
            Assert.Equal("Two litres", stored.Description);
            Assert.Equal(new DateOnly(2026, 4, 1), stored.DueDate);
            Assert.Equal(TestTodo.CreatedAt, stored.CreatedAt);
            Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
            Assert.False(stored.IsCompleted);
        }

        [Fact]
        public async Task CompletedState_SurvivesARestart()
        {
            var item = TestTodo.Create();
            item.MarkComplete();

            await _repository.AddAsync(item, CancellationToken.None);

            var stored = await NewRepository(_filePath).GetByIdAsync(item.Id, CancellationToken.None);

            Assert.NotNull(stored);
            Assert.True(stored.IsCompleted);
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
        public async Task GetByIdAsync_WithUnknownId_ReturnsNull()
        {
            Assert.Null(await _repository.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task UpdateAsync_WithKnownId_ReturnsTrueAndPersistsTheChange()
        {
            var item = TestTodo.Create("Buy milk");
            await _repository.AddAsync(item, CancellationToken.None);

            item.UpdateDetails("Buy bread", null, null);

            Assert.True(await _repository.UpdateAsync(item, CancellationToken.None));

            var stored = await NewRepository(_filePath).GetByIdAsync(item.Id, CancellationToken.None);
            Assert.NotNull(stored);
            Assert.Equal("Buy bread", stored.Title);
        }

        [Fact]
        public async Task UpdateAsync_WithUnknownId_ReturnsFalse()
        {
            Assert.False(await _repository.UpdateAsync(TestTodo.Create(), CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsync_WithKnownId_ReturnsTrueAndRemovesTheItem()
        {
            var item = TestTodo.Create();
            await _repository.AddAsync(item, CancellationToken.None);

            Assert.True(await _repository.DeleteAsync(item.Id, CancellationToken.None));
            Assert.Empty(await NewRepository(_filePath).GetAllAsync(CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsync_WithUnknownId_ReturnsFalse()
        {
            Assert.False(await _repository.DeleteAsync(Guid.NewGuid(), CancellationToken.None));
        }

        [Fact]
        public async Task GetAllAsync_WithAnEmptyFile_ReturnsEmptyListAndKeepsTheFile()
        {
            await File.WriteAllTextAsync(_filePath, string.Empty);

            Assert.Empty(await _repository.GetAllAsync(CancellationToken.None));
            Assert.True(File.Exists(_filePath));
        }

        [Fact]
        public async Task GetAllAsync_WithMalformedJson_MovesTheFileAsideAndStartsEmpty()
        {
            await File.WriteAllTextAsync(_filePath, "{ this is not json");

            Assert.Empty(await _repository.GetAllAsync(CancellationToken.None));
            Assert.False(File.Exists(_filePath));
            Assert.NotEmpty(Directory.GetFiles(_directory, "todos.json.corrupt-*"));
        }

        [Fact]
        public async Task GetAllAsync_WithAnItemThatBreaksTheDomainRules_TreatsTheFileAsCorrupt()
        {
            await File.WriteAllTextAsync(
                _filePath,
                """[{"id":"11111111-1111-1111-1111-111111111111","title":"","createdAt":"2026-01-01T09:00:00Z"}]""");

            Assert.Empty(await _repository.GetAllAsync(CancellationToken.None));
            Assert.NotEmpty(Directory.GetFiles(_directory, "todos.json.corrupt-*"));
        }

        [Fact]
        public async Task ConcurrentAdds_PersistEveryItem()
        {
            var items = Enumerable.Range(0, 20)
                .Select(i => TestTodo.Create($"Item {i}"))
                .ToList();

            await Task.WhenAll(items.Select(item => _repository.AddAsync(item, CancellationToken.None)));

            var all = await _repository.GetAllAsync(CancellationToken.None);

            Assert.Equal(20, all.Count);
        }
    }
}
