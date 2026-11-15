namespace BrimDeck.Core;

// Plain PNG exports for drag-and-drop, including leftovers from an unclean shutdown.
public static class TemporaryPictures
{
    public static void Clear(string folder, DateTime? olderThan = null)
    {
        try
        {
            if (!Directory.Exists(folder)) return;
            foreach (var path in Directory.EnumerateFiles(folder, "*.png", SearchOption.TopDirectoryOnly))
                try
                {
                    if (olderThan is null || File.GetLastWriteTimeUtc(path) < olderThan.Value) File.Delete(path);
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
    }
}
