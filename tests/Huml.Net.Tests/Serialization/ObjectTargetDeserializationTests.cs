using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

/// <summary>
/// Regression tests for the G3.3 silent-data-loss finding: nested mappings and sequences
/// deserialised into <c>object</c>-typed slots produced a bare <c>new object()</c> /
/// content-less value, discarding the document content without any exception. Mirrors
/// System.Text.Json's behaviour of materialising a usable value for <c>object</c> targets:
/// mappings become <c>Dictionary&lt;string, object?&gt;</c>, sequences become
/// <c>List&lt;object?&gt;</c>, scalars box their natural CLR value.
/// </summary>
public class ObjectTargetDeserializationTests
{
    [Fact]
    public void Nested_mapping_into_object_slot_becomes_dictionary()
    {
        var map = HumlSerializer.Deserialize<Dictionary<string, object?>>(
            "dict::\n  key1: \"v1\"\n  key2: 2", HumlOptions.LatestSupported);

        var nested = map["dict"].Should().BeOfType<Dictionary<string, object?>>().Subject;
        nested["key1"].Should().Be("v1");
        nested["key2"].Should().Be(2L);
    }

    [Fact]
    public void Nested_sequence_into_object_slot_becomes_list()
    {
        var map = HumlSerializer.Deserialize<Dictionary<string, object?>>(
            "list::\n  - 1\n  - \"two\"", HumlOptions.LatestSupported);

        var nested = map["list"].Should().BeOfType<List<object?>>().Subject;
        nested.Should().Equal(1L, "two");
    }

    [Theory]
    [InlineData("key: 1", 1L)]
    [InlineData("key: \"s\"", "s")]
    [InlineData("key: true", true)]
    [InlineData("key: 1.5", 1.5)]
    public void Scalar_into_object_slot_boxes_natural_value(string input, object expected)
    {
        var map = HumlSerializer.Deserialize<Dictionary<string, object?>>(input, HumlOptions.LatestSupported);
        map["key"].Should().Be(expected);
    }

    [Fact]
    public void Object_dictionary_round_trips_through_serialiser()
    {
        const string input = "dict::\n  key1: \"v1\"\n  key2: 2";
        var map = HumlSerializer.Deserialize<Dictionary<string, object?>>(input, HumlOptions.LatestSupported);

        var emitted = HumlSerializer.Serialize(map);
        var restored = HumlSerializer.Deserialize<Dictionary<string, object?>>(emitted, HumlOptions.Default);

        var nested = restored["dict"].Should().BeOfType<Dictionary<string, object?>>().Subject;
        nested["key1"].Should().Be("v1");
        nested["key2"].Should().Be(2L);
    }

    [Fact]
    public void Root_scalar_into_dictionary_target_throws_instead_of_silently_yielding_empty()
    {
        var act = () => HumlSerializer.Deserialize<Dictionary<string, object?>>("123", HumlOptions.LatestSupported);
        act.Should().Throw<HumlDeserializeException>();
    }

    [Fact]
    public void Root_scalar_into_object_target_unwraps_to_boxed_value()
    {
        var value = HumlSerializer.Deserialize<object>("123", HumlOptions.LatestSupported);
        value.Should().Be(123L);
    }

    [Fact]
    public void Root_scalar_into_typed_target_unwraps()
    {
        HumlSerializer.Deserialize<long>("123", HumlOptions.LatestSupported).Should().Be(123L);
        HumlSerializer.Deserialize<string>("\"hi\"", HumlOptions.LatestSupported).Should().Be("hi");
    }

    [Fact]
    public void Root_sequence_into_typed_list_unwraps()
    {
        var list = HumlSerializer.Deserialize<List<long>>("1, 2, 3", HumlOptions.LatestSupported);
        list.Should().Equal(1L, 2L, 3L);
    }
}
