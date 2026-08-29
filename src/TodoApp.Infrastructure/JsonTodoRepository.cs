using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TodoApp.Core;

namespace TodoApp.Infrastructure
{
    public sealed class JsonTodoRepository : ITodoRepository
    {
        // Built once. Creating JsonSerializerOptions per call rebuilds the whole
        // reflection cache every time and is a well-known way to make JSON slow.
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // A SemaphoreSlim rather than lock, because every operation awaits file IO and
        // you cannot hold a lock across an await.
        private readonly SemaphoreSlim _gate = new(1, 1);

        private readonly string _filePath;
        private readonly ILogger<JsonTodoRepository> _logger;

        public JsonTodoRepository(string filePath, ILogger<JsonTodoRepository> logger)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A storage file path is required.", nameof(filePath));
            }

            // Resolved once at startup from configuration. No request value ever reaches
            // this, so there is nothing for a path traversal attempt to work with.
            _filePath = Path.GetFullPath(filePath);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);

            try
            {
                return await ReadAsync(cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);

            try
            {
                var items = await ReadAsync(cancellationToken);

                return items.FirstOrDefault(x => x.Id == id);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task AddAsync(TodoItem item, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(item);

            await _gate.WaitAsync(cancellationToken);

            try
            {
                var items = await ReadAsync(cancellationToken);

                if (items.Any(x => x.Id == item.Id))
                {
                    throw new ConflictException("An item with that identifier already exists.");
                }

                items.Add(item);

                await WriteAsync(items, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<bool> UpdateAsync(TodoItem item, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(item);

            await _gate.WaitAsync(cancellationToken);

            try
            {
                var items = await ReadAsync(cancellationToken);
                var index = items.FindIndex(x => x.Id == item.Id);

                if (index < 0)
                {
                    return false;
                }

                items[index] = item;

                await WriteAsync(items, cancellationToken);

                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);

            try
            {
                var items = await ReadAsync(cancellationToken);

                if (items.RemoveAll(x => x.Id == id) == 0)
                {
                    return false;
                }

                await WriteAsync(items, cancellationToken);

                return true;
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<List<TodoItem>> ReadAsync(CancellationToken cancellationToken)
        {
            if (!File.Exists(_filePath))
            {
                return new List<TodoItem>();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_filePath, cancellationToken);

                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<TodoItem>();
                }

                var records = JsonSerializer.Deserialize<List<TodoRecord>>(json, SerializerOptions)
                              ?? new List<TodoRecord>();

                return records.Select(ToDomain).ToList();
            }
            // Malformed JSON and a well-formed file holding an invalid item are the same
            // problem: the file cannot be trusted. The domain guards are what catch the second.
            catch (Exception exception) when (exception is JsonException or ValidationException)
            {
                QuarantineUnreadableFile(exception);

                return new List<TodoItem>();
            }
        }

        private async Task WriteAsync(List<TodoItem> items, CancellationToken cancellationToken)
        {
            var directory = Path.GetDirectoryName(_filePath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(items.Select(ToRecord).ToList(), SerializerOptions);

            // Write next to the real file, then swap it in. If the process dies partway
            // through, the previous file is still whole; the alternative is a truncated store.
            var temporaryPath = _filePath + ".tmp";

            await File.WriteAllTextAsync(temporaryPath, json, cancellationToken);

            File.Move(temporaryPath, _filePath, overwrite: true);
        }

        // Deleting the bad file would destroy whatever the user had. Moving it aside keeps
        // it for inspection and still lets the app start.
        private void QuarantineUnreadableFile(Exception exception)
        {
            var quarantinePath = $"{_filePath}.corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}";

            try
            {
                File.Move(_filePath, quarantinePath, overwrite: true);

                _logger.LogError(
                    exception,
                    "The to-do store could not be read and was moved to {QuarantinePath}. Starting from an empty list.",
                    quarantinePath);
            }
            catch (IOException moveFailure)
            {
                _logger.LogError(
                    moveFailure,
                    "The to-do store could not be read and could not be moved aside. Starting from an empty list.");
            }
        }

        private static TodoRecord ToRecord(TodoItem item) => new()
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            DueDate = item.DueDate,
            IsCompleted = item.IsCompleted,
            CreatedAt = item.CreatedAt
        };

        private static TodoItem ToDomain(TodoRecord record) =>
            TodoItem.FromStorage(
                record.Id,
                record.Title,
                record.Description,
                record.DueDate,
                record.IsCompleted,
                record.CreatedAt);
    }
}
