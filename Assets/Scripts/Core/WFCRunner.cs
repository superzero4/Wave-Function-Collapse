using superzero4.WFC.Core.DataStructures;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using superzero4.WFC.Core.DataStructures.Cells;
using superzero4.WFC.Core.DataStructures.Collection;

namespace superzero4.WFC.Core
{

    public class WFCRunner<Cell, Indexer> where Indexer : struct where Cell : IMarkable, IActivable
    {
        private APositionEnumerable<Cell, Indexer> _enumerable;
        private readonly LoggerWrapper Debug;

        public WFCRunner(APositionEnumerable<Cell, Indexer> enumerable, LoggerWrapper logger)
        {
            _enumerable = enumerable;
            Debug = logger;
        }

        public void Run(Indexer start = default)
        {
            IEnumerable<Cell> dfs = Init(start);
            foreach (Cell cell in dfs)
            {
                cell.Activate();
            }
        }

        public IEnumerator IterativeRun<Yield>(Indexer start = default, Yield yieldOnEachStep = null) where Yield : class
        {
            var dfs = Init(start);
            foreach (Cell cell in dfs)
            {
                cell.Activate();
                yield return yieldOnEachStep;
            }
        }
        private IEnumerable<Cell> Init(Indexer start)
        {
            Debug.Log("Running from " + start);
            _enumerable.GetEnumerator();
            IEnumerable<Cell> dfs = new DFSRunner(_enumerable).DFS(start).Select(p => _enumerable[p]);
            return dfs;
        }

        public class DFSRunner
        {
            private APositionEnumerable<Cell, Indexer> _enumerable;
            private ControlStructure<Indexer> _controlStructure;
            public DFSRunner(APositionEnumerable<Cell, Indexer> enumerable)
            {
                _enumerable = enumerable;
                _controlStructure = new StackWrapper<Indexer>();
            }
            int loopCount = 0;
            public IEnumerable<Indexer> DFS(Indexer start)
            {
                _controlStructure.Add(start);
                foreach (var cell in _enumerable)
                    cell.Marked = false;
                while (_controlStructure.Count != 0 && loopCount < 100000)
                {
                    Indexer popped;
                    loopCount++;
                    popped = _controlStructure.Get();
                    yield return popped;
                    IEnumerable<Indexer> unMarkedRelatives = _enumerable.Enumerator.SafeRelativesPosition(popped).Where(rel => !_enumerable[rel].Marked);
                    foreach (Indexer item in unMarkedRelatives)
                    {
                        OnAdded(popped);
                        _controlStructure.Add(item);
                    }
                }
            }

            private void OnAdded(Indexer popped)
            {
                DoubleLog("Marked", popped);
                _enumerable[popped].Marked = true;
            }
            public void DoubleLog(string msg, Indexer popped) => UnityEngine.Debug.Log(_enumerable[popped] + "  " + msg + " @ " + popped);
        }
    }
    public abstract class ControlStructure<T>
    {
        public abstract void Add(T start);
        internal abstract T Get();
        public abstract int Count { get; }
    }
    public class StackWrapper<T> : ControlStructure<T>
    {
        private readonly Stack<T> _stack;

        public StackWrapper()
        {
            _stack = new Stack<T>();
        }

        public override int Count => _stack.Count;
        public override void Add(T start)
        {
            _stack.Push(start);
        }
        internal override T Get()
        {
            return _stack.Pop();
        }
    }
    public abstract class LoggerWrapper
    {
        public abstract void Log(string message);
    }

}
