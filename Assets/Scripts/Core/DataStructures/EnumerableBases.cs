using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace superzero4.WFC.Core.DataStructures
{

    public interface ICellEnumerator<T>
    {
        public IEnumerable<T> GetRelatives();
    }
    public interface IRelatives<T, P>
    {
        public IEnumerable<T> Relatives(P current);
        public IEnumerable<T> Relatives();
    }
    public abstract class APositionEnumerable<V, P> : IEnumerable<V>, IRelatives<V, P> where P : struct
    {
        // Data to enumerate throught
        //public IPositionEnumerable(IEnumerable<V> data)
        //{
        //}
        protected void CopyData(IEnumerable<V> data)
        {
            var dataCopier = data.GetEnumerator();
            foreach (var _ in this)
            {
                _enumerator.Current = dataCopier.Current;
            }
        }
        /// <summary>
        /// As far as it's a custom method, it's not calling Reset, prefer using classic GetEnumerator() and _enumerator rather than that when initializing a new enumerator
        /// </summary>
        /// <returns></returns>
        protected abstract APositionEnumerator<V, P> GetPositionEnumerator();
        public APositionEnumerator<V, P> _enumerator;
        public IEnumerator<V> GetEnumerator()
        {
            _enumerator = GetPositionEnumerator();
            return _enumerator;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerable<V> Relatives(P from)
        {
            return _enumerator.Relatives(from);
        }
        public IEnumerable<V> Relatives() => _enumerator.Relatives();
    }
    public abstract class APositionEnumerator<T, P> : IEnumerator<T>, IRelatives<T, P> where P : struct
    {
        #region Position
        public abstract T this[P position] { get; set; }
        private P _position;
        protected P Position => _position;
        protected abstract bool MoveNext(ref P position);

        protected abstract P ResetPosition();
        #endregion
        #region IEnumeratorImplementationWithPosition
        public T Current
        {
            get => this[_position];
            set => this[_position] = value;
        }

        object IEnumerator.Current => Current;
        public void Dispose()
        {
            _position = default;
        }
        public bool MoveNext()
        {
            return MoveNext(ref _position);
        }
        public void Reset()
        {
            _position = ResetPosition();
        }

        public abstract IEnumerable<T> Relatives(P current);
        public IEnumerable<T> Relatives() => Relatives(_position);
        protected void ChangePositionTemp(P temporaryPositionToUse, Action actionToPerformWithChangedPos)
        {
            var init = _position;
            _position = temporaryPositionToUse;
            actionToPerformWithChangedPos();
            _position = init;
        }
        #endregion
    }

}
