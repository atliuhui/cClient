using System.Text;

namespace Cep;

/// <summary>
/// Represents a CEP request message with start-line, headers, and ordered arguments.
/// </summary>
public sealed class CepRequestMessage
{
    /// <summary>
    /// Request verb in the start-line (for example, EXEC).
    /// </summary>
    public string Verb { get; }
    /// <summary>
    /// Executable command name or path in the start-line.
    /// </summary>
    public string Command { get; }
    /// <summary>
    /// Protocol token in the start-line (for example, CEP/0.1).
    /// </summary>
    public string Protocol { get; }
    /// <summary>
    /// Header fields, represented as name:value pairs.
    /// </summary>
    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// Ordered arguments.
    /// </summary>
    public IList<CommandArgument> Arguments { get; } = new List<CommandArgument>();

    /// <summary>
    /// Initializes a new CEP request message.
    /// </summary>
    /// <param name="verb">Request verb in the start-line.</param>
    /// <param name="command">Executable command name or path.</param>
    /// <param name="protocol">Protocol token (for example, CEP/0.1).</param>
    public CepRequestMessage(string verb, string command, string protocol)
    {
        Verb = verb ?? throw new ArgumentNullException(nameof(verb));
        Command = command ?? throw new ArgumentNullException(nameof(command));
        Protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
    }

    string ToText()
    {
        var builder = new StringBuilder();

        builder.AppendLine($"{Verb} {Command} {Protocol}");
        foreach (var (name, value) in Headers)
        {
            builder.AppendLine($"{name}: {value}");
        }
        builder.AppendLine();
        foreach (var argument in Arguments)
        {
            argument.WriteTo(builder);
        }

        return builder.ToString();
    }
    public override string ToString() => ToText();

    /// <summary>
    /// Parses CEP wire text into a <see cref="CepRequestMessage"/> instance.
    /// </summary>
    /// <param name="text">CEP request wire text.</param>
    /// <returns>A parsed request message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is null or empty.</exception>
    /// <exception cref="FormatException">Thrown when the wire text format is invalid.</exception>
    public static CepRequestMessage Parse(string text)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(text, nameof(text));

        using var reader = new StringReader(text);

        // ---- Start-Line ----
        var startLine = ReadNonEmptyLine(reader);
        if (startLine is null)
        {
            throw new FormatException("Missing start-line. Expected: <verb> <command> <protocol>.");
        }

        var (verb, command, protocol) = ParseStartLine(startLine);
        var message = new CepRequestMessage(verb, command, protocol);

        // ---- Headers ----
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            // blank line terminates headers section
            if (string.IsNullOrWhiteSpace(line))
            {
                break;
            }

            ParseHeaderLine(message.Headers, line);
        }

        // ---- Arguments (payload) ----
        while ((line = reader.ReadLine()) is not null)
        {
            // ignore blank lines in argument section
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            message.Arguments.Add(ParseArgumentLine(line));
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
    static (string Verb, string Command, string Protocol) ParseStartLine(string line)
    {
        var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
        {
            throw new FormatException($"Invalid start-line: '{line}'. Expected: <verb> <command> <protocol>.");
        }

        return (parts[0], parts[1], parts[2]);
    }
    static void ParseHeaderLine(IDictionary<string, string> headers, string line)
    {
        var idx = line.IndexOf(':');
        if (idx <= 0)
            throw new FormatException($"Invalid header line: '{line}'. Expected: Name: Value");

        var name = line[..idx].Trim();
        var value = line[(idx + 1)..].Trim();

        if (name.Length == 0)
            throw new FormatException($"Invalid header line: '{line}'. Header name is empty.");

        // allow empty value, but still store it
        headers[name] = value.ExpandEnvironmentVariables();
    }
    static CommandArgument ParseArgumentLine(string line)
    {
        // Normalize leading/trailing whitespace before token parsing.
        var trimmed = line.Trim();

        // Split on the first whitespace and keep the remaining text as the value.
        // Example: "-i video.mp4" => name="-i", value="video.mp4"
        // Example: "--filter_complex [0:v]scale=1280:-2" => name="--filter_complex", value="[0:v]scale=1280:-2"
        int firstWs = trimmed.IndexOfAny(new[] { ' ', '\t' });
        if (firstWs < 0)
        {
            // Single token argument (e.g., "-y" or "output.mp4")
            return CommandArgument.Token(trimmed.ExpandEnvironmentVariables());
        }

        var name = trimmed[..firstWs].Trim();
        var value = trimmed[(firstWs + 1)..].Trim();

        if (name.Length == 0)
        {
            // Defensive fallback.
            return CommandArgument.Token(trimmed.ExpandEnvironmentVariables());
        }
        if (value.Length == 0)
        {
            // "-y " => treat as standalone token, keep as "-y"
            return CommandArgument.Token(name.ExpandEnvironmentVariables());
        }

        // Named arguments are serialized in "name value" form.
        return CommandArgument.Named(name, value.ExpandEnvironmentVariables());
    }
}

/// <summary>
/// Represents an ordered argument in a Cep Message request.
/// </summary>
public abstract record CommandArgument
{
    internal abstract void WriteTo(StringBuilder builder);

    public sealed record TokenArgument(string value) : CommandArgument
    {
        internal override void WriteTo(StringBuilder builder) => builder.AppendLine(value);
    }

    public sealed record NamedArgumentValue(string name, string value) : CommandArgument
    {
        internal override void WriteTo(StringBuilder builder) => builder.AppendLine($"{name} {value}");
    }

    /// <summary>
    /// Creates a standalone command-line token argument.
    /// </summary>
    public static CommandArgument Token(string value) => new TokenArgument(value);

    /// <summary>
    /// Creates a named argument in the form &lt;name&gt; &lt;value&gt;.
    /// </summary>
    public static CommandArgument Named(string name, string value) => new NamedArgumentValue(name, value);
}
