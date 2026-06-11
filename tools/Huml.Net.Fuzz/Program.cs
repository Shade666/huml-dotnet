using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Huml.Net.Exceptions;
using Huml.Net.Versioning;
using HumlFacade = Huml.Net.Huml;

// ─────────────────────────────────────────────────────────────────────────────
// Huml.Net.Fuzz — deterministic corpus-seeded mutation fuzzer for Huml.Parse.
//
// Oracle (the "security promise" from docs/internals/threat-model.md): for ANY
// input, Huml.Parse either succeeds or throws HumlParseException /
// HumlUnsupportedVersionException — never any other exception type, never a
// hang, never a crash.
//
// Usage:
//   dotnet run -c Release --project tools/Huml.Net.Fuzz -- [--iterations N]
//       [--seed S] [--timeout-ms T] [--repo PATH]
//
// Deterministic: same seed + same corpus => same mutation sequence. Failing
// inputs are written to tools/Huml.Net.Fuzz/crashes/ as JSON-escaped strings.
// ─────────────────────────────────────────────────────────────────────────────

int iterations = 200_000;
int seed = 20260611;
int timeoutMs = 2_000;
string repo = AppContext.BaseDirectory;
// Walk up from bin/ to the repo root (presence of fixtures/ marks it).
while (!Directory.Exists(Path.Combine(repo, "fixtures")) && Directory.GetParent(repo) is { } parent)
    repo = parent.FullName;

for (int a = 0; a < args.Length - 1; a++)
{
    switch (args[a])
    {
        case "--iterations": iterations = int.Parse(args[a + 1], CultureInfo.InvariantCulture); break;
        case "--seed": seed = int.Parse(args[a + 1], CultureInfo.InvariantCulture); break;
        case "--timeout-ms": timeoutMs = int.Parse(args[a + 1], CultureInfo.InvariantCulture); break;
        case "--repo": repo = args[a + 1]; break;
    }
}

var corpus = LoadCorpus(repo);
if (corpus.Count == 0)
{
    Console.Error.WriteLine($"No corpus found under {repo} — aborting.");
    return 2;
}

Console.WriteLine($"Huml.Net.Fuzz: {corpus.Count} corpus seeds, {iterations} iterations, seed {seed}, timeout {timeoutMs} ms");

var rng = new Random(seed);
var crashesDir = Path.Combine(repo, "tools", "Huml.Net.Fuzz", "crashes");
var sw = Stopwatch.StartNew();
int failures = 0, parsed = 0, rejected = 0;
long slowestMs = 0;
string slowestInput = "";

for (int i = 1; i <= iterations; i++)
{
    string input = Mutate(corpus[rng.Next(corpus.Count)], rng);
    var options = rng.Next(2) == 0 ? HumlOptions.Default : HumlOptions.LatestSupported;

    var iterSw = Stopwatch.StartNew();
    Exception? unexpected = null;
    bool hang = false;

    var task = Task.Run(() =>
    {
        try { HumlFacade.Parse(input, options); Interlocked.Increment(ref parsed); }
        catch (HumlParseException) { Interlocked.Increment(ref rejected); }
        catch (HumlUnsupportedVersionException) { Interlocked.Increment(ref rejected); }
        catch (Exception ex) { unexpected = ex; }
    });

    if (!task.Wait(timeoutMs))
        hang = true;

    iterSw.Stop();
    if (iterSw.ElapsedMilliseconds > slowestMs && !hang)
    {
        slowestMs = iterSw.ElapsedMilliseconds;
        slowestInput = input;
    }

    if (hang || unexpected != null)
    {
        failures++;
        Directory.CreateDirectory(crashesDir);
        string name = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)))[..16];
        string detail = hang
            ? $"HANG (> {timeoutMs} ms)"
            : $"{unexpected!.GetType().FullName}: {unexpected.Message}\n{unexpected.StackTrace}";
        File.WriteAllText(Path.Combine(crashesDir, $"{name}.txt"),
            $"seed: {seed}\niteration: {i}\noptions: {(ReferenceEquals(options, HumlOptions.Default) ? "Default" : "LatestSupported")}\nfailure: {detail}\ninput (JSON-escaped):\n{JsonSerializer.Serialize(input)}\n");
        Console.Error.WriteLine($"[{i}] FAILURE ({(hang ? "hang" : unexpected!.GetType().Name)}) -> crashes/{name}.txt");
        if (hang)
        {
            // The hung task cannot be aborted; report and stop the campaign.
            Console.Error.WriteLine("Hang detected — stopping campaign.");
            break;
        }
    }

    if (i % 10_000 == 0)
        Console.WriteLine($"  {i:N0} iters | {parsed:N0} parsed / {rejected:N0} rejected | {failures} failures | {i / Math.Max(1, sw.Elapsed.TotalSeconds):N0}/s | slowest {slowestMs} ms");
}

