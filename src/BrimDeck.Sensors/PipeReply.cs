using System.IO;

namespace BrimDeck.Sensors;

internal static class PipeReply
{
    // Waiting for a client to drain the pipe has no cancellation API. Only bounded asynchronous writes are used here.
    internal static async Task WriteAsync(Stream pipe, ReadOnlyMemory<byte> reply, CancellationToken stopping, TimeSpan? limit = null)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stopping);
        timeout.CancelAfter(limit ?? TimeSpan.FromSeconds(2));
        await pipe.WriteAsync(reply, timeout.Token).ConfigureAwait(false);
        await pipe.FlushAsync(timeout.Token).ConfigureAwait(false);
    }
}
