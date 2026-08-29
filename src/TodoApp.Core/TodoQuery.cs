using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoApp.Core
{
    public static class TodoQuery
    {
        public static TodoFilter ParseFilter(string? value) => value?.Trim().ToLowerInvariant() switch
        {
            null or "" or "all" => TodoFilter.All,
            "completed" => TodoFilter.Completed,
            "incomplete" => TodoFilter.Incomplete,
            "overdue" => TodoFilter.Overdue,
            _ => throw new ValidationException("'filter' must be one of: all, completed, incomplete, overdue.")
        };

        public static TodoSort ParseSort(string? value) => value?.Trim().ToLowerInvariant() switch
        {
            null or "" or "createdat" => TodoSort.CreatedAt,
            "duedate" => TodoSort.DueDate,
            "title" => TodoSort.Title,
            _ => throw new ValidationException("'sort' must be one of: createdAt, dueDate, title.")
        };
    }
}
