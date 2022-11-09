using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace superzero4.WFC.Core.DataStructures
{
    public class Matrix2D<T> : APositionEnumerable<T, (int, int)> where T : class
    {
        public T[,] _data;
        public Matrix2D(int xMax, int yMax, IEnumerable<T> _dataToCopy)
        {
            _data = new T[xMax, yMax];
            CopyData(_dataToCopy);
        }
        protected override APositionEnumerator<T, (int, int)> GetPositionEnumerator()
        {
            return new MatrixEnumerator(_data);
        }

        public class MatrixEnumerator : APositionEnumerator<T, (int x, int y)>
        {
            private T[,] _data;
            private readonly int xMax, yMax;
            public int X => Position.x;
            public int Y => Position.y;
            public MatrixEnumerator(T[,] data)
            {
                _data = data;
                xMax = data.GetLength(0);
                yMax = data.GetLength(1);
            }
            public T PreviousX => this[(X - 1, Y)];
            public T NextX => this[(X + 1, Y)];
            public T PreviousY => this[(X, Y - 1)];
            public T NextY => this[(X , Y + 1)];

            public override T this[(int x, int y) position]
            {
                get
                {
                    if (position.x >= 0 && position.x < xMax && position.y >= 0 && position.y < yMax)
                        return _data[position.x, position.y];
                    return null;
                }
                set => _data[position.x, position.y] = value;
            }
            public override IEnumerable<T> Relatives((int, int) position)
            {
                IEnumerable<T> l = null;
                ChangePositionTemp(position, () => l = new List<T>() { NextX, PreviousX, NextY, PreviousY }.Where(t => t != null));
                return l;
            }
            protected override bool MoveNext(ref (int x, int y) position)
            {
                position.x++;
                if (position.x == xMax)
                {
                    position.x = 0;
                    position.y++;
                    if (position.y == yMax)
                        return false;
                }
                return true;
            }

            protected override (int, int) ResetPosition()
            {
                return (-1, 0);
            }
        }
    }
}
