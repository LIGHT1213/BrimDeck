using System.Net;
using System.Security.Cryptography;
using BrimDeck.Core;

internal static class ManifestSignatureTests
{
    public static async Task Run(string root, Action<string, bool> check)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var publicKey = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo());
        var installer = new byte[] { 1, 2, 3 };
        var manifest = new UpdateManifest("2.0.0", new([("zh-CN", "更新"), ("en-US", "Update")]), "BrimDeck-2.0.0-Setup.exe", installer.Length,
            Convert.ToHexString(SHA256.HashData(installer)));
        var signed = ManifestSignatures.Sign(manifest, key.ExportPkcs8PrivateKeyPem());
        bool Verified(UpdateManifest value, string? trusted = null)
        {
            try { ManifestSignatures.Verify(value, trusted ?? publicKey); return true; }
            catch (InvalidDataException) { return false; }
        }
        check("publisher signatures survive JSON round trips with multilingual notes", Verified(UpdateManifest.Parse(signed.ToJson())));
        check("an unsigned manifest cannot pass publisher verification", !Verified(manifest));
        check("invalid public keys cannot enable signed installation", !ManifestSignatures.IsPublicKey("") && !ManifestSignatures.IsPublicKey("invalid"));
        foreach (var (name, changed) in new[]
        {
            ("version", signed with { Version = "3.0.0" }),
            ("notes", signed with { Notes = "forged" }),
            ("file name", signed with { File = "other.exe" }),
            ("size", signed with { Size = 4 }),
            ("checksum", signed with { Sha256 = new string('a', 64) }),
            ("portable package", signed with { Portable = new("portable.zip", 3, new string('a', 64)) })
        }) check("publisher signatures reject a changed " + name, !Verified(changed));
        using var other = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        check("a replacement manifest signed by another publisher is refused", !Verified(ManifestSignatures.Sign(manifest, other.ExportPkcs8PrivateKeyPem())));

        using var server = new Feed { Manifest = signed.ToJson(), Installer = installer };
        using var http = new HttpClient(server);
        var directory = Path.Combine(root, "signed-update");
        var launches = 0;
        InstallerUpdateBackend Backend(string? trust = null) => new(http, new Uri("https://updates.test/update.json"),
            (_, _) => new Uri("https://updates.test/package.exe"), "1.0.0", directory, UpdateMode.Installer,
            _ => { launches++; return true; }, signingPublicKey: trust ?? publicKey);
        var backend = Backend();
        var release = await backend.CheckAsync(CancellationToken.None);
        await backend.DownloadAsync(release!, _ => { }, CancellationToken.None);
        Backend().PrepareRestart();
        check("a signed and hashed update can download, survive restart and launch", backend.CanInstall && launches == 1);
        File.WriteAllText(Path.Combine(directory, "pending.json"), (signed with { Notes = "tampered after download" }).ToJson());
        check("a pending manifest is authenticated again after restart", Backend().PendingRelease is null);
        server.Manifest = manifest.ToJson(); var refused = false;
        try { await Backend().CheckAsync(CancellationToken.None); }
        catch (InvalidDataException) { refused = true; }
        check("an unsigned feed never becomes an installable update", refused);
        server.Manifest = signed.ToJson(); backend = Backend("");
        release = await backend.CheckAsync(CancellationToken.None); refused = false;
        try { await backend.DownloadAsync(release!, _ => { }, CancellationToken.None); }
        catch (InvalidOperationException) { refused = true; }
        check("a build without a trust key disables automatic package downloads", !backend.CanInstall && refused && launches == 1);
    }

    private sealed class Feed : HttpMessageHandler
    {
        public string Manifest { get; set; } = "";
        public byte[] Installer { get; set; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = request.RequestUri!.AbsolutePath.EndsWith(".json")
                ? new StringContent(Manifest) : new ByteArrayContent(Installer) });
    }
}
