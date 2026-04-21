using System.Text;
using System.Text.RegularExpressions;

namespace Cep;

internal static class EnvironmentExtension
{
    public static readonly Regex NamePattern = new(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    /// <summary>
    /// Expands placeholders in the form <c>${NAME}</c> using expansion variables first,
    /// then process environment variables as fallback.
    /// Unknown or invalid placeholders are kept unchanged.
    /// </summary>
    /// <param name="text">Input text that may contain placeholders.</param>
    /// <param name="expansionVariables">Variables used for placeholder expansion.</param>
    /// <returns>Text after placeholder expansion.</returns>
    public static string ExpandVariables(
        this string text,
        IReadOnlyDictionary<string, string>? expansionVariables)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        StringBuilder? sb = null;
        var s = text.AsSpan();
        var i = 0;
        while (i < s.Length)
        {
            var open = s[i..].IndexOf("${", StringComparison.Ordinal);
            if (open < 0) break;

            open += i; // absolute index
            var nameStart = open + 2;

            var close = s[nameStart..].IndexOf('}');
            if (close < 0) break;
            close += nameStart;

            // append prefix
            sb ??= new StringBuilder(text.Length);
            sb.Append(s[i..open]);

            var nameSpan = s[nameStart..close];
            if (NamePattern.IsMatch(nameSpan))
            {
                var name = nameSpan.ToString();
                var value = GetValue(expansionVariables, name)
                    ?? Environment.GetEnvironmentVariable(name);
                if (value is not null) sb.Append(value);
                else sb.Append(s[open..(close + 1)]); // keep ${NAME}
            }
            else
            {
                // not a valid placeholder => keep as-is
                sb.Append(s[open..(close + 1)]);
            }

            i = close + 1;
        }

        if (sb is null) return text;
        sb.Append(s[i..]);

        return sb.ToString();
    }

    static string? GetValue(
        IReadOnlyDictionary<string, string>? expansionVariables,
        string name)
    {
        if (expansionVariables is null)
        {
            return null;
        }

        if (expansionVariables.TryGetValue(name, out var value))
        {
            return value;
        }

        foreach (var (key, candidate) in expansionVariables)
        {
            if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return null;
    }
}
