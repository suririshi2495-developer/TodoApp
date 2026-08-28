using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoApp.Core
{
    public interface ITodoRepository
    {
        Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken);

        Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

        Task AddAsync(TodoItem item, CancellationToken cancellationToken);

        // Returns false when the id no longer exists, so a delete that races an update
        // surfaces as 404 instead of silently resurrecting the item.
        Task<bool> UpdateAsync(TodoItem item, CancellationToken cancellationToken);

        Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
    }
}
