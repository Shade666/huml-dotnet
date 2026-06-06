using Huml.Net.Parser;

#pragma warning disable MA0048 // MA0048: HumlConverter<T> — generic type name cannot appear in filename; HumlConverterT.cs is the canonical name for this one-type-per-file rule
namespace Huml.Net.Serialization;

/// <summary>
/// Typed HUML converter for <typeparamref name="T"/>. Override <see cref="Read"/> and
/// <see cref="Write"/> to control how <typeparamref name="T"/> is deserialised and serialised.
/// </summary>
/// <typeparam name="T">The CLR type this converter handles.</typeparam>
/// <remarks>
/// Instances are cached and shared across threads — converters must be stateless.
/// Do not call <see cref="HumlSerializerContext.AppendSerializedValue"/> with a value of
/// type <typeparamref name="T"/> inside <see cref="Write"/>; this creates infinite recursion.
/// Use <see cref="HumlSerializerContext.AppendRaw"/> for custom HUML output instead.
/// </remarks>
public abstract class HumlConverter<T> : HumlConverter
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(T);

    /// <summary>
    /// Reads a <see cref="HumlNode"/> and returns the deserialised <typeparamref name="T"/> value.
    /// </summary>
    /// <param name="node">The fully-parsed AST node for the value being deserialised.</param>
    /// <returns>The deserialised value, or <c>null</c> for nullable types.</returns>
    public abstract T? Read(HumlNode node);

    /// <summary>
    /// Writes <paramref name="value"/> to the serialiser output via <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The serialiser context providing append methods.</param>
    /// <param name="value">The value to serialise. Never <c>null</c> for reference types —
    /// null values are emitted as <c>null</c> by <see cref="WriteObject"/> before <c>Write</c>
    /// is called.</param>
    /// <remarks>
    /// When invoked from a property-level converter context, the output produced by this method
    /// is inserted inline as the scalar value for the mapping entry (<c>key: &lt;output&gt;</c>).
    /// The output must be a single-line HUML scalar (a quoted string, a number, a keyword such
    /// as <c>true</c> / <c>false</c> / <c>null</c>, etc.) — embedded newlines will break the
    /// current mapping entry's syntax. Use <see cref="HumlSerializerContext.AppendRaw"/> only
    /// when you are certain the output is a valid single-line value.
    /// </remarks>
    public abstract void Write(HumlSerializerContext context, T value);

    internal override object? ReadObject(HumlNode node) => Read(node);
    internal override void WriteObject(HumlSerializerContext context, object? value)
    {
        if (value is null)
        {
            if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
                throw new InvalidOperationException(
                    $"Converter '{GetType().Name}' received a null value for non-nullable value type '{typeof(T).Name}'.");
            // Reference type null: emit "null" directly — do not call Write which may not handle null.
            context.AppendRaw("null");
            return;
        }
        Write(context, (T)value);
    }
}
