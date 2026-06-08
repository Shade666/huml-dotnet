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
        ex.Column.Should().BeNull();
    }

    // These tests exercise the [Obsolete] 3-arg constructor to verify existing behaviour
    // is preserved. Suppress CS0618 for the call sites — the constructor is intentionally
    // still present and correct; the [Obsolete] attribute guides new callers to the 4-arg form.
#pragma warning disable CS0618
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
#pragma warning restore CS0618

    [Fact]
    public void Four_param_ctor_sets_column_property()
    {
        var ex = new HumlDeserializeException("type mismatch", "myKey", 42, 7);
        ex.Column.Should().Be(7);
    }

    [Fact]
    public void Four_param_ctor_sets_line_and_key()
    {
        var ex = new HumlDeserializeException("type mismatch", "myKey", 42, 7);
        ex.Line.Should().Be(42);
        ex.Key.Should().Be("myKey");
    }

    [Fact]
    public void Four_param_ctor_message_contains_line_and_column()
    {
        var ex = new HumlDeserializeException("type mismatch", "myKey", 42, 7);
        ex.Message.Should().Contain("[line 42, col 7]");
    }

    [Fact]
    public void Four_param_ctor_null_key_emits_message_as_is()
    {
        var ex = new HumlDeserializeException("bare error", null, 1, 0);
        ex.Message.Should().Be("bare error");
        ex.Column.Should().Be(0);
    }
}
