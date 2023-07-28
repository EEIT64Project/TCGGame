using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.FX
{
    /// <summary>
    /// 捕捉到另一個對象（目標）並跟隨它的 FX
    /// </summary>

    public class SnapFX : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = Vector3.zero;

        void Start()
        {

        }

        void Update()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = target.position + offset;
        }
    }
}
