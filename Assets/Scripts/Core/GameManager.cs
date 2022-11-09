using superzero4.WFC.Core.DataStructures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace superzero4.WFC.Core
{

    public class GameManager : MonoBehaviour
    {
        [SerializeField] private int _maxX;
        [SerializeField] private int _maxY;
        [SerializeField] private List<GameObject> _gameObjects;
        public enum Types { one, two, three, four }
        void Start()
        {
            int sqr = (int)Mathf.Sqrt(_gameObjects.Count);
            Debug.Log("Using " + sqr + " sized matrix");
            var wfc
                = new WFCRunner<GameObject,(int,int)>(new Matrix2D<GameObject>(sqr, sqr, _gameObjects));
                /*= new WFCRunner<GameObject, Types>(new Collection1D<GameObject, Types>(new Dictionary<Types, GameObject>()
                {
                    { Types.one,_gameObjects[0] },
                    { Types.two,_gameObjects[1] },
                    { Types.three,_gameObjects[2] },
                    { Types.four,_gameObjects[3] },
                }));*/
            wfc.Run((1,1));
        }
    }

}
