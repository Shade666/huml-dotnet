using AwesomeAssertions;
using Huml.Net.Exceptions;
using Xunit;

namespace Huml.Net.Tests.Exceptions;

public class HumlDeserializeExceptionTests
{
    [Fact]
    public void Is_sealed_exception()
    {
        typeof(HumlDeserializeException).IsSealed.Should().BeTrue();
        new HumlDeserializeException("err").Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Message_ctor_sets_message()
    {
        new HumlDeserializeException("something went wrong").Message
            .Should().Be("something went wrong");
    }

    [Fact]
    public void Message_ctor_leaves_key_and_line_null()
    {
        var ex = new HumlDeserializeException("msg");
        ex.Key.Should().BeNull();
        ex.Line.Should().BeNull();
    }

    [Fact]
    public void Diagnostic_ctor_sets_key_property()
    {
        var ex = new HumlDeserializeException("type mismatch", "myKey", 42);
        ex.Key.Should().Be("myKey");
    }

    [Fact]
    public void Diagnostic_ctor_sets_line_property()
    {
        var ex = new HumlDeserializeException("type mismatch", "myKey", 42);
        ex.Line.Should().Be(42);
    }

    [Fact]
    public void Diagnostic_ctor_message_contains_line_number()
    {
        var ex = new HumlDeserializeException("type mismatch", "myKey", 42);
        ex.Message.Should().Contain("[line 42]");
    }

    [Fact]
    public void Diagnostic_ctor_message_contains_key()
    {
        var ex = new HumlDeserializeException("type mismatch", "myKey", 42);
        ex.Message.Should().Contain("'myKey'");
    }

    [Fact]
    public void Diagnostic_ctor_message_contains_original_message()
    {
        var ex = new HumlDeserializeException("type mismatch", "myKey", 42);
        ex.Message.Should().Contain("type mismatch");
    }
}
