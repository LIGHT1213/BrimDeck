using System.Security.Cryptography;
using System.Text;
using BrimDeck.Core;

try
{
    if (args is ["keygen", var destination])
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using (var file = new FileStream(destination, options))
        {
            var bytes = Encoding.UTF8.GetBytes(key.ExportPkcs8PrivateKeyPem());
            file.Write(bytes); CryptographicOperations.ZeroMemory(bytes);
        }
        Console.WriteLine("Public key (SPKI base64): " + Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()));
        Console.WriteLine("Private key saved to the requested file. Keep it outside the repository.");
        return 0;
    }
    if (args is ["sign", var manifestPath])
    {
        var privateKey = Environment.GetEnvironmentVariable("BRIMDECK_UPDATE_SIGNING_KEY");
        var publicKey = Environment.GetEnvironmentVariable("BRIMDECK_UPDATE_PUBLIC_KEY");
        if (string.IsNullOrWhiteSpace(privateKey) || !ManifestSignatures.IsPublicKey(publicKey))
            throw new InvalidOperationException("Set BRIMDECK_UPDATE_SIGNING_KEY and BRIMDECK_UPDATE_PUBLIC_KEY before signing.");
        var signed = ManifestSignatures.Sign(UpdateManifest.Parse(File.ReadAllText(manifestPath)), privateKey);
        ManifestSignatures.Verify(signed, publicKey);
        File.WriteAllText(manifestPath, signed.ToJson(), new UTF8Encoding(false));
        Console.WriteLine("Signed update manifest and verified its publisher key.");
        return 0;
    }
    if (args is ["verify", var path, var trustedKey])
    {
        ManifestSignatures.Verify(UpdateManifest.Parse(File.ReadAllText(path)), trustedKey);
        Console.WriteLine("Publisher signature verified."); return 0;
    }
    Console.Error.WriteLine("Use: keygen <new private-key file> | sign <update.json> | verify <update.json> <SPKI public key>");
    return 2;
}
catch (Exception error) when (error is IOException or InvalidDataException or System.Text.Json.JsonException or UnauthorizedAccessException or InvalidOperationException or CryptographicException or ArgumentException)
{
    // Cryptographic input and private keys are never included in diagnostics.
    Console.Error.WriteLine(error is InvalidOperationException ? error.Message : "Signing operation failed. Check the key configuration and file permissions.");
    return 1;
}
