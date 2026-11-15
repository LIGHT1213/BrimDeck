using BrimDeck.Core;

internal static class PortableRollbackTests
{
    public static void Run(string root, Action<string, bool> check)
    {
        string Write(string folder, string file, string text)
        {
            var path = Path.Combine(folder, file);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
            return path;
        }
        void List(string folder, params string[] files) => File.WriteAllLines(Path.Combine(folder, PortableUpdate.ListFile), files);

        var source = Path.Combine(root, "new");
        Write(source, Path.Combine("layout", "new.dll"), "new");
        Write(source, "blocked.dll", "new");
        List(source, Path.Combine("layout", "new.dll"), "blocked.dll");
        var target = Path.Combine(root, "rollback");
        Write(target, "layout", "old file");
        List(target, "layout");
        Directory.CreateDirectory(Path.Combine(target, "blocked.dll"));
        bool failed = false;
        try { PortableUpdate.Apply(source, target); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { failed = true; }
        check("a failed file-to-folder update restores the old file before deleting its backup",
            failed && File.Exists(Path.Combine(target, "layout")) && File.ReadAllText(Path.Combine(target, "layout")) == "old file"
            && PortableUpdate.Files(target).Contains("layout") && Directory.GetDirectories(target, ".brimdeck-update-*").Length == 0);

        source = Path.Combine(root, "flat-new"); target = Path.Combine(root, "flat-target");
        Write(source, "layout", "new file"); List(source, "layout");
        Write(target, Path.Combine("layout", "old.dll"), "old"); List(target, Path.Combine("layout", "old.dll"));
        PortableUpdate.Apply(source, target);
        check("an emptied package folder can become a file in the next version", File.ReadAllText(Path.Combine(target, "layout")) == "new file");

        target = Path.Combine(root, "with-user-file");
        Write(target, Path.Combine("layout", "old.dll"), "old"); List(target, Path.Combine("layout", "old.dll"));
        Write(target, Path.Combine("layout", "user.txt"), "mine");
        failed = false;
        try { PortableUpdate.Apply(source, target); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { failed = true; }
        check("a folder-to-file update keeps unlisted files and restores the old package on failure",
            failed && File.ReadAllText(Path.Combine(target, "layout", "old.dll")) == "old"
            && File.ReadAllText(Path.Combine(target, "layout", "user.txt")) == "mine");

        target = Path.Combine(root, "recovery");
        Write(target, "layout", "old"); List(target, "layout");
        var recovered = Write(target, Path.Combine(".brimdeck-update-recovery", "saved.dll"), "recover me");
        PortableUpdate.Apply(source, target);
        check("a later update preserves backups left by an earlier incomplete rollback", File.Exists(recovered) && File.ReadAllText(recovered) == "recover me");
    }
}
