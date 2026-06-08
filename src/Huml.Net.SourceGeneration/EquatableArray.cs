using System;

namespace Huml.Net.SourceGeneration;

/// <summary>
/// A value-equatable wrapper for arrays. Used in Roslyn incremental pipeline models so the
/// engine can cache and skip regeneration when inputs have not changed.
/// </summary>
internal sealed class EquatableArray<T> : IEquatable<EquatableArray<T>>
    where T : IEquatable<T>
{
    public static readonly EquatableArray<T> Empty = new(Array.Empty<T>());

    private readonly T[] _array;

    public EquatableArray(T[] array) => _array = array;

    public int Count => _array.Length;

    public T this[int index] => _array[index];

    public T[] ToArray() => _array;

    public bool Equals(EquatableArray<T>? other)
    {
        if (other is null) return false;
        if (_array.Length != other._array.Length) return false;
        for (var i = 0; i < _array.Length; i++)
            if (!_array[i].Equals(other._array[i])) return false;
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var item in _array)
                hash = hash * 31 + item.GetHashCode();
            return hash;
        }
    }
}
