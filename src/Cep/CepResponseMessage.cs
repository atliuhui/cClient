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
    /// <param name="protocol">Protocol token of the response.</param>
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
}
