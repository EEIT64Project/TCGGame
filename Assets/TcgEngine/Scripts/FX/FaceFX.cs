using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine.FX
{
    /// <summary>
    /// 旋轉 FX 面向相機
    /// </summary>

    public class FaceFX : MonoBehaviour
    {
        public FaceType type;

        void Start()
        {
            Vector3 up = GameBoard.Get().transform.up;

            if (type == FaceType.FaceCamera)
            {
                GameCamera cam = GameCamera.Get();
                if (cam != null)
                {
                    Vector3 forward = cam.transform.forward;
                    transform.rotation = Quaternion.LookRotation(forward, up);
                }
            }

            if (type == FaceType.FaceCameraCenter)
            {
                GameCamera cam = GameCamera.Get();
                if (cam != null)
                {
                    Vector3 forward = transform.position - cam.transform.position;
                    transform.rotation = Quaternion.LookRotation(forward.normalized, up);
                }
            }

            if (type == FaceType.FaceBoard)
            {
                GameBoard board = GameBoard.Get();
                if (board != null)
                {
                    Vector3 forward = board.transform.forward;
                    transform.rotation = Quaternion.LookRotation(forward, up);
                }
            }
        }
    }

    public enum FaceType
    {
        FaceCamera = 0,         //設置與相機旋轉相同的旋轉
        FaceCameraCenter = 5,   //臉部相機世界位置
        FaceBoard = 10          //設置與板旋轉相同的旋轉
    }
}
