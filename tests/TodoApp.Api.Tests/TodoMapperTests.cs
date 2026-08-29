using TodoApp.Core;

namespace TodoApp.Api.Tests
{
    public class TodoMapperTests
    {
        private static readonly DateTime CreatedAt = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void ToResponse_CopiesEveryField()
        {
            var item = new TodoItem(
                Guid.NewGuid(),
                "Buy milk",
                "Two litres",
                new DateOnly(2026, 4, 1),
                CreatedAt);

            var response = TodoMapper.ToResponse(item);

            Assert.Equal(item.Id, response.Id);
            Assert.Equal("Buy milk", response.Title);
            Assert.Equal("Two litres", response.Description);
            Assert.Equal(new DateOnly(2026, 4, 1), response.DueDate);
            Assert.False(response.IsCompleted);
            Assert.Equal(CreatedAt, response.CreatedAt);
        }

        [Fact]
        public void ToResponse_KeepsCompletedState()
        {
            var item = new TodoItem(Guid.NewGuid(), "Buy milk", null, null, CreatedAt);
            item.MarkComplete();

            Assert.True(TodoMapper.ToResponse(item).IsCompleted);
        }

        [Fact]
        public void ToResponse_KeepsOptionalFieldsNull()
        {
            var item = new TodoItem(Guid.NewGuid(), "Buy milk", null, null, CreatedAt);

            var response = TodoMapper.ToResponse(item);

            Assert.Null(response.Description);
            Assert.Null(response.DueDate);
        }

        [Fact]
        public void ToResponse_WithNullItem_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => TodoMapper.ToResponse(null!));
        }
    }
}
