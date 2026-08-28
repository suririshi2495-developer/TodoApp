using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken) =>
            _repository.GetAllAsync(cancellationToken);

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

        private static ValidationException NotFound(Guid id) =>
            new($"No to-do item was found with id '{id}'.");
    }
}
