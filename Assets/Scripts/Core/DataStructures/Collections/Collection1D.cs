using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using System;
using System.Linq;

namespace superzero4.WFC.Core.DataStructures.Collection
{
    public class List1D<T> : APositionEnumerable<T, int>
    {
        public IList<T> _list;
        public List1D(IEnumerable<T> list)
        {
            _list = new List<T>(list);
        }
        protected override APositionEnumerator<T, int> GetPositionEnumerator()
        {
            return new ListPositionEnumerator(_list);
            //return new ListPositionWrappingEnumerator(_list);
        }
        private class ListPositionEnumerator : APositionEnumerator<T, int>
        {
            protected readonly IList<T> _list;
            public ListPositionEnumerator(IList<T> list)
            {
                _list = list;
            }

            public override T this[int position] { get => _list[position]; protected set => _list[position] = value; }

            protected override bool IsInside(int position)
            {
                return position >= 0 && position < _list.Count;
            }

            protected override bool MoveNext(ref int position)
            {
                position++;
                return position < _list.Count;
                //Infinite collection variant, should only be used with a break condition (for instance, visited all members)
                /*if (position == _list.Count)
                    position = 0;
                return true;*/
            }

            protected override IEnumerable<int> RelativesPositions(int current)
            {
                yield return (current - 1);
                yield return (current + 1);
            }

            protected override int ResetPosition()
            {
                return -1;
            }
        }
        private class ListPositionWrappingEnumerator : ListPositionEnumerator
        {
            public ListPositionWrappingEnumerator(IList<T> list) : base(list)
            {
            }
            protected override IEnumerable<int> RelativesPositions(int current)
            {
                yield return (current - 1 + _list.Count) % _list.Count;
                yield return (current + 1) % _list.Count;
            }
        }
    }
    /*public class Dictionnary1D<T, E> : APositionEnumerable<T, E> where E : struct
    {
        public Dictionary<E, T> _dictionary;
        public Dictionnary1D(Dictionary<E, T> dictionary)
        {
            _dictionary = dictionary;
        }

        protected override APositionEnumerator<T, E> GetPositionEnumerator()
        {
            return new DictionaryEnumerator(_dictionary);
        }

        private static int EnumToInt(E key)
        {
            return Enum.GetNames(key.GetType())
                            .Select((s, index) => (s, index))
                            .First(si => si.s.Equals(key.ToString())).index;
        }

        public class DictionaryEnumerator : APositionEnumerator<T, E>
        {
            private Dictionary<int, T> _dictionary;
            int index;

            public DictionaryEnumerator(IDictionary<E, T> dictionary)
            {
                _dictionary = new Dictionary<int, T>(dictionary.Select(kv => new KeyValuePair<int, T>(
                EnumToInt(kv.Key)
                , kv.Value)));
            }

            protected override T this[E position]
            {
                get
                {
                    if (_dictionary.TryGetValue(EnumToInt(position), out T value))
                        return value;
                    return default(T);
                }
                set
                {
                    if (_dictionary.ContainsKey(EnumToInt(position)))
                        _dictionary[EnumToInt(position)] = value;
                    else
                        _dictionary.Add(EnumToInt(position), value);
                }
            }
            protected override IEnumerable<E> RelativesPositions(E current)
            {
                Debug.LogWarning("This implementation is only returning current element, quite worthless");
                yield return current;
            }

            protected override void GetNextPos()
            {
                index = EnumToInt(Position);
                index++;
                return index;
            }


            protected override E ResetPosition()
            {
                index = -1;
                return default;
            }

            protected override bool IsInside(E position)
            {
                int index = EnumToInt(position);
                return index>0 && index == Enum.GetValues(typeof(E)).Length
            }
        }
    }
    */

}
