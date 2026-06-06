using AwesomeAssertions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class PropertyDescriptorConcurrencyTests
{
    private static readonly HumlOptions Opts = HumlOptions.LatestSupported;

    private sealed class SampleDto
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool Flag { get; set; }
    }

    // ── CONC-01: 16 concurrent threads — no exceptions ────────────────────────

    [Fact]
    public void Conc01_concurrent_deserialise_does_not_throw()
    {
        PropertyDescriptor.ClearCache();

        const string huml = """
            %HUML v0.2.0
            Name: "concurrent"
            Count: 42
            Flag: true
            """;

        var exceptions = new System.Collections.Concurrent.ConcurrentBag<Exception>();
        var threads = Enumerable.Range(0, 16).Select(_ => new Thread(() =>
        {
            try { Huml.Deserialize<SampleDto>(huml, Opts); }
            catch (Exception ex) { exceptions.Add(ex); }
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        exceptions.Should().BeEmpty();
    }

    // ── CONC-02: 16 concurrent threads — correct values ───────────────────────

    [Fact]
    public void Conc02_concurrent_deserialise_returns_correct_values()
    {
        PropertyDescriptor.ClearCache();

        const string huml = """
            %HUML v0.2.0
            Name: "race"
            Count: 7
            Flag: false
            """;

        var results = new System.Collections.Concurrent.ConcurrentBag<SampleDto>();
        var threads = Enumerable.Range(0, 16).Select(_ => new Thread(() =>
        {
            var dto = Huml.Deserialize<SampleDto>(huml, Opts);
            results.Add(dto);
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        results.Should().HaveCount(16);
        results.Should().AllSatisfy(dto =>
        {
            dto.Name.Should().Be("race");
            dto.Count.Should().Be(7);
            dto.Flag.Should().BeFalse();
        });
    }

    // ── CONC-03: cache is populated after concurrent run ─────────────────────

    [Fact]
    public void Conc03_cache_populated_after_concurrent_run()
    {
        PropertyDescriptor.ClearCache();

        const string huml = """
            %HUML v0.2.0
            Name: "cached"
            Count: 1
            Flag: true
            """;

        var threads = Enumerable.Range(0, 16).Select(_ => new Thread(() =>
        {
            Huml.Deserialize<SampleDto>(huml, Opts);
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        // Cache should be warm — a second call succeeds without exception.
        var act = () => Huml.Deserialize<SampleDto>(huml, Opts);
        act.Should().NotThrow();
    }
}
