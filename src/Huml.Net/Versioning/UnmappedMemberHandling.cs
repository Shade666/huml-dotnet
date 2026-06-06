namespace Huml.Net.Versioning;

/// <summary>
/// Controls how <see cref="T:Huml.Net.Serialization.HumlDeserializer"/> behaves when a HUML
/// document contains a key that does not match any property on the target type and is not
/// captured by a <c>[HumlExtensionData]</c> property.
/// </summary>
public enum UnmappedMemberHandling
{
    /// <summary>
    /// Silently ignore unrecognised keys. This is the default, preserving existing behaviour
    /// and forward-compatibility with documents produced by newer HUML writers.
    /// </summary>
    Skip = 0,

    /// <summary>
    /// Throw <see cref="T:Huml.Net.Exceptions.HumlDeserializeException"/> when an unrecognised
    /// key is encountered and no <c>[HumlExtensionData]</c> property is present on the type.
    /// Use this in strict pipelines where unexpected keys indicate a schema mismatch.
    /// </summary>
    Disallow = 1,
}
