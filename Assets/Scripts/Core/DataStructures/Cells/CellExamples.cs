using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace superzero4.WFC.Core.DataStructures.Cells
{

    public class GameObjectCell : CellBase<GameObject>
    {
        public GameObjectCell(GameObject data) : base(data) { }

        public override void Activate()
        {
            _data.SetActive(true);
            _data.GetComponent<Renderer>().material.SetColor(0, UnityEngine.Random.ColorHSV());
        }
    }

}
