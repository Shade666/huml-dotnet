// This shim allows `init`-only setters (C# 9+) to compile on netstandard2.1.
// The type is defined natively in .NET 5+ and later; for netstandard2.1 we provide
// a private stub so the compiler can emit the required custom modifier.
#if NETSTANDARD2_1

namespace System.Runtime.CompilerServices;

/// <summary>
/// Reserved for use by the compiler. Do not use.
/// </summary>
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
internal static class IsExternalInit { }

#endif
