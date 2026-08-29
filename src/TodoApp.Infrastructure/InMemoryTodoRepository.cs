using TodoApp.Core;

namespace TodoApp.Infrastructure
{
    public sealed class InMemoryTodoRepository : ITodoRepository
    {
        private readonly object _gate = new();
        private readonly Dictionary<Guid, TodoItem> _items = new();

        public Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                IReadOnlyList<TodoItem> snapshot = _items.Values.Select(Copy).ToList();

                return Task.FromResult(snapshot);
            }
        }

        public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                return Task.FromResult(_items.TryGetValue(id, out var item) ? Copy(item) : null);
            }
        }

        public Task AddAsync(TodoItem item, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(item);

            lock (_gate)
            {
                // Without this, Dictionary.Add throws an ArgumentException whose message
                // describes the dictionary, not the request. Fail on our own terms instead.
                if (_items.ContainsKey(item.Id))
                {
                    throw new ConflictException("An item with that identifier already exists.");
                }

                _items.Add(item.Id, Copy(item));
            }

            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(TodoItem item, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(item);

            lock (_gate)
            {
                if (!_items.ContainsKey(item.Id))
                {
                    return Task.FromResult(false);
                }

                _items[item.Id] = Copy(item);

                return Task.FromResult(true);
            }
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                return Task.FromResult(_items.Remove(id));
            }
        }

        // TodoItem is mutable, so handing out the stored instance would let a caller
        // change the store without going through UpdateAsync. The JSON store cannot do
        // that (it deserializes a fresh object), and a fake that behaves differently
        // from the real thing makes tests lie.
        private static TodoItem Copy(TodoItem item) =>
            TodoItem.FromStorage(
                item.Id,
                item.Title,
                item.Description,
                item.DueDate,
                item.IsCompleted,
                item.CreatedAt);
    }
}
