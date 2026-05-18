using System.Reflection;
using AwesomeAssertions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class DefaultIgnoreConditionTests
{
    // ── Test DTOs ─────────────────────────────────────────────────────────────

    [HumlIgnoreDefaults]
    private class IgnoreDefaultsDto
    {
        public int Count { get; set; }
        public string? Tag { get; set; }
        public bool Active { get; set; }
        public double Score { get; set; }
    }

    [HumlIgnoreDefaults]
    private class IgnoreDefaultsWithOverrideDto
    {
        [HumlProperty(OmitIfDefault = true)]
        public int ExplicitOmit { get; set; }

        public int ClassOmit { get; set; }

        public string? Name { get; set; }
    }

    private class PlainDto
    {
        public int Count { get; set; }
        public string? Tag { get; set; }
        public bool Active { get; set; }
    }

    [HumlIgnoreDefaults]
    private class BaseIgnoreDto
    {
        public int BaseCount { get; set; }
    }

    private class DerivedIgnoreDto : BaseIgnoreDto
    {
        public int DerivedCount { get; set; }
    }

    private class BaseNoIgnoreDto
    {
        public int BaseCount { get; set; }
    }

    [HumlIgnoreDefaults]
    private class DerivedWithIgnoreDto : BaseNoIgnoreDto
    {
        public int DerivedCount { get; set; }
    }

    // ── Constructor ───────────────────────────────────────────────────────────

    public DefaultIgnoreConditionTests()
    {
        // Clear the descriptor cache before each test for isolation.
        PropertyDescriptor.ClearCache();
    }

    // ── IGN-01: HumlIgnoreCondition enum values and bitmask contract ──────────

    [Fact]
    public void HumlIgnoreCondition_Never_IsZero()
    {
        ((int)HumlIgnoreCondition.Never).Should().Be(0);
    }

    [Fact]
    public void HumlIgnoreCondition_WhenWritingNull_IsOne()
    {
        ((int)HumlIgnoreCondition.WhenWritingNull).Should().Be(1);
    }

    [Fact]
    public void HumlIgnoreCondition_WhenWritingDefault_IsTwo()
    {
        ((int)HumlIgnoreCondition.WhenWritingDefault).Should().Be(2);
    }

    [Fact]
    public void HumlIgnoreCondition_Always_IsThree()
    {
        ((int)HumlIgnoreCondition.Always).Should().Be(3);
    }

    [Fact]
    public void HumlIgnoreCondition_Always_EqualsBitwiseOrOfNullAndDefault()
    {
        var combined = (int)HumlIgnoreCondition.WhenWritingNull | (int)HumlIgnoreCondition.WhenWritingDefault;
        combined.Should().Be((int)HumlIgnoreCondition.Always);
    }

    // ── IGN-02: HumlIgnoreDefaultsAttribute usage constraints ─────────────────

    [Fact]
    public void HumlIgnoreDefaultsAttribute_ValidOn_HasClassAndStructFlags()
    {
        var usage = typeof(HumlIgnoreDefaultsAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>()!;

        usage.ValidOn.HasFlag(AttributeTargets.Class).Should().BeTrue();
        usage.ValidOn.HasFlag(AttributeTargets.Struct).Should().BeTrue();
    }

    [Fact]
    public void HumlIgnoreDefaultsAttribute_Inherited_IsTrue()
    {
        var usage = typeof(HumlIgnoreDefaultsAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>()!;

        usage.Inherited.Should().BeTrue();
    }

    [Fact]
    public void HumlIgnoreDefaultsAttribute_AllowMultiple_IsFalse()
    {
        var usage = typeof(HumlIgnoreDefaultsAttribute)
            .GetCustomAttribute<AttributeUsageAttribute>()!;

        usage.AllowMultiple.Should().BeFalse();
    }

    // ── IGN-03: PropertyDescriptor.ClassIgnoresDefaults ───────────────────────

    [Fact]
    public void ClassIgnoresDefaults_IsTrue_WhenTypeHasHumlIgnoreDefaultsAttribute()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(IgnoreDefaultsDto));
        var countDesc = Array.Find(descriptors, d => string.Equals(d.HumlKey, "Count", StringComparison.Ordinal))!;

        countDesc.ClassIgnoresDefaults.Should().BeTrue();
    }

    [Fact]
    public void ClassIgnoresDefaults_IsFalse_WhenTypeHasNoAttribute()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(PlainDto));
        var countDesc = Array.Find(descriptors, d => string.Equals(d.HumlKey, "Count", StringComparison.Ordinal))!;

        countDesc.ClassIgnoresDefaults.Should().BeFalse();
    }

    // ── IGN-04: PropertyDescriptor.DefaultValue unconditional computation ──────

    [Fact]
    public void DefaultValue_ForIntProperty_IsBoxedZero_EvenWithoutOmitIfDefault()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(PlainDto));
        var countDesc = Array.Find(descriptors, d => string.Equals(d.HumlKey, "Count", StringComparison.Ordinal))!;

        countDesc.OmitIfDefault.Should().BeFalse(); // confirm OmitIfDefault is NOT set
        countDesc.DefaultValue.Should().Be((object)0);
    }

    [Fact]
    public void DefaultValue_ForBoolProperty_IsBoxedFalse_EvenWithoutOmitIfDefault()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(PlainDto));
        var activeDesc = Array.Find(descriptors, d => string.Equals(d.HumlKey, "Active", StringComparison.Ordinal))!;

        activeDesc.OmitIfDefault.Should().BeFalse();
        activeDesc.DefaultValue.Should().Be((object)false);
    }

    [Fact]
    public void DefaultValue_ForNullableStringProperty_IsNull()
    {
        var descriptors = PropertyDescriptor.GetDescriptors(typeof(PlainDto));
        var tagDesc = Array.Find(descriptors, d => string.Equals(d.HumlKey, "Tag", StringComparison.Ordinal))!;

        tagDesc.DefaultValue.Should().BeNull();
    }

    // ── IGN-05: [HumlIgnoreDefaults] attribute suppresses default-valued properties ──

    [Fact]
    public void HumlIgnoreDefaults_SkipsAllDefaultProperties_WhenAllAtDefault()
    {
        var dto = new IgnoreDefaultsDto(); // Count=0, Tag=null, Active=false, Score=0.0
        var result = Huml.Serialize(dto, HumlOptions.Default);

        result.Should().NotContain("Count:");
        result.Should().NotContain("Tag:");
        result.Should().NotContain("Active:");
        result.Should().NotContain("Score:");
    }

    [Fact]
    public void HumlIgnoreDefaults_EmitsNonDefaultInt_WhenCountIsNonZero()
    {
        var dto = new IgnoreDefaultsDto { Count = 5 };
        var result = Huml.Serialize(dto, HumlOptions.Default);

        result.Should().Contain("Count: 5");
    }

    [Fact]
    public void HumlIgnoreDefaults_EmitsNonDefaultString_WhenTagIsSet()
    {
        var dto = new IgnoreDefaultsDto { Tag = "hello" };
        var result = Huml.Serialize(dto, HumlOptions.Default);

        result.Should().Contain("Tag:");
        result.Should().Contain("hello");
    }

    // ── IGN-06: DefaultIgnoreCondition = WhenWritingDefault ───────────────────

    [Fact]
    public void WhenWritingDefault_OmitsIntProperty_WhenValueIsZero()
    {
        var dto = new PlainDto { Count = 0 };
        var options = new HumlOptions { DefaultIgnoreCondition = HumlIgnoreCondition.WhenWritingDefault };
        var result = Huml.Serialize(dto, options);

        result.Should().NotContain("Count:");
    }

    [Fact]
    public void WhenWritingDefault_OmitsStringProperty_WhenValueIsNull()
    {
        var dto = new PlainDto { Tag = null };
        var options = new HumlOptions { DefaultIgnoreCondition = HumlIgnoreCondition.WhenWritingDefault };
        var result = Huml.Serialize(dto, options);

        result.Should().NotContain("Tag:");
    }

    [Fact]
    public void WhenWritingDefault_EmitsIntProperty_WhenValueIsNonZero()
    {
        var dto = new PlainDto { Count = 5 };
        var options = new HumlOptions { DefaultIgnoreCondition = HumlIgnoreCondition.WhenWritingDefault };
        var result = Huml.Serialize(dto, options);

        result.Should().Contain("Count: 5");
    }

    // ── IGN-07: DefaultIgnoreCondition = WhenWritingNull ──────────────────────

    [Fact]
    public void WhenWritingNull_OmitsNullStringProperty()
    {
        var dto = new PlainDto { Tag = null };
        var options = new HumlOptions { DefaultIgnoreCondition = HumlIgnoreCondition.WhenWritingNull };
        var result = Huml.Serialize(dto, options);

        result.Should().NotContain("Tag:");
    }

    [Fact]
    public void WhenWritingNull_StillEmitsZeroValueIntProperty()
    {
        var dto = new PlainDto { Count = 0 };
        var options = new HumlOptions { DefaultIgnoreCondition = HumlIgnoreCondition.WhenWritingNull };
        var result = Huml.Serialize(dto, options);

        result.Should().Contain("Count: 0");
    }

    // ── IGN-08: DefaultIgnoreCondition = Always ────────────────────────────────

    [Fact]
    public void Always_OmitsEveryProperty_RegardlessOfValue()
    {
        var dto = new PlainDto { Count = 42, Tag = "x", Active = true };
        var options = new HumlOptions { DefaultIgnoreCondition = HumlIgnoreCondition.Always };
        var result = Huml.Serialize(dto, options);

        result.Should().NotContain("Count:");
        result.Should().NotContain("Tag:");
        result.Should().NotContain("Active:");
    }

    // ── IGN-09: DefaultIgnoreCondition = Never preserves existing behaviour ────

    [Fact]
    public void Never_EmitsAllProperties_IncludingDefaultValues()
    {
        var dto = new PlainDto { Count = 0 };
        var result = Huml.Serialize(dto, HumlOptions.Default);

        result.Should().Contain("Count: 0");
    }

    // ── IGN-10: Precedence chain (per-property > class-level > global) ─────────

    [Fact]
    public void Precedence_PerPropertyOmitIfDefault_FiresForExplicitOmitProperty()
    {
        // ExplicitOmit=0 should be omitted by per-property [HumlProperty(OmitIfDefault=true)]
        // ClassOmit=0 should be omitted by class-level [HumlIgnoreDefaults]
        var dto = new IgnoreDefaultsWithOverrideDto { ExplicitOmit = 0, ClassOmit = 0 };
        var result = Huml.Serialize(dto, HumlOptions.Default);

        result.Should().NotContain("ExplicitOmit:");
        result.Should().NotContain("ClassOmit:");
    }

    [Fact]
    public void Precedence_PerPropertyOmitIfDefault_DoesNotFireOnNonDefault()
    {
        // ExplicitOmit=5 is non-default — per-property omit does NOT fire
        // ClassOmit=0 still omitted by class-level
        var dto = new IgnoreDefaultsWithOverrideDto { ExplicitOmit = 5, ClassOmit = 0 };
        var result = Huml.Serialize(dto, HumlOptions.Default);

        result.Should().Contain("ExplicitOmit: 5");
        result.Should().NotContain("ClassOmit:");
    }

    // ── IGN-11: [HumlIgnoreDefaults] inheritance — attribute on base propagates down ──

    [Fact]
    public void HumlIgnoreDefaults_InheritedFromBase_SuppressesBothBaseAndDerivedProperties()
    {
        // BaseIgnoreDto has [HumlIgnoreDefaults]; DerivedIgnoreDto inherits it.
        // Both BaseCount and DerivedCount are at their CLR default (0).
        var dto = new DerivedIgnoreDto { BaseCount = 0, DerivedCount = 0 };
        var result = Huml.Serialize(dto, HumlOptions.Default);

        result.Should().NotContain("BaseCount:");
        result.Should().NotContain("DerivedCount:");
    }

    // ── IGN-12: [HumlIgnoreDefaults] only on derived — base properties still emitted ──

    [Fact]
    public void HumlIgnoreDefaults_OnlyOnDerived_DoesNotSuppressBaseProperties()
    {
        // BaseNoIgnoreDto has NO attribute; DerivedWithIgnoreDto adds [HumlIgnoreDefaults].
        // BaseCount declared on undecorated BaseNoIgnoreDto should be emitted.
        // DerivedCount declared on decorated DerivedWithIgnoreDto should be suppressed.
        var dto = new DerivedWithIgnoreDto { BaseCount = 0, DerivedCount = 0 };
        var result = Huml.Serialize(dto, HumlOptions.Default);

        result.Should().Contain("BaseCount:");
        result.Should().NotContain("DerivedCount:");
    }
}
