namespace Huml.Net.SourceGeneration;

/// <summary>A CLR type registered via <c>[HumlSerializable(typeof(T))]</c>.</summary>
internal readonly record struct TypeModel(
    string Name,
    string FullyQualifiedName,
    EquatableArray<PropertyModel> Properties);
