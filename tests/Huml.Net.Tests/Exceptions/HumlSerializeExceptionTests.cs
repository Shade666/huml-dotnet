using AwesomeAssertions;
using Huml.Net.Exceptions;
using Xunit;

namespace Huml.Net.Tests.Exceptions;

public class HumlSerializeExceptionTests
{
    [Fact]
    public void Is_sealed_exception()
    {
        typeof(HumlSerializeException).IsSealed.Should().BeTrue();
        new HumlSerializeException("err").Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Message_ctor_sets_message()
    {
        new HumlSerializeException("something went wrong").Message
            .Should().Be("something went wrong");
    }

    [Fact]
    public void Message_and_inner_ctor_sets_message_and_inner_exception()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new HumlSerializeException("outer", inner);

        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Does_not_have_serialization_info_constructor()
    {
        // Binary serialization constructor must NOT be present (SYSLIB0051 pattern)
        var ctors = typeof(HumlSerializeException).GetConstructors(
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        foreach (var ctor in ctors)
        {
            var parameters = ctor.GetParameters();
            bool hasSerializationInfo = false;
            foreach (var p in parameters)
                if (string.Equals(p.ParameterType.FullName, "System.Runtime.Serialization.SerializationInfo", StringComparison.Ordinal))
                    hasSerializationInfo = true;
            hasSerializationInfo.Should().BeFalse("binary serialization constructor must not be present");
        }
    }
}
