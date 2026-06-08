// Shim allowing init-only setters and record types to compile on netstandard2.0.
// IsExternalInit is defined natively in .NET 5+; netstandard2.0 needs this stub.
#if NETSTANDARD2_0

namespace System.Runtime.CompilerServices;

/// <summary>Reserved for use by the compiler. Do not use.</summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal static class IsExternalInit { }

#endif
