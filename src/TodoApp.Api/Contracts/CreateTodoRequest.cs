using System.ComponentModel.DataAnnotations;
using TodoApp.Core;

namespace TodoApp.Api.Contracts
{
    public sealed class CreateTodoRequest
    {
        // The attributes are a cheap gate that rejects an oversized payload before any
        // domain code runs. They are not the authority on what a valid to-do is —
        // TodoItem's constructor is — but they stop a 50 MB title at the edge.
        [Required]
        [StringLength(TodoItem.MaxTitleLength)]
        public string? Title { get; init; }

        [StringLength(TodoItem.MaxDescriptionLength)]
        public string? Description { get; init; }

        public DateOnly? DueDate { get; init; }
    }
}
