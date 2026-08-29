namespace TodoApp.Core.Tests
{
    public class TodoItemCreationTests
    {
        [Fact]
        public void Constructor_WithValidInput_SetsAllProperties()
        {
            var dueDate = new DateOnly(2026, 3, 1);

            var item = new TodoItem(ValidTodo.Id, "Buy milk", "Semi-skimmed", dueDate, ValidTodo.CreatedAt);

            Assert.Equal(ValidTodo.Id, item.Id);
            Assert.Equal("Buy milk", item.Title);
            Assert.Equal("Semi-skimmed", item.Description);
            Assert.Equal(dueDate, item.DueDate);
            Assert.Equal(ValidTodo.CreatedAt, item.CreatedAt);
            Assert.False(item.IsCompleted);
        }

        [Fact]
        public void Constructor_WithEmptyId_ThrowsValidationException()
        {
            var exception = Assert.Throws<ValidationException>(
                () => new TodoItem(Guid.Empty, "Buy milk", null, null, ValidTodo.CreatedAt));

            Assert.Contains("'id'", exception.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithMissingTitle_ThrowsValidationException(string? title)
        {
            var exception = Assert.Throws<ValidationException>(() => ValidTodo.Create(title));

            Assert.Contains("'title'", exception.Message);
        }

        [Fact]
        public void Constructor_WithTitleOverMaxLength_ThrowsValidationException()
        {
            var title = new string('a', TodoItem.MaxTitleLength + 1);

            var exception = Assert.Throws<ValidationException>(() => ValidTodo.Create(title));

            Assert.Contains(TodoItem.MaxTitleLength.ToString(), exception.Message);
        }

        [Fact]
        public void Constructor_WithTitleAtMaxLength_Succeeds()
        {
            var title = new string('a', TodoItem.MaxTitleLength);

            var item = ValidTodo.Create(title);

            Assert.Equal(title, item.Title);
        }

        [Fact]
        public void Constructor_TrimsTitle()
        {
            var item = ValidTodo.Create("  Buy milk  ");

            Assert.Equal("Buy milk", item.Title);
        }

        [Fact]
        public void Constructor_MeasuresTitleLengthAfterTrimming()
        {
            var title = new string('a', TodoItem.MaxTitleLength);

            var item = ValidTodo.Create($"   {title}   ");

            Assert.Equal(title, item.Title);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithBlankDescription_StoresNull(string? description)
        {
            var item = ValidTodo.Create(description: description);

            Assert.Null(item.Description);
        }

        [Fact]
        public void Constructor_TrimsDescription()
        {
            var item = ValidTodo.Create(description: "  Semi-skimmed  ");

            Assert.Equal("Semi-skimmed", item.Description);
        }

        [Fact]
        public void Constructor_WithDescriptionOverMaxLength_ThrowsValidationException()
        {
            var description = new string('a', TodoItem.MaxDescriptionLength + 1);

            var exception = Assert.Throws<ValidationException>(
                () => ValidTodo.Create(description: description));

            Assert.Contains("'description'", exception.Message);
        }

        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void Constructor_WithNonUtcCreatedAt_ThrowsValidationException(DateTimeKind kind)
        {
            var createdAt = new DateTime(2026, 1, 1, 9, 0, 0, kind);

            var exception = Assert.Throws<ValidationException>(
                () => new TodoItem(ValidTodo.Id, "Buy milk", null, null, createdAt));

            Assert.Contains("UTC", exception.Message);
        }

        [Fact]
        public void Constructor_AlwaysStartsIncomplete()
        {
            var item = ValidTodo.Create();

            Assert.False(item.IsCompleted);
        }
    }
}
