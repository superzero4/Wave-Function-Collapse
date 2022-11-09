using superzero4.WFC.Core.DataStructures;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace superzero4.WFC.Core
{

    public class WFCRunner<Cell,Indexer> where Indexer : struct
    {
        private APositionEnumerable<Cell, Indexer> _enumerable;

        public WFCRunner(APositionEnumerable<Cell, Indexer> enumerable)
        {
            _enumerable = enumerable;
        }

        internal void Run(Indexer start=default)
        {
            _enumerable.GetEnumerator();
            foreach (Cell cell in _enumerable.Relatives(start))
                Debug.Log(cell);
        }
    }

}
