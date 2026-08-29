namespace TodoApp.Api.Contracts
{
    public sealed class TodoResponse
    {
        // required means the compiler refuses to build a response with a field left unset,
        // so adding a field here cannot silently produce nulls in the payload.
        public required Guid Id { get; init; }

        public required string Title { get; init; }

        public string? Description { get; init; }

        public DateOnly? DueDate { get; init; }

        public required bool IsCompleted { get; init; }

        public required DateTime CreatedAt { get; init; }
    }
}
