using BrimDeck.Core;
using BrimDeck.Native;

internal static class PlayerPatchPolicyTests
{
    public static void Run(string root, Action<string, bool> check)
    {
        var path = Path.Combine(root, "libcef.dll");
        var profile = new LibcefPatch(12345, new LibcefPatch.Site([10, 11], [12, 13], [14, 15], [16, 17]));
        var original = profile.Sites.Single().Window(false);
        File.WriteAllBytes(path, original);
        var refused = false;
        try { LibcefFile.Write(path, profile, true); }
        catch (InvalidOperationException) { refused = true; }
        check("player DLL patching is refused before changing bytes", refused && File.ReadAllBytes(path).SequenceEqual(original));
        check("the elevated apply entry cannot bypass the player patch policy", LibcefFile.RunElevated(profile, "apply", path) != 0 && File.ReadAllBytes(path).SequenceEqual(original));
        File.WriteAllBytes(path, profile.Sites.Single().Window(true));
        LibcefFile.Write(path, profile, false);
        check("a legacy debugging patch can still be restored", File.ReadAllBytes(path).SequenceEqual(original));
    }
}
