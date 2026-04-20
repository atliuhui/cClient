using Cep;
using System.Globalization;

namespace cClient;

internal static class ActionReporter
{
    public static void Print(
        string session,
        CepRequestMessage request,
        CepResponseMessage response,
        TimeSpan elapsed)
    {
        Console.WriteLine();

        var status = GetStatus(response);
        var statusText = status switch
        {
            "success" => "succeeded",
            "timeout" => "timed out",
            "canceled" => "canceled",
            _ => "failed",
        };
        var heading = $"[cclient] execution {statusText}.";

        var statusColor = status switch
        {
            "success" => ConsoleColor.Green,
            "timeout" => ConsoleColor.Yellow,
            "canceled" => ConsoleColor.DarkYellow,
            _ => ConsoleColor.Red,
        };

        var details = new List<(string Label, string Value)>
        {
            ("Session", session),
            ("Command", request.Command),
            ("Protocol", request.Protocol),
        };

        if (response.Headers.TryGetValue("Reason", out var reason))
        {
            details.Add(("Reason", reason));
        }
        else
        {
            details.Add(("Exit Code", response.ExitCode.ToString(CultureInfo.InvariantCulture)));
        }

        details.Add(("Time", DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture)));
        details.Add(("Duration", $"{elapsed.TotalMilliseconds.ToString("N0", CultureInfo.InvariantCulture)} ms ({elapsed:c})"));

        if (response.Headers.TryGetValue("Working-Directory", out var workingDirectory))
        {
            details.Add(("Work Dir", workingDirectory));
        }

        if (response.Headers.TryGetValue("Process-Id", out var processId))
        {
            details.Add(("Process Id", processId));
        }

        var labelWidth = details.Max(x => x.Label.Length);
        var rows = details.Select(x => FormatKeyValueLine(x.Label, x.Value, labelWidth)).ToList();

        var innerWidth = heading.Length;
        foreach (var row in rows)
        {
            if (row.Length > innerWidth)
            {
                innerWidth = row.Length;
            }
        }

        var maxInnerWidth = GetMaxInnerWidth();
        innerWidth = Math.Min(innerWidth, maxInnerWidth);

        WriteBoxHeaderBorder(heading, innerWidth, statusColor);
        foreach (var row in rows)
        {
            WriteBoxLine(row, innerWidth);
        }
        WriteBoxBorder(innerWidth);
    }

    static string GetStatus(CepResponseMessage response)
    {
        if (response.Headers.TryGetValue("Reason", out var reason))
        {
            return reason.Equals("Timeout", StringComparison.OrdinalIgnoreCase)
                ? "timeout"
                : reason.Equals("Canceled", StringComparison.OrdinalIgnoreCase)
                    ? "canceled"
                    : "failed";
        }

        return response.ExitCode == 0 ? "success" : "failed";
    }
    static int GetMaxInnerWidth()
    {
        var width = 120;
        try
        {
            width = Console.WindowWidth;
        }
        catch
        {
            // Keep default width for redirected output or unsupported terminals.
        }

        return Math.Max(40, width - 4);
    }
    static void WriteBoxBorder(int innerWidth)
    {
        Console.WriteLine($"└─{new string('─', innerWidth)}─┘");
    }
    static void WriteBoxHeaderBorder(string title, int innerWidth, ConsoleColor color)
    {
        var maxTitleWidth = Math.Max(1, innerWidth - 2);
        var fittedTitle = FitToWidth(title, maxTitleWidth);
        var decoratedTitle = $" {fittedTitle} ";
        var tailWidth = Math.Max(0, innerWidth - decoratedTitle.Length);
        var line = $"┌─{decoratedTitle}{new string('─', tailWidth)}─┐";

        WriteColored(line, color);
        Console.WriteLine();
    }
    static void WriteBoxLine(string text, int innerWidth, ConsoleColor? color = null)
    {
        var fitted = FitToWidth(text, innerWidth);
        var padded = fitted.PadRight(innerWidth);

        Console.Write("│ ");
        if (color.HasValue)
        {
            WriteColored(padded, color.Value);
        }
        else
        {
            Console.Write(padded);
        }
        Console.WriteLine(" │");
    }
    static string FitToWidth(string text, int width)
    {
        if (text.Length <= width)
        {
            return text;
        }

        if (width <= 3)
        {
            return text[..width];
        }

        return text[..(width - 3)] + "...";
    }
    static void WriteColored(string text, ConsoleColor color)
    {
        var original = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = original;
    }
    static string FormatKeyValueLine(string label, string value, int labelWidth)
    {
        return $"{label.PadRight(labelWidth)} : {value}";
    }
}
