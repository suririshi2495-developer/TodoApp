using TodoApp.Api.Contracts;
using TodoApp.Core;

namespace TodoApp.Api
{
    public static class TodoMapper
    {
        public static TodoResponse ToResponse(TodoItem item)
        {
            ArgumentNullException.ThrowIfNull(item);

            return new TodoResponse
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                DueDate = item.DueDate,
                IsCompleted = item.IsCompleted,
                CreatedAt = item.CreatedAt
            };
        }
    }
}
