namespace Huml.Net.SourceGeneration;

/// <summary>A single gettable/settable property on a registered type.</summary>
internal readonly record struct PropertyModel(
    string Name,
    string TypeName,
    bool HasGet,
    bool HasSet,
    string DeclaringTypeFqn);
