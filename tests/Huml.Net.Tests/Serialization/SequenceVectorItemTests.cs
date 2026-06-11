using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

/// <summary>
/// Regression tests for the G3.3 finding that vector items inside multi-line lists were
/// serialised as a bare dash with trailing whitespace (<c>"- \n"</c>) followed by a
/// key-value block — a form both the HUML grammar and our own parser reject. Per the
/// grammar (<c>multiline_list_item = "- " MULTILINE_VECTOR_START …</c>) and go-huml's
/// encoder, a vector list item is emitted as <c>- ::</c> with the block one level deeper.
/// </summary>
public class SequenceVectorItemTests
{
    public sealed record Child(string Name, int Value);

    public sealed class Holder
    {
        public IList<Child> Items { get; init; } = [];
    }

    [Fact]
    public void List_of_objects_round_trips()
    {
        var holder = new Holder { Items = [new Child("a", 1), new Child("b", 2)] };

        var huml = HumlSerializer.Serialize(holder);
        var restored = HumlSerializer.Deserialize<Holder>(huml, HumlOptions.Default);

        restored.Items.Should().HaveCount(2);
        restored.Items[0].Should().Be(new Child("a", 1));
        restored.Items[1].Should().Be(new Child("b", 2));
    }

    [Fact]
    public void List_of_objects_serialises_items_with_vector_indicator()
    {
        var holder = new Holder { Items = [new Child("a", 1)] };

        var huml = HumlSerializer.Serialize(holder);

        huml.Should().Contain("- ::", because: "vector list items use the '- ::' form");
        foreach (var line in huml.Split('\n'))
            line.Should().NotMatchRegex(@" $", because: "no line may carry trailing whitespace");
    }

    [Fact]
    public void List_of_dictionaries_round_trips()
    {
        var value = new Dictionary<string, IList<Dictionary<string, int>>>
        {
            ["outer"] = [new Dictionary<string, int> { ["x"] = 1 }, new Dictionary<string, int> { ["y"] = 2 }],
        };

        var huml = HumlSerializer.Serialize(value);
        var restored = HumlSerializer.Deserialize<Dictionary<string, IList<Dictionary<string, int>>>>(huml, HumlOptions.Default);

        restored["outer"].Should().HaveCount(2);
        restored["outer"][0]["x"].Should().Be(1);
        restored["outer"][1]["y"].Should().Be(2);
    }

    [Fact]
    public void List_of_lists_round_trips()
    {
        var holder = new ListsHolder { Rows = [[1, 2], [3]] };

        var huml = HumlSerializer.Serialize(holder);
        var restored = HumlSerializer.Deserialize<ListsHolder>(huml, HumlOptions.Default);

        restored.Rows.Should().HaveCount(2);
        restored.Rows[0].Should().Equal(1, 2);
        restored.Rows[1].Should().Equal(3);
    }

    public sealed class ListsHolder
    {
        public IList<IList<int>> Rows { get; init; } = [];
    }

    [Fact]
    public void Empty_nested_collection_items_use_inline_empty_signifiers()
    {
        var holder = new ListsHolder { Rows = [[]] };

        var huml = HumlSerializer.Serialize(holder);

        huml.Should().Contain("- :: []", because: "an empty vector item must not produce an ambiguous bare '::'");
        var act = () => HumlSerializer.Deserialize<ListsHolder>(huml, HumlOptions.Default);
        act.Should().NotThrow();
    }

    [Fact]
    public void Empty_poco_list_item_uses_inline_empty_dict()
    {
        var holder = new EmptyPocoHolder { Items = [new EmptyPoco()] };

        var huml = HumlSerializer.Serialize(holder);

        huml.Should().Contain("- :: {}", because: "a POCO with no serialisable members is an empty dict item");
        var act = () => HumlSerializer.Parse(huml, HumlOptions.Default);
        act.Should().NotThrow();
    }

    public sealed class EmptyPoco;

    public sealed class EmptyPocoHolder
    {
        public IList<EmptyPoco> Items { get; init; } = [];
    }

    [Fact]
    public void Ast_document_with_vector_list_items_round_trips()
    {
        const string input = "items::\n  - ::\n    a: 1\n  - ::\n    a: 2";
        var doc = HumlSerializer.Parse(input, HumlOptions.LatestSupported);

        var emitted = HumlSerializer.Serialize(doc);
        var act = () => HumlSerializer.Parse(emitted, HumlOptions.Default);

        act.Should().NotThrow(because: $"re-emitted AST must be valid HUML (got: {emitted})");
    }
}
