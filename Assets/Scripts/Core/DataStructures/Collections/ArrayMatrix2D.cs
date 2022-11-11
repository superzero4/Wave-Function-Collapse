using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace superzero4.WFC.Core.DataStructures.Collection
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
            public T NextY => this[(X, Y + 1)];

            public override T this[(int x, int y) position]
            {
                get => _data[position.x, position.y];
                protected set => _data[position.x, position.y] = value;
            }
            protected override IEnumerable<(int, int)> RelativesPositions((int, int) position)
            {
#if false
                IEnumerable<int> MinusOnePlusOne = Enumerable.Range(0, 2).Select(i => (i * 2) - 1);
                return MinusOnePlusOne.Select(i => (position.Item1 + i, position.Item2))
                    .Concat(MinusOnePlusOne.Select(i => (position.Item1, position.Item2 + 1)));
#else
                yield return (position.Item1 - 1, position.Item2);
                yield return (position.Item1 + 1, position.Item2);
                yield return (position.Item1, position.Item2 - 1);
                yield return (position.Item1, position.Item2 + 1);
#endif
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

            protected override bool IsInside((int x, int y) position)
            {
                return position.x >= 0 && position.x < xMax && position.y >= 0 && position.y < yMax;
            }
        }
    }
}
