namespace Huml.Net.SourceGeneration;

/// <summary>The user-declared <c>HumlGeneratedContext</c> subclass with all its registered types.</summary>
internal readonly record struct ContextModel(
    string ClassName,
    string Namespace,
    EquatableArray<TypeModel> Types);
