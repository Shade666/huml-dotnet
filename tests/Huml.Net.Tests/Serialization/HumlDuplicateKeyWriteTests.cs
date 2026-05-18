using System.Collections;
using AwesomeAssertions;
using Huml.Net.Exceptions;
using Huml.Net.Versioning;
using Xunit;

namespace Huml.Net.Tests.Serialization;

public class HumlDuplicateKeyWriteTests
{
    // ── DUP-01: Default value is false ────────────────────────────────────────

    [Fact]
    public void DUP01_ValidateDuplicateKeysOnWrite_DefaultIsFalse()
    {
        new HumlOptions().ValidateDuplicateKeysOnWrite.Should().BeFalse();
        HumlOptions.Default.ValidateDuplicateKeysOnWrite.Should().BeFalse();
        HumlOptions.LatestSupported.ValidateDuplicateKeysOnWrite.Should().BeFalse();
    }

    // ── DUP-02: false path — no throw ─────────────────────────────────────────

    [Fact]
    public void DUP02_WhenFalse_DuplicateDictKeys_NoThrow()
    {
        var dict = new DuplicateKeyDictionary("foo");
        var options = new HumlOptions { ValidateDuplicateKeysOnWrite = false };

        var act = () => Huml.Serialize(dict, options);

        act.Should().NotThrow();
    }

    // ── DUP-03: true path — throws with key name in message ───────────────────

    [Fact]
    public void DUP03_WhenTrue_DuplicateDictKey_ThrowsHumlSerializeException()
    {
        var dict = new DuplicateKeyDictionary("foo");
        var options = new HumlOptions { ValidateDuplicateKeysOnWrite = true };

        var act = () => Huml.Serialize(dict, options);

        var ex = act.Should().Throw<HumlSerializeException>().Which;
        ex.Message.Should().Contain("foo");
    }

    // ── DUP-04: case-different keys are NOT duplicates (StringComparer.Ordinal) ─

    [Fact]
    public void DUP04_CaseDifferentKeys_AreNotDuplicates()
    {
        var dict = new DuplicateKeyDictionary("foo", "FOO");
        var options = new HumlOptions { ValidateDuplicateKeysOnWrite = true };

        var act = () => Huml.Serialize(dict, options);

        act.Should().NotThrow();
    }

    // ── DUP-05: single-entry dictionary never throws ───────────────────────────

    [Fact]
    public void DUP05_SingleEntryDict_NoThrow()
    {
        var dict = new Dictionary<string, string> { ["only"] = "one" };
        var options = new HumlOptions { ValidateDuplicateKeysOnWrite = true };

        var act = () => Huml.Serialize(dict, options);

        act.Should().NotThrow();
    }

    // ── DUP-06: empty dictionary never throws ─────────────────────────────────

    [Fact]
    public void DUP06_EmptyDict_NoThrow()
    {
        var dict = new Dictionary<string, string>();
        var options = new HumlOptions { ValidateDuplicateKeysOnWrite = true };

        var act = () => Huml.Serialize(dict, options);

        act.Should().NotThrow();
    }

    // ── DUP-07: nested dict — inner duplicate throws with inner key name ───────

    [Fact]
    public void DUP07_NestedDict_InnerDuplicate_Throws()
    {
        var inner = new DuplicateKeyDictionary("inner-key");
        var outer = new Dictionary<string, object> { ["outer"] = inner };
        var options = new HumlOptions { ValidateDuplicateKeysOnWrite = true };

        var act = () => Huml.Serialize(outer, options);

        var ex = act.Should().Throw<HumlSerializeException>().Which;
        ex.Message.Should().Contain("inner-key");
    }

    // ── DuplicateKeyDictionary helper ─────────────────────────────────────────

    /// <summary>
    /// A minimal <see cref="IDictionary"/> that yields exactly two entries with
    /// programmer-controlled keys. Necessary because <see cref="Dictionary{TKey,TValue}"/>
    /// enforces unique keys at the type level.
    /// </summary>
    private sealed class DuplicateKeyDictionary : IDictionary
    {
        private readonly (string Key, string Value)[] _entries;

        /// <summary>Creates two entries with the same key string.</summary>
        public DuplicateKeyDictionary(string duplicateKey)
        {
            _entries = [(duplicateKey, "v1"), (duplicateKey, "v2")];
        }

        /// <summary>Creates two entries with distinct key strings.</summary>
        public DuplicateKeyDictionary(string key1, string key2)
        {
            _entries = [(key1, "v1"), (key2, "v2")];
        }

        public int Count => _entries.Length;
        public bool IsFixedSize => true;
        public bool IsReadOnly => true;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        public ICollection Keys => Array.Empty<object>();
        public ICollection Values => Array.Empty<object>();

        public IEnumerator GetEnumerator() => new Enumerator(_entries);
        IDictionaryEnumerator IDictionary.GetEnumerator() => new Enumerator(_entries);

        // Unused IDictionary members
        public object? this[object key]
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public void Add(object key, object? value) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(object key) => throw new NotSupportedException();
        public void CopyTo(Array array, int index) => throw new NotSupportedException();
        public void Remove(object key) => throw new NotSupportedException();

        private sealed class Enumerator : IDictionaryEnumerator
        {
            private readonly (string Key, string Value)[] _entries;
            private int _index = -1;

            public Enumerator((string Key, string Value)[] entries) => _entries = entries;

            public bool MoveNext() => ++_index < _entries.Length;
            public void Reset() => _index = -1;

            public DictionaryEntry Entry
                => new(_entries[_index].Key, _entries[_index].Value);

            public object Key => _entries[_index].Key;
            public object Value => _entries[_index].Value;
            public object Current => Entry;
        }
    }
}
