using System.Diagnostics.CodeAnalysis;

namespace Core.Extensions;

public static class StringExtensions
{
    [return: NotNullIfNotNull("value")]
    public static string? Truncate (this string? value, int maxLength, string truncationSuffix = "…")
        => value?.Length > maxLength
            ? value[..maxLength] + truncationSuffix
            : value;
}
