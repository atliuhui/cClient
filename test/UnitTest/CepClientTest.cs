using Cep;
using System.Text;

namespace UnitTest;

[TestClass]
public sealed class CepClientTest
{
    [TestMethod]
    public async Task RunAsync_Normal()
    {
        var request = new CepRequestMessage("EXEC", "dotnet", "CEP/0.1");
        request.Arguments.Add(CommandArgument.Token("--version"));

        var client = new CepClient();
        var response = await client.RunAsync(request);

        Console.WriteLine(response.ToString());

        Assert.AreEqual(0, response.ExitCode);
    }
    [TestMethod]
    public async Task RunAsync_echo()
    {
        var request = new CepRequestMessage("EXEC", "cmd", "CEP/0.1");
        request.Arguments.Add(CommandArgument.Token("/c"));
        request.Arguments.Add(CommandArgument.Named("echo", "Hello World!"));

        var client = new CepClient();
        var response = await client.RunAsync(request);

        Console.WriteLine(response.ToString());

        Assert.AreEqual(0, response.ExitCode);
    }
    [TestMethod]
    public async Task RunAsync_ffmpeg()
    {
        var request = new CepRequestMessage("EXEC", "ffmpeg", "CEP/0.1");
        request.Headers.Add("Working-Directory", @$"{Environment.GetEnvironmentVariable("USERPROFILE")}\Downloads");
        request.Arguments.Add(CommandArgument.Named("-i", "video.mp4"));
        request.Arguments.Add(CommandArgument.Named("-i", "audio.mp4"));
        request.Arguments.Add(CommandArgument.Named("-c:v", "copy"));
        request.Arguments.Add(CommandArgument.Named("-c:a", "aac"));
        request.Arguments.Add(CommandArgument.Named("-map", "0:v:0"));
        request.Arguments.Add(CommandArgument.Named("-map", "1:a:0"));
        request.Arguments.Add(CommandArgument.Token("-y"));
        request.Arguments.Add(CommandArgument.Token("output.mp4"));

        var client = new CepClient();
        var response = await client.RunAsync(request);

        Console.WriteLine(response.ToString());

        Assert.AreEqual(0, response.ExitCode);
    }
    [TestMethod]
    public async Task RunAsync_Timeout()
    {
        // ping -n 10 triggers a ~9 s wait; 1 s timeout should fire first.
        var request = new CepRequestMessage("EXEC", "ping", "CEP/0.1");
        request.Headers["Charset"] = "GBK";
        request.Headers["Timeout"] = "1";            // 1 second
        request.Arguments.Add(CommandArgument.Named("-n", "10"));
        request.Arguments.Add(CommandArgument.Token("127.0.0.1"));

        var client = new CepClient();
        var response = await client.RunAsync(request);

        Console.WriteLine(response.ToString());

        Assert.AreEqual(124, response.ExitCode);
        Assert.AreEqual("Timeout", response.Headers["Reason"]);
    }
    [TestMethod]
    public async Task RunAsync_Canceled()
    {
        var request = new CepRequestMessage("EXEC", "ping", "CEP/0.1");
        request.Headers["Charset"] = "GBK";
        request.Headers["Timeout"] = "30";            // 30 second
        request.Arguments.Add(CommandArgument.Named("-n", "10"));
        request.Arguments.Add(CommandArgument.Token("127.0.0.1"));

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(1));     // caller cancels after 1 s

        var client = new CepClient();
        var response = await client.RunAsync(request, cts.Token);

        Console.WriteLine(response.ToString());

        Assert.AreEqual(130, response.ExitCode);
        Assert.AreEqual("Canceled", response.Headers["Reason"]);
    }

    [TestMethod]
    public async Task Parse_Normal()
    {
        var request = CepRequestMessage.Parse(File.ReadAllText(@"examples/dotnet-request.cep", Encoding.UTF8));

        var client = new CepClient();
        var response = await client.RunAsync(request);

        Console.WriteLine(response.ToString());

        Assert.AreEqual(0, response.ExitCode);
    }
    [TestMethod]
    public async Task Parse_echo()
    {
        var request = CepRequestMessage.Parse(File.ReadAllText(@"examples/echo-request.cep", Encoding.UTF8));

        var client = new CepClient();
        var response = await client.RunAsync(request);

        Console.WriteLine(response.ToString());

        Assert.AreEqual(0, response.ExitCode);
    }
    [TestMethod]
    public async Task Parse_ffmpeg()
    {
        var request = CepRequestMessage.Parse(File.ReadAllText(@"examples/ffmpeg-request.cep", Encoding.UTF8));

        var client = new CepClient();
        var response = await client.RunAsync(request);

        Console.WriteLine(response.ToString());

        Assert.AreEqual(0, response.ExitCode);
    }
}
