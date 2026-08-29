namespace TodoApp.Core
{
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
