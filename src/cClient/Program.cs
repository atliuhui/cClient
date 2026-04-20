using cClient;
using Cep;
using System.CommandLine;
using System.Text;

// https://learn.microsoft.com/zh-cn/dotnet/csharp/fundamentals/tutorials/system-command-line

var textOption = new Option<string?>("--text", "-t")
{
    Description = "CEP message wire text. If provided, --file must not be provided.",
};
var fileOption = new Option<FileInfo?>("--file", "-f")
{
    Description = "Path to a text file that contains CEP message wire text. If provided, --text must not be provided.",
};
var logOption = new Option<bool>("--log", "-l")
{
    Description = "Enable request/response log file output. Default is false.",
};

var runCommand = new Command("run", "Execute a CEP request message")
{
    Options = { textOption, fileOption, logOption, },
};
runCommand.SetAction(async (parseResult, cancellationToken) =>
{
    var text = parseResult.GetValue(textOption);
    var file = parseResult.GetValue(fileOption);
    var writeLog = parseResult.GetValue(logOption);

    if (file is not null)
    {
        using var reader = file.OpenText();
        text = await reader.ReadToEndAsync(cancellationToken);
    }

    if (!string.IsNullOrWhiteSpace(text))
    {
        await RunAsync(text, writeLog, cancellationToken);
    }
});

var rootCommand = new RootCommand("CEP Client CLI - execute CEP request messages")
{
    Options = { textOption, fileOption, logOption, },
    Subcommands = { runCommand, },
};

return rootCommand.Parse(args).Invoke();

static async Task RunAsync(string text, bool writeLog, CancellationToken cancellationToken)
{
    var session = DateTime.Now.ToString("yyyyMMddHHmmss");
    var request = CepRequestMessage.Parse(text);
    string? logsDirectory = null;
    if (writeLog)
    {
        logsDirectory = Path.Combine(Environment.CurrentDirectory, "logs");
        Directory.CreateDirectory(logsDirectory);
    }

    if (writeLog)
    {
        var requestLogPath = Path.Combine(logsDirectory!, $"{session}.{nameof(request)}.log");
        File.WriteAllText(requestLogPath, request.ToString(), Encoding.UTF8);
    }

    using (var meter = new ActionMeter($"{request.Command}"))
    {
        var print = new Action<string?>(text =>
        {
            if (string.IsNullOrEmpty(text)) return;
            meter.Print(text);
        });

        var client = new CepClient()
        {
            PrintStandardOutput = print,
            PrintStandardError = print,
        };

        var response = await client.RunAsync(request, cancellationToken);
        if (writeLog)
        {
            var responseLogPath = Path.Combine(logsDirectory!, $"{session}.{nameof(response)}.log");
            File.WriteAllText(responseLogPath, response.ToString(), Encoding.UTF8);
        }

        meter.Clear();
        Console.WriteLine(response.Payload);
        ActionReporter.Print(session, request, response, meter.Elapsed);
    }
}
