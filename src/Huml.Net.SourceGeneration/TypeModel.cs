namespace Huml.Net.SourceGeneration;

/// <summary>A CLR type registered via <c>[HumlSerializable(typeof(T))]</c>.</summary>
/// <param name="Name">The type's simple name (for display only).</param>
/// <param name="PropertyName">The public accessor name on the generated context — the simple
/// name when unique within the context, else the collision-free <paramref name="UniqueId"/>.</param>
/// <param name="UniqueId">A collision-free identifier derived from the fully-qualified name,
/// used for generated member and nested-class names so two registered types that share a
/// simple name do not collide.</param>
/// <param name="FullyQualifiedName">Fully-qualified type name.</param>
/// <param name="CanConstruct">Whether <c>new T()</c> is valid — the type is non-abstract, has an
/// accessible parameterless constructor, and has no <c>required</c> members. When false the
/// generated <c>CreateObject</c> is <c>null</c> and the deserialiser falls back to its
/// constructor-binding/reflection path.</param>
/// <param name="Properties">The serialisable properties.</param>
internal readonly record struct TypeModel(
    string Name,
    string PropertyName,
    string UniqueId,
    string FullyQualifiedName,
    bool CanConstruct,
    EquatableArray<PropertyModel> Properties);
