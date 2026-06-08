using Huml.Net.Serialization;

namespace Huml.Net.Tests.Serialization;

[HumlPolymorphic]
[HumlDerivedType(typeof(SGCircle), "circle")]
public class SGShape
{
    public string Color { get; set; } = string.Empty;
}