sw.Stop();
Console.WriteLine($"Done: {iterations:N0} iterations in {sw.Elapsed.TotalSeconds:N1}s — {parsed:N0} parsed, {rejected:N0} rejected, {failures} failures.");
if (slowestMs > 250)
    Console.WriteLine($"NOTE: slowest input took {slowestMs} ms — inspect for quadratic behaviour:\n{JsonSerializer.Serialize(slowestInput)}");
return failures == 0 ? 0 : 1;

// ── Corpus ──────────────────────────────────────────────────────────────────

static List<string> LoadCorpus(string repo)
{
    var corpus = new List<string>();
    foreach (var version in new[] { "v0.1", "v0.2", Path.Combine("extensions", "v0.1"), Path.Combine("extensions", "v0.2") })
    {
        var dir = Path.Combine(repo, "fixtures", version, "assertions");
        if (!Directory.Exists(dir)) continue;
        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(file));
            foreach (var row in doc.RootElement.EnumerateArray())
                if (row.TryGetProperty("input", out var input) && input.GetString() is { } s)
                    corpus.Add(s);
        }
    }
    foreach (var version in new[] { "v0.1", "v0.2" })
    {
        var dir = Path.Combine(repo, "fixtures", version, "documents");
        if (!Directory.Exists(dir)) continue;
        foreach (var file in Directory.GetFiles(dir, "*.huml"))
            corpus.Add(File.ReadAllText(file));
    }
    return corpus;
}

// ── Mutation engine ─────────────────────────────────────────────────────────

static string Mutate(string seed, Random rng)
{
    // Hostile dictionary: structural tokens, version directives, numeric prefixes,
    // and Unicode nasties (BOM, bidi controls, zero-width, lone surrogates).
    string[] dictionary =
    [
        "::", ": ", "- ", "\"\"\"", "```", "%HUML v0.2.0", "%HUML v0.1.0", "%HUML",
        "[]", "{}", "[ ]", "# ", "#", ",", ", ", "\n", "\r\n", "\r", "  ", "\t",
        "0x", "0b", "0o", "1e", "e+", "_", "1_0", "+", "-", "+inf", "-inf", "nan",
        "null", "true", "false", "\\u0041", "\\n", "\\\"", "\\\\",
        "﻿", "‮", "​", "‏", "\uD800", "\uDFFF", "🌏", "ഭൂമി",
        new string(' ', 64), new string('a', 256), new string('\n', 32),
    ];

    var sb = new StringBuilder(seed);
    int mutations = 1 + rng.Next(8);
    for (int m = 0; m < mutations; m++)
    {
        switch (rng.Next(8))
        {
            case 0 when sb.Length > 0: // replace a char
                sb[rng.Next(sb.Length)] = (char)rng.Next(1, 0x250);
                break;
            case 1: // insert from dictionary
                sb.Insert(rng.Next(sb.Length + 1), dictionary[rng.Next(dictionary.Length)]);
                break;
            case 2 when sb.Length > 1: // delete a span
            {
                int start = rng.Next(sb.Length);
                sb.Remove(start, Math.Min(rng.Next(1, 16), sb.Length - start));
                break;
            }
            case 3 when sb.Length > 0: // duplicate a span (amplification)
            {
                int start = rng.Next(sb.Length);
                int len = Math.Min(rng.Next(1, 64), sb.Length - start);
                string chunk = sb.ToString(start, len);
                int reps = 1 + rng.Next(8);
                for (int r = 0; r < reps; r++)
                    sb.Insert(rng.Next(sb.Length + 1), chunk);
                break;
            }
            case 4: // truncate
                if (sb.Length > 0) sb.Length = rng.Next(sb.Length);
                break;
            case 5: // prepend/append indentation noise
                sb.Insert(rng.Next(2) == 0 ? 0 : sb.Length, new string(' ', rng.Next(1, 12)));
                break;
            case 6: // deep-nesting bomb: repeated 'key::\n' at growing indent
            {
                var bomb = new StringBuilder();
                int depth = rng.Next(4, 120);
                for (int d = 0; d < depth; d++)
                    bomb.Append(new string(' ', d * 2)).Append('k').Append(d).Append("::\n");
                sb.Insert(rng.Next(sb.Length + 1), bomb.ToString());
                break;
            }
            case 7 when sb.Length > 0: // bit-flip a char
            {
                int pos = rng.Next(sb.Length);
                sb[pos] = (char)(sb[pos] ^ (1 << rng.Next(8)));
                break;
            }
        }
    }
    return sb.ToString();
}
