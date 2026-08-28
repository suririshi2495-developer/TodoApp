using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoApp.Core
{
    public static class Guard
    {
        public static string RequiredText(string? value, int maxLength, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ValidationException($"'{parameterName}' is required.");
            }

            var trimmed = value.Trim();

            if (trimmed.Length > maxLength)
            {
                throw new ValidationException($"'{parameterName}' must be {maxLength} characters or fewer.");
            }

            return trimmed;
        }

        // Blank and absent mean the same thing for optional text, so both normalise to null.
        // Without this, "" and "   " and null would be three different states in storage.
        public static string? OptionalText(string? value, int maxLength, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();

            if (trimmed.Length > maxLength)
            {
                throw new ValidationException($"'{parameterName}' must be {maxLength} characters or fewer.");
            }

            return trimmed;
        }

        public static Guid NonEmptyGuid(Guid value, string parameterName)
        {
            if (value == Guid.Empty)
            {
                throw new ValidationException($"'{parameterName}' must be a non-empty identifier.");
            }

            return value;
        }

        public static DateTime UtcDateTime(DateTime value, string parameterName)
        {
            if (value.Kind != DateTimeKind.Utc)
            {
                throw new ValidationException($"'{parameterName}' must be a UTC timestamp.");
            }

            return value;
        }
    }
}
