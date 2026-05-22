namespace Huml.Net.Serialization;

/// <summary>
/// Designates the constructor to use during HUML deserialisation. When multiple public
/// constructors exist, annotate exactly one with this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = false)]
public sealed class HumlConstructorAttribute : Attribute { }
