namespace Huml.Net.Serialization.Attributes;

/// <summary>
/// Registers a type for HUML source-generation. Apply to a <c>partial</c> subclass of
/// <c>HumlGeneratedContext</c>; the source generator will emit a
/// <c>HumlTypeInfo&lt;T&gt;</c> for each registered type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class HumlSerializableAttribute : Attribute
{
    /// <summary>The CLR type to register for source-generation.</summary>
    public Type SerializableType { get; }

    /// <summary>Initialises a new instance registering <paramref name="serializableType"/> for source-generation.</summary>
    /// <param name="serializableType">The CLR type to register for source-generation.</param>
    public HumlSerializableAttribute(Type serializableType)
    {
        SerializableType = serializableType;
    }
}
