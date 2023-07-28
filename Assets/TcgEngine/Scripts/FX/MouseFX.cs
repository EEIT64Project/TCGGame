using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.FX
{
    /// <summary>
    /// 跟隨鼠標的 FX
    /// </summary>

    public class MouseFX : MonoBehaviour
    {
        public float speed = 20f;

        void Start()
        {

        }

        // 每幀調用一次更新
        void Update()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.forward, 0f);
            plane.Raycast(ray, out float dist);
            Vector3 tpos = ray.GetPoint(dist);
            transform.position = Vector3.Lerp(transform.position, tpos, speed * Time.deltaTime);
        }
    }
}
