using System.Security.Cryptography;
using System.Text;

namespace BrimDeck.Core;

// ECDSA P-256. The trusted SPKI public key is embedded in the application, never obtained from the update feed.
public static class ManifestSignatures
{
    private static byte[] Payload(UpdateManifest manifest) => Encoding.UTF8.GetBytes("BrimDeck update manifest v1\n" + (manifest with { Signature = null }).ToJson());

    public static bool IsPublicKey(string? publicKey)
    {
        try { using var key = ReadKey(publicKey); return true; }
        catch (Exception error) when (error is CryptographicException or FormatException or ArgumentException) { return false; }
    }

    private static ECDsa ReadKey(string? publicKey)
    {
        var bytes = Convert.FromBase64String(publicKey ?? "");
        var key = ECDsa.Create();
        try
        {
            key.ImportSubjectPublicKeyInfo(bytes, out var read);
            if (read != bytes.Length || key.KeySize != 256 || key.ExportParameters(false).Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
                throw new CryptographicException("Expected a P-256 public key.");
            return key;
        }
        catch { key.Dispose(); throw; }
    }

    public static UpdateManifest Sign(UpdateManifest manifest, string privateKeyPem)
    {
        using var key = ECDsa.Create(); key.ImportFromPem(privateKeyPem);
        if (key.KeySize != 256 || key.ExportParameters(false).Curve.Oid.Value != ECCurve.NamedCurves.nistP256.Oid.Value)
            throw new CryptographicException("Expected a P-256 signing key.");
        return manifest with { Signature = Convert.ToBase64String(key.SignData(Payload(manifest), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation)) };
    }

    public static void Verify(UpdateManifest manifest, string? publicKey)
    {
        try
        {
            using var key = ReadKey(publicKey);
            var signature = Convert.FromBase64String(manifest.Signature ?? "");
            if (signature.Length == 64 && key.VerifyData(Payload(manifest), signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation)) return;
        }
        catch (Exception error) when (error is CryptographicException or FormatException or ArgumentException) { }
        throw new InvalidDataException("The update manifest has no valid publisher signature.");
    }
}
