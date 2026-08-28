using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TodoApp.Core;

namespace TodoApp.Infrastructure.Tests
{
    internal static class TestTodo
    {
        public static readonly DateTime CreatedAt = new(2026, 1, 1, 9, 0, 0, DateTimeKind.Utc);

        public static TodoItem Create(string title = "Buy milk") =>
            new(Guid.NewGuid(), title, null, null, CreatedAt);

        public static TodoItem WithId(Guid id, string title = "Buy milk") =>
            new(id, title, null, null, CreatedAt);
    }
}
