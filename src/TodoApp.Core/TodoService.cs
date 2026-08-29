namespace TodoApp.Core
{
    public sealed class TodoService
    {
        private readonly ITodoRepository _repository;
        private readonly IClock _clock;

        public TodoService(ITodoRepository repository, IClock clock)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public async Task<TodoItem> CreateAsync(
            string? title,
            string? description,
            DateOnly? dueDate,
            CancellationToken cancellationToken)
        {
            // No validation here on purpose: the constructor is the only way to build a
            // TodoItem, so putting the checks anywhere else would let an invalid one exist.
            var item = new TodoItem(Guid.NewGuid(), title, description, dueDate, _clock.UtcNow);

            await _repository.AddAsync(item, cancellationToken);

            return item;
        }

        public async Task<IReadOnlyList<TodoItem>> GetAllAsync(TodoFilter filter, TodoSort sort, CancellationToken cancellationToken)
        {
            var items = await _repository.GetAllAsync(cancellationToken);

            // "Today" comes from the injected clock, so an overdue test does not depend on
            // the day the suite happens to run.
            var today = DateOnly.FromDateTime(_clock.UtcNow);

            return Sort(items.Where(item => Matches(item, filter, today)), sort).ToList();
        }

        public async Task<TodoItem> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            Guard.NonEmptyGuid(id, nameof(id));

            var item = await _repository.GetByIdAsync(id, cancellationToken);

            return item ?? throw NotFound(id);
        }

        public async Task<TodoItem> UpdateAsync(
            Guid id,
            string? title,
            string? description,
            DateOnly? dueDate,
            CancellationToken cancellationToken)
        {
            var item = await GetByIdAsync(id, cancellationToken);

            item.UpdateDetails(title, description, dueDate);
            await SaveAsync(item, cancellationToken);

            return item;
        }

        public async Task<TodoItem> CompleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var item = await GetByIdAsync(id, cancellationToken);

            item.MarkComplete();
            await SaveAsync(item, cancellationToken);

            return item;
        }

        public async Task<TodoItem> IncompleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var item = await GetByIdAsync(id, cancellationToken);

            item.MarkIncomplete();
            await SaveAsync(item, cancellationToken);

            return item;
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            Guard.NonEmptyGuid(id, nameof(id));

            if (!await _repository.DeleteAsync(id, cancellationToken))
            {
                throw NotFound(id);
            }
        }

        // The repository reports false when the id vanished between our load and our save,
        // which is another request deleting it. That is a 404, not a lost write.
        private async Task SaveAsync(TodoItem item, CancellationToken cancellationToken)
        {
            if (!await _repository.UpdateAsync(item, cancellationToken))
            {
                throw NotFound(item.Id);
            }
        }

        // The overdue rule is not restated here. TodoItem.IsOverdue owns it, so the filter
        // and anything else that asks the question get the same answer.
        private static bool Matches(TodoItem item, TodoFilter filter, DateOnly today) => filter switch
        {
            TodoFilter.Completed => item.IsCompleted,
            TodoFilter.Incomplete => !item.IsCompleted,
            TodoFilter.Overdue => item.IsOverdue(today),
            _ => true
        };

        private static IEnumerable<TodoItem> Sort(IEnumerable<TodoItem> items, TodoSort sort) => sort switch
        {
            // Items with no due date sort last, not first. A task with no deadline is not
            // the most urgent thing on the list, which is what null-sorts-first would say.
            TodoSort.DueDate => items.OrderBy(item => item.DueDate is null).ThenBy(item => item.DueDate),
            TodoSort.Title => items.OrderBy(item => item.Title, StringComparer.OrdinalIgnoreCase),
            _ => items.OrderBy(item => item.CreatedAt)
        };

        private static NotFoundException NotFound(Guid id) =>
            new($"No to-do item was found with id '{id}'.");
    }
}
