using System.ComponentModel.DataAnnotations;
using TodoApp.Core;

namespace TodoApp.Api.Contracts
{
    public sealed class UpdateTodoRequest
    {
        [Required]
        [StringLength(TodoItem.MaxTitleLength)]
        public string? Title { get; init; }

        [StringLength(TodoItem.MaxDescriptionLength)]
        public string? Description { get; init; }

        public DateOnly? DueDate { get; init; }
    }
}
