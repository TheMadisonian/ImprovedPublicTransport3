// .NET 3.5 compatibility shims for Auto Line Color.
// IReadOnlyList<T>, IReadOnlyDictionary<,>, and ReadOnlyDictionary<,> are absent from both
// the .NET 3.5 reference assemblies used at compile-time and the game's bundled mscorlib.
// These minimal implementations satisfy the compile-time contract; all usage is internal to ALC.
// Note: variance (out/in) is omitted because net35's IEnumerable<T> is invariant.

namespace System.Collections.Generic
{
    public interface IReadOnlyCollection<T> : IEnumerable<T>
    {
        int Count { get; }
    }

    public interface IReadOnlyList<T> : IReadOnlyCollection<T>
    {
        T this[int index] { get; }
    }

    public interface IReadOnlyDictionary<TKey, TValue> : IReadOnlyCollection<KeyValuePair<TKey, TValue>>
    {
        TValue this[TKey key] { get; }
        IEnumerable<TKey> Keys { get; }
        IEnumerable<TValue> Values { get; }
        bool ContainsKey(TKey key);
        bool TryGetValue(TKey key, out TValue value);
    }
}

namespace System.Collections.ObjectModel
{
    using System.Collections;
    using System.Collections.Generic;

    public class ReadOnlyDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _dict;

        public ReadOnlyDictionary(IDictionary<TKey, TValue> dict)
        {
            _dict = dict;
        }

        public TValue this[TKey key] => _dict[key];
        public IEnumerable<TKey> Keys => _dict.Keys;
        public IEnumerable<TValue> Values => _dict.Values;
        public int Count => _dict.Count;
        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _dict.GetEnumerator();
    }
}
