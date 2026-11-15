using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using BrimDeck.Sensors;

internal static class SensorPipeTests
{
    public static async Task Run(Action<string, bool> check)
    {
        var name = "BrimDeck-SensorTest-" + Guid.NewGuid().ToString("N");
        using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using (var server = new NamedPipeServerStream(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 256, 256))
        using (var client = new NamedPipeClientStream(".", name, PipeDirection.In, PipeOptions.Asynchronous))
        {
            var connection = server.WaitForConnectionAsync(lifetime.Token);
            await client.ConnectAsync(lifetime.Token); await connection;
            var clock = Stopwatch.StartNew(); bool cancelled = false;
            try { await PipeReply.WriteAsync(server, new byte[8 * 1024 * 1024], lifetime.Token, TimeSpan.FromMilliseconds(100)); }
            catch (OperationCanceledException) { cancelled = true; }
            check("a sensor client that does not read cannot hold the response writer", cancelled && clock.Elapsed < TimeSpan.FromSeconds(3));
        }
        using (var server = new NamedPipeServerStream(name, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
        using (var client = new NamedPipeClientStream(".", name, PipeDirection.In, PipeOptions.Asynchronous))
        {
            var connection = server.WaitForConnectionAsync(lifetime.Token);
            await client.ConnectAsync(lifetime.Token); await connection;
            var reading = new StreamReader(client).ReadToEndAsync(lifetime.Token);
            await PipeReply.WriteAsync(server, "healthy report"u8.ToArray(), lifetime.Token);
            server.Dispose();
            check("a healthy client gets a complete response after the stalled client", await reading == "healthy report");
        }
    }
}
