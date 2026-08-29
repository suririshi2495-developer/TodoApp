namespace TodoApp.Core.Tests
{
    public class TodoItemBehaviourTests
    {
        [Fact]
        public void UpdateDetails_WithValidInput_ReplacesFields()
        {
            var item = ValidTodo.Create("Buy milk", "Semi-skimmed", new DateOnly(2026, 3, 1));

            item.UpdateDetails("Buy bread", "Sourdough", new DateOnly(2026, 4, 1));

            Assert.Equal("Buy bread", item.Title);
            Assert.Equal("Sourdough", item.Description);
            Assert.Equal(new DateOnly(2026, 4, 1), item.DueDate);
        }

        [Fact]
        public void UpdateDetails_WithBlankDescription_ClearsDescription()
        {
            var item = ValidTodo.Create("Buy milk", "Semi-skimmed");

            item.UpdateDetails("Buy milk", "   ", null);

            Assert.Null(item.Description);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDetails_WithMissingTitle_ThrowsValidationException(string? title)
        {
            var item = ValidTodo.Create();

            Assert.Throws<ValidationException>(() => item.UpdateDetails(title, null, null));
        }

        [Fact]
        public void UpdateDetails_WithInvalidDescription_LeavesItemUnchanged()
        {
            var item = ValidTodo.Create("Buy milk", "Semi-skimmed", new DateOnly(2026, 3, 1));
            var tooLong = new string('a', TodoItem.MaxDescriptionLength + 1);

            Assert.Throws<ValidationException>(
                () => item.UpdateDetails("Buy bread", tooLong, new DateOnly(2026, 4, 1)));

            Assert.Equal("Buy milk", item.Title);
            Assert.Equal("Semi-skimmed", item.Description);
            Assert.Equal(new DateOnly(2026, 3, 1), item.DueDate);
        }

        [Fact]
        public void MarkComplete_WhenIncomplete_SetsCompleted()
        {
            var item = ValidTodo.Create();

            item.MarkComplete();

            Assert.True(item.IsCompleted);
        }

        [Fact]
        public void MarkComplete_WhenAlreadyComplete_ThrowsConflictException()
        {
            var item = ValidTodo.Create();
            item.MarkComplete();

            Assert.Throws<ConflictException>(item.MarkComplete);
        }

        [Fact]
        public void MarkIncomplete_WhenComplete_SetsIncomplete()
        {
            var item = ValidTodo.Create();
            item.MarkComplete();

            item.MarkIncomplete();

            Assert.False(item.IsCompleted);
        }

        [Fact]
        public void MarkIncomplete_WhenAlreadyIncomplete_ThrowsConflictException()
        {
            var item = ValidTodo.Create();

            Assert.Throws<ConflictException>(item.MarkIncomplete);
        }

        [Fact]
        public void IsOverdue_WithDueDateBeforeToday_ReturnsTrue()
        {
            var item = ValidTodo.Create(dueDate: new DateOnly(2026, 3, 1));

            Assert.True(item.IsOverdue(new DateOnly(2026, 3, 2)));
        }

        [Fact]
        public void IsOverdue_WithDueDateToday_ReturnsFalse()
        {
            var item = ValidTodo.Create(dueDate: new DateOnly(2026, 3, 1));

            Assert.False(item.IsOverdue(new DateOnly(2026, 3, 1)));
        }

        [Fact]
        public void IsOverdue_WithDueDateAfterToday_ReturnsFalse()
        {
            var item = ValidTodo.Create(dueDate: new DateOnly(2026, 3, 5));

            Assert.False(item.IsOverdue(new DateOnly(2026, 3, 1)));
        }

        [Fact]
        public void IsOverdue_WithNoDueDate_ReturnsFalse()
        {
            var item = ValidTodo.Create(dueDate: null);

            Assert.False(item.IsOverdue(new DateOnly(2026, 3, 1)));
        }

        [Fact]
        public void IsOverdue_WhenCompleted_ReturnsFalse()
        {
            var item = ValidTodo.Create(dueDate: new DateOnly(2026, 3, 1));
            item.MarkComplete();

            Assert.False(item.IsOverdue(new DateOnly(2026, 3, 2)));
        }
    }
}
