using Huml.Net.Serialization;
using Huml.Net.Serialization.Attributes;

namespace Huml.Net.Tests.Serialization;

[HumlSerializable(typeof(SGShape))]
[HumlSerializable(typeof(SGCircle))]
public partial class SGShapeContext : HumlGeneratedContext { }
