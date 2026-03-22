using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace AutoLineColor.Util
{
    internal class ReadOnlyList<T> : IReadOnlyList<T>
    {
        [NotNull] private readonly IList<T> _list;

        public ReadOnlyList([NotNull] IList<T> list)
        {
            _list = list;
        }

        public ReadOnlyList([NotNull] IEnumerable<T> list)
        {
            _list = list.ToArray();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _list.Count;

        public T this[int index] => _list[index];
    }
}
