using AwesomeAssertions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class SourceGeneratorIntegrationTests
{
    private static readonly HumlOptions SGOptions = new() { TypeInfoResolver = SGTestContext.Default };

    [Fact]
    public void SG01_Default_singleton_is_not_null()
        => SGTestContext.Default.Should().NotBeNull();

    [Fact]
    public void SG02_typed_property_for_registered_type_exists_and_has_properties()
    {
        var typeInfo = SGTestContext.Default.SGDto;
        typeInfo.Should().NotBeNull();
        typeInfo.Properties.Should().NotBeNull();
        typeInfo.Properties!.Count.Should().Be(2);
    }

    [Fact]
    public void SG03_GetTypeInfo_returns_type_info_with_non_null_properties()
    {
        var info = SGTestContext.Default.GetTypeInfo(typeof(SGDto), HumlOptions.LatestSupported);
        info.Should().NotBeNull();
        info!.Properties.Should().NotBeNull();
    }

    [Fact]
    public void SG04_GetTypeInfo_returns_null_for_unregistered_type()
    {
        var info = SGTestContext.Default.GetTypeInfo(typeof(string), HumlOptions.LatestSupported);
        info.Should().BeNull();
    }

    [Fact]
    public void SG05_round_trip_via_source_generated_context()
    {
        var original = new SGDto { Name = "Alice", Age = 30 };
        var huml = HumlSerializer.Serialize(original, SGOptions);
        var result = HumlSerializer.Deserialize<SGDto>(huml, SGOptions);
        result.Name.Should().Be("Alice");
        result.Age.Should().Be(30);
    }

    [Fact]
    public void SG06_source_gen_delegates_get_and_set_properties()
    {
        var typeInfo = SGTestContext.Default.SGDto;
        typeInfo.Properties.Should().NotBeNull();

        var dto = new SGDto { Name = "Bob", Age = 25 };

        var nameProp = typeInfo.Properties![0]; // Name
        var ageProp = typeInfo.Properties![1];  // Age

        nameProp.Name.Should().Be("Name");
        nameProp.Get!(dto).Should().Be("Bob");

        ageProp.Name.Should().Be("Age");
        ageProp.Get!(dto).Should().Be(25);

        nameProp.Set!(dto, "Charlie");
        ageProp.Set!(dto, 40);

        dto.Name.Should().Be("Charlie");
        dto.Age.Should().Be(40);
    }

    [Fact]
    public void SG07_CreateObject_factory_creates_instance()
    {
        var typeInfo = SGTestContext.Default.SGDto;
        typeInfo.CreateObject.Should().NotBeNull();
        var instance = typeInfo.CreateObject!();
        instance.Should().NotBeNull();
        instance.Should().BeOfType<SGDto>();
    }
}
