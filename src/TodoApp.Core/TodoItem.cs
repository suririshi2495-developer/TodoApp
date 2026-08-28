using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoApp.Core
{
    public sealed class TodoItem
    {
        public const int MaxTitleLength = 200;
        public const int MaxDescriptionLength = 1000;

        public TodoItem(Guid id, string? title, string? description, DateOnly? dueDate, DateTime createdAtUtc)
        {
            Id = Guard.NonEmptyGuid(id, nameof(id));
            Title = Guard.RequiredText(title, MaxTitleLength, nameof(title));
            Description = Guard.OptionalText(description, MaxDescriptionLength, nameof(description));
            DueDate = dueDate;
            CreatedAt = Guard.UtcDateTime(createdAtUtc, nameof(createdAtUtc));
            IsCompleted = false;
        }

        public Guid Id { get; }

        public string Title { get; private set; }

        public string? Description { get; private set; }

        public DateOnly? DueDate { get; private set; }

        public bool IsCompleted { get; private set; }

        public DateTime CreatedAt { get; }

        public void UpdateDetails(string? title, string? description, DateOnly? dueDate)
        {
            // Validate everything before assigning anything, so a rejected update
            // cannot leave the item holding a mix of new and old values.
            var validatedTitle = Guard.RequiredText(title, MaxTitleLength, nameof(title));
            var validatedDescription = Guard.OptionalText(description, MaxDescriptionLength, nameof(description));

            Title = validatedTitle;
            Description = validatedDescription;
            DueDate = dueDate;
        }

        public void MarkComplete()
        {
            if (IsCompleted)
            {
                throw new ConflictException("The item is already completed.");
            }

            IsCompleted = true;
        }

        public void MarkIncomplete()
        {
            if (!IsCompleted)
            {
                throw new ConflictException("The item is already incomplete.");
            }

            IsCompleted = false;
        }

        public bool IsOverdue(DateOnly today) =>
            DueDate.HasValue && !IsCompleted && DueDate.Value < today;

        public static TodoItem FromStorage(Guid id, string? title, string? description, DateOnly? dueDate, 
            bool isCompleted, DateTime createdAtUtc)
        {
            return new TodoItem(id, title, description, dueDate, createdAtUtc)
            {
                IsCompleted = isCompleted
            };
        }
    }
}
