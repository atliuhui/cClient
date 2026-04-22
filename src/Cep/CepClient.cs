using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Cep;

/// <summary>
/// Executes CEP requests by spawning local command-line processes and collecting their output.
/// </summary>
public class CepClient
{
    static CepClient()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Default working directory if request does not specify one.
    /// </summary>
    public string WorkingDirectory { get; set; } = Environment.CurrentDirectory;
    /// <summary>
    /// Default text encoding used for both standard output and standard error.
    /// </summary>
    public Encoding StandardEncoding { get; set; } = Encoding.UTF8;
    /// <summary>
    /// When true, combines standard output and standard error into a single payload string.
    /// When false, prefers standard output; if standard output is empty, uses standard error.
    /// </summary>
    public bool MergeStandardOutputAndStandardError { get; set; } = true;
    /// <summary>
    /// Default timeout.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.Zero;
    /// <summary>
    /// Optional callback invoked for each line received from standard output.
    /// </summary>
    public Action<string?>? PrintStandardOutput { get; set; }
    /// <summary>
    /// Optional callback invoked for each line received from standard error.
    /// </summary>
    public Action<string?>? PrintStandardError { get; set; }

    /// <summary>
    /// Runs a CEP request and returns a response containing exit code, headers, and payload.
    /// </summary>
    public async Task<CepResponseMessage> RunAsync(
        CepRequestMessage request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var timeout = request.Headers.GetTimeout(this.Timeout);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (timeout != TimeSpan.Zero)
        {
            linked.CancelAfter(timeout);
        }

        var info = CreateProcessStartInfo(request);
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.Now;

        try
        {
            using var proc = new Process
            {
                StartInfo = info,
                EnableRaisingEvents = true,
            };

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();
            proc.OutputDataReceived += (_, e) => { stdout.AppendLine(e.Data); PrintStandardOutput?.Invoke(e.Data); };
            proc.ErrorDataReceived += (_, e) => { stderr.AppendLine(e.Data); PrintStandardError?.Invoke(e.Data); };

            if (!proc.Start())
            {
                throw new InvalidOperationException($"Failed to start process: {info.FileName}");
            }

            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            await proc.WaitForExitAsync(linked.Token).ConfigureAwait(false);
            stopwatch.Stop();

            return BuildSuccessResponse(request.Protocol, info, startTime, stopwatch.Elapsed, proc, stdout, stderr);
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        {
            stopwatch.Stop();
            var timedOut = timeout > TimeSpan.Zero && !cancellationToken.IsCancellationRequested;

            return timedOut
                ? BuildTimeoutResponse(request.Protocol, info, startTime, stopwatch.Elapsed)
                : BuildCanceledResponse(request.Protocol, info, startTime, stopwatch.Elapsed);
        }
    }

    ProcessStartInfo CreateProcessStartInfo(CepRequestMessage request)
    {
        var workdir = request.Headers.GetWorkingDirectory(this.WorkingDirectory);
        var info = new ProcessStartInfo
        {
            FileName = request.Command,
            WorkingDirectory = workdir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = request.Headers.GetEncoding(this.StandardEncoding),
            StandardErrorEncoding = request.Headers.GetEncoding(this.StandardEncoding),
        };

        foreach (var (name, value) in request.Headers)
        {
            if (EnvironmentExtension.NamePattern.IsMatch(name))
            {
                info.Environment.Add(name, value);
            }
        }

        foreach (var arg in request.Arguments)
        {
            switch (arg)
            {
                case CommandArgument.NamedArgumentValue named:
                    info.ArgumentList.Add(named.name);
                    info.ArgumentList.Add(named.value);
                    break;
                case CommandArgument.TokenArgument token:
                    info.ArgumentList.Add(token.value);
                    break;
                default:
                    info.ArgumentList.Add(arg.ToString() ?? string.Empty);
                    break;
            }
        }

        return info;
    }
    CepResponseMessage BuildSuccessResponse(
        string protocol, ProcessStartInfo info,
        DateTimeOffset startTime, TimeSpan elapsed,
        Process proc, StringBuilder stdout, StringBuilder stderr)
    {
        var response = new CepResponseMessage(protocol, proc.ExitCode)
        {
            Payload = this.MergeStandardOutputAndStandardError
                ? $"{stdout}{stderr}"
                : stdout.Length == 0 ? stderr.ToString() : stdout.ToString(),
        };

        response.Headers.TrySetValue("Working-Directory", () => info.WorkingDirectory);
        response.Headers.TrySetValue("Process-Id", () => proc.Id.ToString(CultureInfo.InvariantCulture));
        response.Headers.TrySetValue("Start-Time", () => startTime.ToString("O", CultureInfo.InvariantCulture));
        response.Headers.TrySetValue("Exit-Time", () => DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture));
        response.Headers.TrySetValue("Total-Time", () => elapsed.ToString());
        response.Headers.TrySetValue("User-Time", () => proc.UserProcessorTime.ToString());

        return response;
    }
    static CepResponseMessage BuildTimeoutResponse(
        string protocol, ProcessStartInfo info,
        DateTimeOffset startTime, TimeSpan elapsed)
    {
        var response = new CepResponseMessage(protocol, exitCode: 124) // 124: timeout
        {
            Payload = string.Empty,
        };

        response.Headers.TrySetValue("Working-Directory", () => info.WorkingDirectory);
        response.Headers.TrySetValue("Start-Time", () => startTime.ToString("O", CultureInfo.InvariantCulture));
        response.Headers.TrySetValue("Exit-Time", () => DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture));
        response.Headers.TrySetValue("Total-Time", () => elapsed.ToString());
        response.Headers.TrySetValue("Reason", () => "Timeout");

        return response;
    }
    static CepResponseMessage BuildCanceledResponse(
        string protocol, ProcessStartInfo info,
        DateTimeOffset startTime, TimeSpan elapsed)
    {
        var response = new CepResponseMessage(protocol, exitCode: 130) // 130: canceled
        {
            Payload = string.Empty,
        };

        response.Headers.TrySetValue("Working-Directory", () => info.WorkingDirectory);
        response.Headers.TrySetValue("Start-Time", () => startTime.ToString("O", CultureInfo.InvariantCulture));
        response.Headers.TrySetValue("Exit-Time", () => DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture));
        response.Headers.TrySetValue("Total-Time", () => elapsed.ToString());
        response.Headers.TrySetValue("Reason", () => "Canceled");

        return response;
    }
}
