using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;
using System;
using System.Linq;

namespace superzero4.WFC.Core.DataStructures
{

    public class Collection1D<T, E> : APositionEnumerable<T, E> where E : struct
    {
        public Dictionary<E, T> _dictionary;
        public Collection1D(Dictionary<E, T> dictionary)
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

            public override T this[E position]
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

            public override IEnumerable<T> Relatives(E current)
            {
                IEnumerable<T> result = null;
                ChangePositionTemp(current, () => result = new List<T>() { Current });
                return result;
            }

            protected override bool MoveNext(ref E position)
            {
                index = EnumToInt(position);
                index++;
                if (index == Enum.GetValues(typeof(E)).Length)
                    return false;
                return true;
            }

            protected override E ResetPosition()
            {
                index = -1;
                return default;
            }
        }
    }

}
