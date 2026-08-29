namespace TodoApp.Core.Tests
{
    // Hands back an item on read but reports it gone on write. That is exactly what
    // happens when another request deletes the item between the service's load and its
    // save, and it is the only way to reach that branch from a test.
    internal sealed class VanishingTodoRepository : ITodoRepository
    {
        private readonly TodoItem _item;

        public VanishingTodoRepository(TodoItem item) => _item = item;

        public Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<TodoItem>>(new[] { _item });

        public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<TodoItem?>(id == _item.Id ? _item : null);

        public Task AddAsync(TodoItem item, CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<bool> UpdateAsync(TodoItem item, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(false);
    }
}
