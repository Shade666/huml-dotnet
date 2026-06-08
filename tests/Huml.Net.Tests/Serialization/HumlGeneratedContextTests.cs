using AwesomeAssertions;
using Huml.Net.Serialization;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public sealed class HumlGeneratedContextTests
{
    private sealed class SomeDto
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class SomeDtoTypeInfo : HumlTypeInfo<SomeDto> { }

    private sealed class TestGeneratedContext : HumlGeneratedContext
    {
        private readonly HumlTypeInfo<SomeDto> _someDtoInfo = new SomeDtoTypeInfo();

        public override HumlTypeInfo? GetTypeInfo(Type type, HumlOptions options)
        {
            if (type == typeof(SomeDto)) return _someDtoInfo;
            return null;
        }
    }

    [Fact]
    public void GC01_subclass_GetTypeInfo_dispatches_registered_type()
    {
        var ctx = new TestGeneratedContext();
        var info = ctx.GetTypeInfo(typeof(SomeDto), HumlOptions.LatestSupported);
        info.Should().NotBeNull();
        info.Should().BeOfType<SomeDtoTypeInfo>();
    }

    [Fact]
    public void GC02_GetTypeInfoT_returns_typed_result()
    {
        var ctx = new TestGeneratedContext();
        var info = ctx.GetTypeInfo<SomeDto>();
        info.Should().NotBeNull();
        info.Should().BeOfType<SomeDtoTypeInfo>();
    }

    [Fact]
    public void GC03_GetTypeInfo_returns_null_for_unregistered_type()
    {
        var ctx = new TestGeneratedContext();
        var info = ctx.GetTypeInfo(typeof(string), HumlOptions.LatestSupported);
        info.Should().BeNull();
    }

    [Fact]
    public void GC04_context_is_accepted_as_TypeInfoResolver_by_HumlOptions()
    {
        var ctx = new TestGeneratedContext();
        var options = new HumlOptions { TypeInfoResolver = ctx };
        options.TypeInfoResolver.Should().NotBeNull();
        options.TypeInfoResolver.Should().BeAssignableTo<HumlGeneratedContext>();
    }
}
