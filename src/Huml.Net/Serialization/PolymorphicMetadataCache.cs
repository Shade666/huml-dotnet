using System.Collections.Concurrent;

namespace Huml.Net.Serialization;

/// <summary>
/// Process-wide caches for <c>[HumlPolymorphic]</c> and <c>[HumlDerivedType]</c> lookups,
/// shared by the serialiser and deserialiser. Attribute metadata is type-intrinsic, so a
/// static cache never goes stale. Caching the base-attribute lookup matters on the hot
/// path: <c>GetCustomAttribute</c> allocates on every call even when the type carries no
/// attribute at all.
/// </summary>
internal static class PolymorphicMetadataCache
{
    private static readonly ConcurrentDictionary<Type, HumlPolymorphicAttribute?> PolymorphicAttributeCache = new();

    private static readonly ConcurrentDictionary<Type, HumlDerivedTypeAttribute[]> DerivedTypeCache = new();

    internal static HumlPolymorphicAttribute? GetPolymorphicAttribute(Type type) =>
        PolymorphicAttributeCache.GetOrAdd(type, static t =>
            (HumlPolymorphicAttribute?)Attribute.GetCustomAttribute(t, typeof(HumlPolymorphicAttribute), inherit: false));

    internal static HumlDerivedTypeAttribute[] GetDerivedTypeRegistrations(Type type) =>
        DerivedTypeCache.GetOrAdd(type, static t =>
            (HumlDerivedTypeAttribute[])t.GetCustomAttributes(typeof(HumlDerivedTypeAttribute), inherit: false));
}
