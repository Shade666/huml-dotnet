using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

/// <summary>
/// Regression tests for TASK-019 (review finding H1): overridden and new-shadowed
/// properties must serialise exactly once, with the derived-most declaration winning,
/// and the reflection path must agree with the source-generated path.
/// </summary>
public sealed class InheritanceDedupTests
{
    // ── Test hierarchies (reflection path) ────────────────────────────────────

    public class DedupAnimal
    {
        public virtual string Name { get; set; } = "Generic";
        public int Legs { get; set; } = 4;
    }

    public class DedupDog : DedupAnimal
    {
        public override string Name { get; set; } = "Rex";
        public string Breed { get; set; } = "Collie";
    }

    public class DedupShadowBase
    {
        public int Id { get; set; } = 1;
    }

    public class DedupShadowDerived : DedupShadowBase
    {
        public new int Id { get; set; } = 2;
    }

    public class DedupLevel1
    {
        public virtual string Tag { get; set; } = "l1";
        public int Count { get; set; } = 10;
    }

    public class DedupLevel2 : DedupLevel1
    {
        public override string Tag { get; set; } = "l2";
        public new int Count { get; set; } = 20;
    }

    public class DedupLevel3 : DedupLevel2
    {
        public override string Tag { get; set; } = "l3";
        public new int Count { get; set; } = 30;
    }

    private static int CountOccurrences(string text, string token)
        => text.Split([token], StringSplitOptions.None).Length - 1;

    // ── AC #1: virtual/override serialises exactly once, derived-most wins ──

    [Fact]
    public void Override_property_serialises_exactly_once()
    {
        var huml = HumlSerializer.Serialize(new DedupDog());
        CountOccurrences(huml, "Name:").Should().Be(1);
    }

    [Fact]
    public void Override_property_emits_derived_value()
    {
        var huml = HumlSerializer.Serialize(new DedupDog());
        huml.Should().Contain("Name: \"Rex\"");
    }

    [Fact]
    public void Override_property_keeps_base_declaration_position()
    {
        var huml = HumlSerializer.Serialize(new DedupDog());
        var nameIdx = huml.IndexOf("Name:", StringComparison.Ordinal);
        var legsIdx = huml.IndexOf("Legs:", StringComparison.Ordinal);
        var breedIdx = huml.IndexOf("Breed:", StringComparison.Ordinal);
        nameIdx.Should().BeLessThan(legsIdx);
        legsIdx.Should().BeLessThan(breedIdx);
    }

    // ── AC #2: new-shadowed property serialises exactly once, derived value ──

    [Fact]
    public void Shadowed_property_serialises_exactly_once()
    {
        var huml = HumlSerializer.Serialize(new DedupShadowDerived());
        CountOccurrences(huml, "Id:").Should().Be(1);
    }

    [Fact]
    public void Shadowed_property_emits_derived_value()
    {
        var huml = HumlSerializer.Serialize(new DedupShadowDerived());
        huml.Should().Contain("Id: 2");
    }

    // ── AC #3: round-trip for a hierarchy with overridden properties ──

    [Fact]
    public void Override_hierarchy_round_trips()
    {
        var original = new DedupDog { Name = "Fido", Breed = "Beagle", Legs = 3 };
        var huml = HumlSerializer.Serialize(original);

        var act = () => HumlSerializer.Deserialize<DedupDog>(huml);
        act.Should().NotThrow();

        var result = HumlSerializer.Deserialize<DedupDog>(huml);
        result.Name.Should().Be("Fido");
        result.Breed.Should().Be("Beagle");
        result.Legs.Should().Be(3);
    }

    [Fact]
    public void Shadowed_hierarchy_round_trips_with_derived_value()
    {
        var original = new DedupShadowDerived { Id = 42 };
        var huml = HumlSerializer.Serialize(original);

        var result = HumlSerializer.Deserialize<DedupShadowDerived>(huml);
        result.Id.Should().Be(42);
    }

    // ── AC #5: multi-level inheritance chains ──

    [Fact]
    public void Multi_level_override_chain_serialises_derived_most_exactly_once()
    {
        var huml = HumlSerializer.Serialize(new DedupLevel3());
        CountOccurrences(huml, "Tag:").Should().Be(1);
        huml.Should().Contain("Tag: \"l3\"");
    }

    [Fact]
    public void Multi_level_shadow_chain_serialises_derived_most_exactly_once()
    {
        var huml = HumlSerializer.Serialize(new DedupLevel3());
        CountOccurrences(huml, "Count:").Should().Be(1);
        huml.Should().Contain("Count: 30");
    }

    [Fact]
    public void Mid_level_of_chain_serialises_its_own_declarations_once()
    {
        var huml = HumlSerializer.Serialize(new DedupLevel2());
        CountOccurrences(huml, "Tag:").Should().Be(1);
        huml.Should().Contain("Tag: \"l2\"");
        CountOccurrences(huml, "Count:").Should().Be(1);
        huml.Should().Contain("Count: 20");
    }

    [Fact]
    public void Multi_level_chain_round_trips()
    {
        var original = new DedupLevel3 { Tag = "custom", Count = 99 };
        var huml = HumlSerializer.Serialize(original);

        var result = HumlSerializer.Deserialize<DedupLevel3>(huml);
        result.Tag.Should().Be("custom");
        result.Count.Should().Be(99);
    }

    // ── AC #4: reflection path and source-generated path produce identical output ──

    [Fact]
    public void Reflection_and_source_generated_output_are_identical_for_override_hierarchy()
    {
        var dog = new SGDedupDog { Name = "Parity", Legs = 6 };

        var reflection = HumlSerializer.Serialize(dog);
        var sourceGen = HumlSerializer.Serialize(
            dog, new HumlOptions { TypeInfoResolver = SGDedupContext.Default });

        sourceGen.Should().Be(reflection);
    }

    [Fact]
    public void Source_generated_path_emits_shadowed_property_with_derived_value()
    {
        var dog = new SGDedupDog();
        ((SGDedupAnimal)dog).Legs = 4;
        dog.Legs = 3;

        var huml = HumlSerializer.Serialize(
            dog, new HumlOptions { TypeInfoResolver = SGDedupContext.Default });

        CountOccurrences(huml, "Legs:").Should().Be(1);
        huml.Should().Contain("Legs: 3");
    }
}
