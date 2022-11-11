using superzero4.WFC.Core.DataStructures;
using superzero4.WFC.Core.DataStructures.Cells;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using superzero4.WFC.Core.DataStructures.Collection;

namespace superzero4.WFC.Core
{

    public class GameManager : MonoBehaviour
    {
        [SerializeField] private int _maxX;
        [SerializeField] private int _maxY;
        [SerializeField] private List<GameObject> _gameObjects;
        [SerializeField, Range(0, 3f)] private float _delay;

        public enum Types { one, two, three, four }
        void Start()
        {
            int sqr = (int)Mathf.Sqrt(_gameObjects.Count);
            Debug.Log("Using " + sqr + " sized matrix");
            var wfc
                = new WFCRunner<CellBase<GameObject>, (int, int)>(new Matrix2D<CellBase<GameObject>>(sqr, sqr, _gameObjects.Select(g => new GameObjectCell(g))), new UnityDebugLogger());
            //= new WFCRunner<GameObjectCell, int>(new List1D<GameObjectCell>(_gameObjects.Select(g=>new CellBase<GameObject>(g))), new UnityDebugLogger());
            /*= new WFCRunner<GameObject, Types>(new Collection1D<GameObject, Types>(new Dictionary<Types, GameObject>()
                {
                    { Types.one,_gameObjects[0] },
                    { Types.two,_gameObjects[1] },
                    { Types.three,_gameObjects[2] },
                    { Types.four,_gameObjects[3] },
                }));*/
            /*for (int i = 0; i < sqr; i++)
                wfc.Run((i, i));*/
            StartCoroutine(wfc.IterativeRun((1, 1), new WaitForSeconds(_delay)));
        }
        public class UnityDebugLogger : LoggerWrapper
        {
            public override void Log(string message)
            {
                Debug.Log(message);
            }
        }
    }

}
