using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoApp.Infrastructure;

namespace TodoApp.Core.Tests
{
    public class TodoServiceQueryTests
    {
        private static readonly DateTime Now = new(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        // Items are seeded straight into the repository rather than created through the
        // service, because CreatedAt comes from the clock and these tests need to control it.
        private static TodoItem Item(
            string title,
            DateOnly? dueDate = null,
            bool completed = false,
            int createdMinutesAgo = 0)
        {
            var item = new TodoItem(Guid.NewGuid(), title, null, dueDate, Now.AddMinutes(-createdMinutesAgo));

            if (completed)
            {
                item.MarkComplete();
            }

            return item;
        }

        private static async Task<TodoService> ServiceWith(params TodoItem[] items)
        {
            var repository = new InMemoryTodoRepository();

            foreach (var item in items)
            {
                await repository.AddAsync(item, CancellationToken.None);
            }

            return new TodoService(repository, new FixedClock(Now));
        }

        private static Task<IReadOnlyList<TodoItem>> Query(
            TodoService service,
            TodoFilter filter = TodoFilter.All,
            TodoSort sort = TodoSort.CreatedAt) =>
            service.GetAllAsync(filter, sort, CancellationToken.None);

        [Fact]
        public async Task GetAll_WithTheAllFilter_ReturnsEveryItem()
        {
            var service = await ServiceWith(Item("Done", completed: true), Item("Open"));

            Assert.Equal(2, (await Query(service)).Count);
        }

        [Fact]
        public async Task GetAll_WithTheCompletedFilter_ReturnsOnlyCompletedItems()
        {
            var service = await ServiceWith(Item("Done", completed: true), Item("Open"));

            var items = await Query(service, TodoFilter.Completed);

            Assert.Equal("Done", Assert.Single(items).Title);
        }

        [Fact]
        public async Task GetAll_WithTheIncompleteFilter_ReturnsOnlyIncompleteItems()
        {
            var service = await ServiceWith(Item("Done", completed: true), Item("Open"));

            var items = await Query(service, TodoFilter.Incomplete);

            Assert.Equal("Open", Assert.Single(items).Title);
        }

        [Fact]
        public async Task GetAll_WithTheOverdueFilter_ReturnsOnlyItemsPastTheirDueDate()
        {
            var service = await ServiceWith(
                Item("Late", dueDate: new DateOnly(2026, 6, 14)),
                Item("Due today", dueDate: new DateOnly(2026, 6, 15)),
                Item("Later", dueDate: new DateOnly(2026, 6, 16)),
                Item("No due date"));

            var items = await Query(service, TodoFilter.Overdue);

            Assert.Equal("Late", Assert.Single(items).Title);
        }

        [Fact]
        public async Task GetAll_WithTheOverdueFilter_ExcludesCompletedItems()
        {
            var service = await ServiceWith(
                Item("Late but done", dueDate: new DateOnly(2026, 6, 1), completed: true));

            Assert.Empty(await Query(service, TodoFilter.Overdue));
        }

        [Fact]
        public async Task GetAll_SortedByCreatedAt_ReturnsOldestFirst()
        {
            var service = await ServiceWith(
                Item("Newest", createdMinutesAgo: 0),
                Item("Oldest", createdMinutesAgo: 30),
                Item("Middle", createdMinutesAgo: 15));

            var items = await Query(service, sort: TodoSort.CreatedAt);

            Assert.Equal(new[] { "Oldest", "Middle", "Newest" }, items.Select(item => item.Title));
        }

        [Fact]
        public async Task GetAll_SortedByDueDate_PutsItemsWithoutADueDateLast()
        {
            var service = await ServiceWith(
                Item("None"),
                Item("Later", dueDate: new DateOnly(2026, 7, 1)),
                Item("Soon", dueDate: new DateOnly(2026, 6, 20)));

            var items = await Query(service, sort: TodoSort.DueDate);

            Assert.Equal(new[] { "Soon", "Later", "None" }, items.Select(item => item.Title));
        }

        [Fact]
        public async Task GetAll_SortedByTitle_IgnoresCase()
        {
            var service = await ServiceWith(Item("cherry"), Item("Banana"), Item("apple"));

            var items = await Query(service, sort: TodoSort.Title);

            Assert.Equal(new[] { "apple", "Banana", "cherry" }, items.Select(item => item.Title));
        }

        [Fact]
        public async Task GetAll_CombinesTheFilterAndTheSort()
        {
            var service = await ServiceWith(
                Item("zebra", completed: true),
                Item("apple", completed: true),
                Item("mango"));

            var items = await Query(service, TodoFilter.Completed, TodoSort.Title);

            Assert.Equal(new[] { "apple", "zebra" }, items.Select(item => item.Title));
        }
    }
}
