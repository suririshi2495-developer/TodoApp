namespace TodoApp.Infrastructure
{
    // The on-disk shape, kept separate from TodoItem so the storage format can change
    // without touching the domain, and so the domain never needs public setters or a
    // parameterless constructor just to satisfy a serializer.
    internal sealed class TodoRecord
    {
        public Guid Id { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public DateOnly? DueDate { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
