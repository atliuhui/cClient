using System.Globalization;
using System.Text;

namespace Cep;

internal static class DictionaryExtension
{
    /// <summary>
    /// Attempts to assign a computed value to the dictionary and ignores invalid process-state failures.
    /// </summary>
    public static void TrySetValue(
        this IDictionary<string, string> dict,
        string key,
        Func<string> factory)
    {
        try
        {
            dict[key] = factory();
        }
        catch
        {
        }
    }

    /// <summary>
    /// Gets a string value by key, or returns the provided default when the key is missing.
    /// </summary>
    public static string GetValueOrDefault(
        this IDictionary<string, string> dict,
        string key,
        string defaultValue = default!)
    {
        return dict.TryGetValue(key, out var value)
            ? value
            : defaultValue;
    }
    /// <summary>
    /// Gets a timeout value in seconds by key, or returns the provided default when missing or invalid.
    /// </summary>
    public static TimeSpan GetTimeSpanOrDefault(
        this IDictionary<string, string> dict,
        string key,
        TimeSpan defaultValue = default!)
    {
        return (dict.TryGetValue(key, out var value)
            && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
            ? seconds > 0 ? TimeSpan.FromSeconds(seconds) : TimeSpan.Zero
            : defaultValue;
    }
    /// <summary>
    /// Gets an encoding by key name, or returns the provided default when the key is missing.
    /// </summary>
    public static Encoding GetEncodingOrDefault(
        this IDictionary<string, string> dict,
        string key,
        Encoding defaultValue = default!)
    {
        return dict.TryGetValue(key, out var value)
            ? Encoding.GetEncoding(value)
            : defaultValue;
    }

    /// <summary>
    /// Gets the CEP working directory header value.
    /// </summary>
    public static string GetWorkingDirectory(
        this IDictionary<string, string> dict,
        string defaultValue = default!)
    {
        return GetValueOrDefault(dict, "Working-Directory", defaultValue);
    }
    /// <summary>
    /// Gets the CEP timeout header value.
    /// </summary>
    public static TimeSpan GetTimeout(
        this IDictionary<string, string> dict,
        TimeSpan defaultValue = default!)
    {
        return GetTimeSpanOrDefault(dict, "Timeout", defaultValue);
    }
    /// <summary>
    /// Gets the CEP charset header value.
    /// </summary>
    public static Encoding GetEncoding(
        this IDictionary<string, string> dict,
        Encoding defaultValue = default!)
    {
        return GetEncodingOrDefault(dict, "Charset", defaultValue);
    }
}
