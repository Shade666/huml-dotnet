// This shim allows the C# `required` modifier to compile on netstandard2.1 and pre-.NET-7 targets.
// RequiredMemberAttribute is defined natively in .NET 7+; for older TFMs we provide a private
// stub so the compiler can emit the required custom modifier on properties.
#if !NET7_0_OR_GREATER

namespace System.Runtime.CompilerServices;

[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
[System.AttributeUsage(
    System.AttributeTargets.Class | System.AttributeTargets.Struct |
    System.AttributeTargets.Field | System.AttributeTargets.Property,
    AllowMultiple = false, Inherited = false)]
internal sealed class RequiredMemberAttribute : System.Attribute { }

#endif
