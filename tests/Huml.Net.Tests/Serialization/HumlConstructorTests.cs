using System.Reflection;
using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class HumlConstructorTests
{
    // ── Test DTOs ─────────────────────────────────────────────────────────────

    // CTOR-01 / CTOR-02: basic record — auto-selected single non-parameterless ctor
    private record Point(int X, int Y);

    // CTOR-02: class with a single non-parameterless ctor (auto-selected per D-02 priority 2)
    private class SingleCtorClass
    {
        public string Name { get; }
        public int Age { get; }
        public SingleCtorClass(string name, int age) { Name = name; Age = age; }
    }

    // CTOR-02 + CTOR-09: class with [HumlConstructor] on the parameterised ctor; parameterless also present
    private class AnnotatedCtorClass
    {
        public string Value { get; }
        [HumlConstructor]
        public AnnotatedCtorClass(string value) { Value = value; }
        public AnnotatedCtorClass() { Value = string.Empty; }
    }

    // CTOR-06: record with an optional parameter (HasDefaultValue = true, default 99)
    private record PointWithDefault(int X, int Y = 99);

    // CTOR-07 / CTOR-12: init-only properties on a parameterless-ctor class
    private class InitOnlyClass
    {
        public string? Name { get; init; }
        public int Count { get; init; }
    }

    // CTOR-08: record with an extra init-only property beyond the ctor parameters
    private record PersonWithExtra(string FirstName, string LastName)
    {
        public string? Nickname { get; init; }
    }

    // CTOR-09: two public non-parameterless ctors, no [HumlConstructor] → ambiguous
    private class AmbiguousCtorClass
    {
        public AmbiguousCtorClass(string name) { }
        public AmbiguousCtorClass(int id) { }
    }

    // CTOR-09: two ctors both carrying [HumlConstructor] → multi-annotated
    private class MultiAnnotatedCtorClass
    {
        [HumlConstructor]
        public MultiAnnotatedCtorClass(string a) { }
        [HumlConstructor]
        public MultiAnnotatedCtorClass(int b) { }
    }

    // CTOR-10: round-trip record via Huml facade
    private record ColorRecord(string Name, int Red, int Green, int Blue);

    // CTOR-11: record with camelCase parameter names matched via KebabCase policy
    private record PersonKebab(string firstName, string lastName);

    // ── Constructor: test isolation ───────────────────────────────────────────

    public HumlConstructorTests() { PropertyDescriptor.ClearCache(); }

    // ── CTOR-01: [HumlConstructor] attribute meta ─────────────────────────────

    [Fact]
    public void HumlConstructorAttribute_HasCorrectAttributeUsage()
    {
        var attr = typeof(HumlConstructorAttribute).GetCustomAttribute<AttributeUsageAttribute>()!;
        attr.ValidOn.Should().Be(AttributeTargets.Constructor);
        attr.AllowMultiple.Should().BeFalse();
        attr.Inherited.Should().BeFalse();
    }

    // ── CTOR-02: constructor selection cached in PropertyDescriptorCache ───────

    [Fact]
    public void GetSelectedConstructor_Record_ReturnsNonNullWithTwoParams()
    {
        var ctor = PropertyDescriptor.GetSelectedConstructor(typeof(Point));
        ctor.Should().NotBeNull();
        ctor!.GetParameters().Should().HaveCount(2);
    }

    [Fact]
    public void GetSelectedConstructor_AnnotatedCtorClass_ReturnsAnnotatedCtor()
    {
        var ctor = PropertyDescriptor.GetSelectedConstructor(typeof(AnnotatedCtorClass));
        ctor.Should().NotBeNull();
        ctor!.GetParameters().Should().HaveCount(1); // the [HumlConstructor]-annotated one, not the parameterless
    }

    [Fact]
    public void GetSelectedConstructor_SingleCtorClass_AutoSelectsNonParameterless()
    {
        var ctor = PropertyDescriptor.GetSelectedConstructor(typeof(SingleCtorClass));
        ctor.Should().NotBeNull();
        ctor!.GetParameters().Should().HaveCount(2);
    }

    // ── CTOR-03: non-public constructors are not selected (D-03) ──────────────
    // Verified implicitly: if private ctors were included the counts above would differ.
    // Parameterless ctors (length 0) are excluded from the non-parameterless list, so
    // a type with only one private non-parameterless ctor has no selectedConstructor.

    // ── CTOR-09: ambiguous constructor detection ──────────────────────────────

    [Fact]
    public void GetHasAmbiguousConstructors_AmbiguousClass_IsTrue()
    {
        PropertyDescriptor.GetHasAmbiguousConstructors(typeof(AmbiguousCtorClass)).Should().BeTrue();
        PropertyDescriptor.GetSelectedConstructor(typeof(AmbiguousCtorClass)).Should().BeNull();
    }

    [Fact]
    public void GetHasAmbiguousConstructors_MultiAnnotated_IsTrue()
    {
        PropertyDescriptor.GetHasAmbiguousConstructors(typeof(MultiAnnotatedCtorClass)).Should().BeTrue();
    }

    // ── CTOR-09c/d: ambiguous throws at deserialise time (RED until Plan 02) ──

    [Fact]
    public void Deserialize_AmbiguousCtors_ThrowsHumlDeserializeException()
    {
        var act = () => Huml.Deserialize<AmbiguousCtorClass>("X: 1");
        act.Should().Throw<HumlDeserializeException>().WithMessage("*multiple*");
    }

    [Fact]
    public void Deserialize_MultiAnnotatedCtors_ThrowsHumlDeserializeException()
    {
        var act = () => Huml.Deserialize<MultiAnnotatedCtorClass>("X: 1");
        act.Should().Throw<HumlDeserializeException>().WithMessage("*[HumlConstructor]*");
    }

    // ── CTOR-04 / CTOR-11: naming-policy-aware parameter matching ─────────────

    [Fact]
    public void Deserialize_KebabCasePolicy_BindsKebabKeysToParameterNames()
    {
        const string huml = """
            first-name: "Alice"
            last-name: "Smith"
            """;
        var options = new HumlOptions { PropertyNamingPolicy = HumlNamingPolicy.KebabCase };
        var result = Huml.Deserialize<PersonKebab>(huml, options);
        result.firstName.Should().Be("Alice");
        result.lastName.Should().Be("Smith");
    }

    // ── CTOR-05: missing required parameter ───────────────────────────────────

    [Fact]
    public void Deserialize_MissingRequiredParam_ThrowsHumlDeserializeExceptionWithParamName()
    {
        var act = () => Huml.Deserialize<Point>("X: 3"); // Y is missing
        act.Should().Throw<HumlDeserializeException>().WithMessage("*Y*");
    }

    // ── CTOR-06: optional parameter uses declared default ────────────────────

    [Fact]
    public void Deserialize_OptionalParamAbsent_UsesDefaultValue()
    {
        var result = Huml.Deserialize<PointWithDefault>("X: 5");
        result.X.Should().Be(5);
        result.Y.Should().Be(99); // default from PointWithDefault(int X, int Y = 99)
    }

    // ── CTOR-07 / CTOR-12: init-only on parameterless-ctor type ──────────────

    [Fact]
    public void Deserialize_InitOnlyProperties_SetViaReflectionSuccessfully()
    {
        const string huml = """
            Name: "test"
            Count: 7
            """;
        var result = Huml.Deserialize<InitOnlyClass>(huml);
        result.Name.Should().Be("test");
        result.Count.Should().Be(7);
    }

    // ── CTOR-08: alreadyBound — post-construction loop skips ctor-bound keys ──

    [Fact]
    public void Deserialize_RecordWithExtraInitProp_SetsAllPropertiesOnce()
    {
        const string huml = """
            FirstName: "Bob"
            LastName: "Jones"
            Nickname: "BJ"
            """;
        var result = Huml.Deserialize<PersonWithExtra>(huml);
        result.FirstName.Should().Be("Bob");
        result.LastName.Should().Be("Jones");
        result.Nickname.Should().Be("BJ");
    }

    // ── CTOR-10: record round-trip ────────────────────────────────────────────

    [Fact]
    public void Deserialize_Record_RoundTripProducesValueEqualInstance()
    {
        var original = new ColorRecord("Red", 255, 0, 0);
        var huml = Huml.Serialize(original);
        var result = Huml.Deserialize<ColorRecord>(huml);
        result.Should().Be(original);
    }
}
