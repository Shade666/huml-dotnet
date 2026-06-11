namespace Huml.Net.SourceGeneration;

/// <summary>A single gettable/settable property on a registered type.</summary>
/// <param name="Name">The CLR property name (used for member access; escaped when emitted).</param>
/// <param name="HumlKey">The HUML key — the <c>[HumlProperty]</c> override, else the CLR name.</param>
/// <param name="TypeName">Fully-qualified property type.</param>
/// <param name="HasGet">Whether a usable public getter exists.</param>
/// <param name="HasSet">Whether a usable settable (non-init) public setter exists. Init-only
/// setters are deliberately excluded — a compiled delegate cannot assign them.</param>
/// <param name="DeclaringTypeFqn">Fully-qualified type that declares the property.</param>
internal readonly record struct PropertyModel(
    string Name,
    string HumlKey,
    string TypeName,
    bool HasGet,
    bool HasSet,
    string DeclaringTypeFqn);
