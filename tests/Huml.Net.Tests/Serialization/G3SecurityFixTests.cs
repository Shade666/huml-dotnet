using System.Globalization;
using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

/// <summary>
/// Regression tests for the critical/high findings from the G3.2 adversarial review
/// (docs/internals/g3-security-review.md). Each test reproduces a confirmed defect.
/// </summary>
public class G3SecurityFixTests
{
    // ── C1: cyclic object graph must not crash the process (StackOverflow) ──

    public sealed class Node
    {
        public string Name { get; set; } = "";
        public Node? Next { get; set; }
    }

    [Fact]
    public void Cyclic_object_graph_throws_instead_of_crashing()
    {
        var a = new Node { Name = "a" };
        var b = new Node { Name = "b" };
        a.Next = b;
        b.Next = a; // cycle

        var act = () => Huml.Serialize(a);
        act.Should().Throw<HumlSerializeException>();
    }

    [Fact]
    public void Deeply_nested_graph_beyond_limit_throws_serialize_exception()
    {
        var head = new Node { Name = "0" };
        var cur = head;
        for (int i = 1; i < 2000; i++)
        {
            cur.Next = new Node { Name = i.ToString(CultureInfo.InvariantCulture) };
            cur = cur.Next;
        }

        var act = () => Huml.Serialize(head);
        act.Should().Throw<HumlSerializeException>();
    }

    // ── H2/H3/H4: constructor and setter exceptions surface as HumlDeserializeException ──

    public sealed class CtorValidates
    {
        public CtorValidates(int age)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(age);
            Age = age;
        }

        public int Age { get; }
    }

    [Fact]
    public void Throwing_parameterised_constructor_surfaces_as_deserialize_exception()
    {
        var act = () => Huml.Deserialize<CtorValidates>("age: -5", HumlOptions.LatestSupported);
        act.Should().Throw<HumlDeserializeException>();
    }

    public sealed class ParamlessThrows
    {
        public ParamlessThrows() => throw new InvalidOperationException("no");
        public int X { get; set; }
    }

    [Fact]
    public void Throwing_parameterless_constructor_surfaces_as_deserialize_exception()
    {
        var act = () => Huml.Deserialize<ParamlessThrows>("x: 1", HumlOptions.LatestSupported);
        act.Should().Throw<HumlDeserializeException>();
    }

    public sealed class SetterThrows
    {
        private int _x;
        public int X
        {
            get => _x;
            set => _x = value < 0 ? throw new InvalidOperationException("bad") : value;
        }
    }

    [Fact]
    public void Throwing_property_setter_surfaces_as_deserialize_exception()
    {
        var act = () => Huml.Deserialize<SetterThrows>("X: -1", HumlOptions.LatestSupported);
        act.Should().Throw<HumlDeserializeException>();
    }

    // ── H5: an empty POCO as a mapping value must emit a re-parseable form ──

    public sealed class Empty;

    public sealed class HasEmpty
    {
        public Empty Inner { get; set; } = new();
        public string Name { get; set; } = "x";
    }

    [Fact]
    public void Empty_poco_property_value_round_trips()
    {
        var huml = Huml.Serialize(new HasEmpty());
        var act = () => Huml.Parse(huml, HumlOptions.Default);
        act.Should().NotThrow(because: $"empty POCO values must emit ':: {{}}', got:\n{huml}");
    }

    // ── H12: a throwing property getter surfaces as HumlSerializeException ──

    public sealed class GetterThrows
    {
        public string Ok { get; set; } = "fine";
        public string Bad => Ok.Length > 0 ? throw new InvalidOperationException("boom") : "";
    }

    [Fact]
    public void Throwing_property_getter_surfaces_as_serialize_exception()
    {
        var act = () => Huml.Serialize(new GetterThrows());
        act.Should().Throw<HumlSerializeException>();
    }

    // ── H11: dictionary keys must format with invariant culture ──

    [Fact]
    public void Dictionary_with_numeric_keys_uses_invariant_formatting()
    {
        var dict = new Dictionary<double, string> { [1.5] = "a" };

        // Serialise under a culture that uses ',' as the decimal separator.
        var original = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            var huml = Huml.Serialize(dict);
            huml.Should().Contain("1.5", because: "numeric keys must use invariant formatting regardless of thread culture");
            huml.Should().NotContain("1,5");
        }
        finally
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = original;
        }
    }

    // ── M9: enum with case-insensitively colliding member names surfaces cleanly ──

#pragma warning disable CA1708 // intentional case-only collision under test
    public enum Collide { Value, value }
#pragma warning restore CA1708

    public sealed class HasCollidingEnum
    {
        public Collide E { get; set; }
    }

    [Fact]
    public void Enum_with_case_insensitive_collision_does_not_leak_raw_exception()
    {
        var act = () => Huml.Deserialize<HasCollidingEnum>("E: \"Value\"", HumlOptions.LatestSupported);
        act.Should().NotThrow<InvalidOperationException>(
            because: "a colliding-name enum must not leak a raw InvalidOperationException");
    }
}
