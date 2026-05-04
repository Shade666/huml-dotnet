// Shim for System.Diagnostics.CodeAnalysis trim/AOT annotation attributes on netstandard2.1.
// RequiresUnreferencedCodeAttribute and DynamicallyAccessedMembersAttribute were introduced in
// .NET 5.0; RequiresDynamicCodeAttribute was introduced in .NET 7.0. None are available on
// netstandard2.1. This shim allows the compiler to process the attribute syntax on all TFMs;
// the attributes are metadata-only (no-ops at runtime) on all platforms.
// Pattern mirrors IsExternalInit.cs already in this project.
#if NETSTANDARD2_1

// MA0048: File name must match type name — intentionally suppressed; this file defines multiple
// shim types that must live together (they form a single logical unit mirroring the BCL namespace).
// MA0062: Non-flags enum 'All = ~None' — suppressed; ~None is the canonical BCL pattern for
// DynamicallyAccessedMemberTypes.All (see dotnet/runtime source).
#pragma warning disable MA0048, MA0062

namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(
    AttributeTargets.Constructor | AttributeTargets.Method,
    Inherited = false)]
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal sealed class RequiresUnreferencedCodeAttribute : Attribute
{
    public RequiresUnreferencedCodeAttribute(string message) { Message = message; }
    public string Message { get; }
    public string? Url { get; set; }
}

[AttributeUsage(
    AttributeTargets.Constructor | AttributeTargets.Method,
    Inherited = false)]
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal sealed class RequiresDynamicCodeAttribute : Attribute
{
    public RequiresDynamicCodeAttribute(string message) { Message = message; }
    public string Message { get; }
    public string? Url { get; set; }
}

[Flags]
internal enum DynamicallyAccessedMemberTypes
{
    None                           = 0,
    PublicParameterlessConstructor = 0x0001,
    PublicConstructors             = 0x0002,
    NonPublicConstructors          = 0x0004,
    PublicMethods                  = 0x0008,
    NonPublicMethods               = 0x0010,
    PublicFields                   = 0x0020,
    NonPublicFields                = 0x0040,
    PublicNestedTypes              = 0x0080,
    NonPublicNestedTypes           = 0x0100,
    PublicProperties               = 0x0200,
    NonPublicProperties            = 0x0400,
    PublicEvents                   = 0x0800,
    NonPublicEvents                = 0x1000,
    Interfaces                     = 0x2000,
    All                            = ~None,
}

[AttributeUsage(
    AttributeTargets.Field | AttributeTargets.GenericParameter |
    AttributeTargets.Method | AttributeTargets.Parameter |
    AttributeTargets.Property | AttributeTargets.ReturnValue,
    Inherited = false)]
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal sealed class DynamicallyAccessedMembersAttribute : Attribute
{
    public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
        { MemberTypes = memberTypes; }
    public DynamicallyAccessedMemberTypes MemberTypes { get; }
}

#pragma warning restore MA0048, MA0062

#endif
