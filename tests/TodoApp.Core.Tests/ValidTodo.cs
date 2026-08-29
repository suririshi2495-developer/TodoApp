namespace TodoApp.Core.Tests
{
    internal static class ValidTodo
    {
        public static readonly Guid Id = Guid.Parse("3f2504e0-4f89-41d3-9a0c-0305e82c3301");

        public static readonly DateTime CreatedAt = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        public static TodoItem Create(
            string? title = "Buy milk",
            string? description = null,
            DateOnly? dueDate = null)
            => new(Id, title, description, dueDate, CreatedAt);
    }
}
