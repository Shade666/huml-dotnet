namespace Huml.Net.Serialization;

/// <summary>
/// Marks a property as required during HUML deserialisation.
/// When a HUML input does not contain a mapping entry for this property,
/// <c>HumlDeserializeException</c> is thrown listing all missing required members.
/// Has no effect during <c>HumlSerializer.Populate&lt;T&gt;</c>.
/// Mirrors <c>System.Text.Json.Serialization.JsonRequiredAttribute</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class HumlRequiredAttribute : Attribute { }
