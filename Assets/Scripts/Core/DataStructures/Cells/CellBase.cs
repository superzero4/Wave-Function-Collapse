using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace superzero4.WFC.Core.DataStructures.Cells
{

    public interface IMarkable
    {
        public bool Marked { get; set; }
    }
    public interface IActivable
    {
        public void Activate();
    }
    [System.Serializable]
    public abstract class CellBase<T> : IMarkable,IActivable
    {
        [SerializeField]
        protected T _data;

        public CellBase(T data)
        {
            _data = data;
        }

        private bool _marked=false;
        public bool Marked { get => _marked; set => _marked = value; }
        public override string ToString()
        {
            return _data.ToString();
        }

        public abstract void Activate();
    }
}
