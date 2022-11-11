using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace superzero4.WFC.Core.DataStructures.Collection
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
                dataCopier.MoveNext();
                _enumerator.Current = dataCopier.Current;
            }
        }
        public V this[P position] => _enumerator[position];
        /// <summary>
        /// As far as it's a custom method, it's not calling Reset, prefer using classic GetEnumerator() and _enumerator rather than that when initializing a new enumerator this
        /// </summary>
        /// <returns></returns>
        protected abstract APositionEnumerator<V, P> GetPositionEnumerator();
        protected APositionEnumerator<V, P> _enumerator;
        public APositionEnumerator<V, P> Enumerator => _enumerator;
        public IEnumerator<V> GetEnumerator()
        {
            _enumerator = GetPositionEnumerator();
            _enumerator.Reset();
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
        #region public
        public IEnumerable<T> Relatives(P current)
        {
            return SafeRelativesPosition(current).Select((p) => this[p]);
        }

        public IEnumerable<P> SafeRelativesPosition(P current)
        {
            return RelativesPositions(current).Where(p => IsInside(p));
        }

        public IEnumerable<T> Relatives() => Relatives(_position);
        #endregion
        #region protected abstract
        /// <summary>
        /// Unsafe, should be kept unsafe in implementaito, checks are made with IsInside method
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public abstract T this[P position] { get; protected set; }
        /// <summary>
        /// Will be used before generating any sequence of P, to avoid OutOfIndexException or things like that to be thrown by lower level
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        protected abstract bool IsInside(P position);
        protected bool IsInside() => IsInside(_position);
        protected abstract bool MoveNext(ref P position);
        protected abstract P ResetPosition();
        protected abstract IEnumerable<P> RelativesPositions(P current);
        #endregion
        protected P Position => _position;
        protected void ChangePositionTemp(P temporaryPositionToUse, Action actionToPerformWithChangedPos)
        {
            var init = _position;
            _position = temporaryPositionToUse;
            actionToPerformWithChangedPos();
            _position = init;
        }
        private P _position;
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
            MoveNext(ref _position);
            return IsInside();
        }
        public void Reset()
        {
            _position = ResetPosition();
        }

        #endregion
    }

}
