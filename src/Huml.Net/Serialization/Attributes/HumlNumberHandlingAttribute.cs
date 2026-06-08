using Huml.Net.Versioning;

namespace Huml.Net.Serialization;

/// <summary>
/// Overrides <see cref="HumlOptions.NumberHandling"/> for the annotated property.
/// Takes precedence over the global option during serialisation and deserialisation.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class HumlNumberHandlingAttribute : Attribute
{
    /// <summary>
    /// The per-member number handling mode.
    /// </summary>
    public HumlNumberHandling Handling { get; }

    /// <summary>Initialises a new instance specifying the number handling mode.</summary>
    /// <param name="handling">The number handling mode for this property.</param>
    public HumlNumberHandlingAttribute(HumlNumberHandling handling)
    {
        Handling = handling;
    }
}
