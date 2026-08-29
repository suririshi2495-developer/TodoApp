using TodoApp.Core;

namespace TodoApp.Infrastructure
{
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
