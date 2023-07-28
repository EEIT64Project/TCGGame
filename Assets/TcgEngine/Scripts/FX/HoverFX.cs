using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.FX
{
    /// <summary>
    /// 懸停目標時出現的 FX
    /// </summary>

    public class HoverFX : MonoBehaviour
    {
        public GameObject fx;

        private bool hover = false;

        void Start()
        {

        }

        void Update()
        {
            if (hover != fx.activeSelf)
                fx.SetActive(hover);
        }

        public void PointerEnter()
        {
            hover = true;
        }

        public void PointerExit()
        {
            hover = false;
        }
    }
}
