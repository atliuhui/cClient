using System.Globalization;
using System.Text;

namespace Cep;

/// <summary>
/// Represents a CEP response message with protocol version, exit code, headers, and payload.
/// </summary>
public sealed class CepResponseMessage
{
    /// <summary>
    /// Protocol token of the response (for example, CEP/0.1).
    /// </summary>
    public string Protocol { get; }
    /// <summary>
    /// Process exit code for the executed command.
    /// </summary>
    public int ExitCode { get; }
    /// <summary>
    /// Header fields, represented as name:value pairs.
    /// </summary>
    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Opaque output produced during execution (payload).
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// Initializes a new CEP response message.
    /// </summary>
    /// <param name="protocol">Protocol token (for example, CEP/0.1).</param>
    /// <param name="exitCode">Process exit code for the executed command.</param>
    public CepResponseMessage(string protocol, int exitCode)
    {
        Protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
        ExitCode = exitCode;
    }

    string ToText()
    {
        var builder = new StringBuilder();

        builder.AppendLine($"{Protocol} {ExitCode}");
        foreach (var (name, value) in Headers)
        {
            builder.AppendLine($"{name}: {value}");
        }
        builder.AppendLine();
        if (string.IsNullOrEmpty(Payload) == false)
        {
            builder.AppendLine(Payload);
        }

        return builder.ToString();
    }
    public override string ToString() => ToText();

    /// <summary>
    /// Parses a CEP response text into a <see cref="CepResponseMessage"/> instance.
    /// </summary>
    /// <param name="text">Raw CEP response text.</param>
    /// <returns>The parsed CEP response message.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="text"/> is null, empty, or whitespace.</exception>
    /// <exception cref="FormatException">Thrown when the response text format is invalid.</exception>
    public static CepResponseMessage Parse(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));

        using var reader = new StringReader(text);

        // ---- Status-Line ----
        var statusLine = ReadNonEmptyLine(reader);
        if (statusLine is null)
        {
            throw new FormatException("Missing start-line. Expected: <protocol> <exitcode>.");
        }

        var (protocol, exitcode) = ParseStatusLine(statusLine);
        var message = new CepResponseMessage(protocol, exitcode);

        // ---- Headers ----
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            // blank line terminates headers section
            if (line.Length == 0)
            {
                break;
            }

            ParseHeaderLine(message.Headers, line);
        }

        // ---- Payload ----
        var payload = reader.ReadToEnd().Trim();
        if (payload.Length > 0)
        {
            message.Payload = payload;
        }

        return message;
    }
    static string? ReadNonEmptyLine(StringReader reader)
    {
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (!string.IsNullOrWhiteSpace(line))
                return line;
        }
        return null;
    }
    static (string Protocol, int ExitCode) ParseStatusLine(string line)
    {
        var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            throw new FormatException($"Invalid status-line: '{line}'. Expected: <protocol> <exitcode>.");
        }

        if (int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var exitCode))
        {
            return (parts[0], exitCode);
        }
        else
        {
            throw new FormatException("Invalid response exit code.");
        }
    }
    static void ParseHeaderLine(IDictionary<string, string> headers, string line)
    {
        var idx = line.IndexOf(':');
        if (idx <= 0)
        {
            throw new FormatException($"Invalid header line: '{line}'. Expected: Name: Value");
        }

        var name = line[..idx].Trim();
        var value = line[(idx + 1)..].Trim();

        if (name.Length == 0)
        {
            throw new FormatException($"Invalid header line: '{line}'. Header name is empty.");
        }

        // allow empty value, but still store it
        headers[name] = value;
    }
}
