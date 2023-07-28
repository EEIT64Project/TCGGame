using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.UI;

namespace TcgEngine.FX
{
    /// <summary>
    /// 卡牌受到傷害時出現的文本數字 FX
    /// </summary>

    public class DamageFX : MonoBehaviour
    {
        public Text text_value;

        void Start()
        {

        }

        void Update()
        {

        }

        public void SetValue(int value)
        {
            if (text_value != null)
                text_value.text = value.ToString();
        }

        public void SetValue(string value)
        {
            if (text_value != null)
                text_value.text = value;
        }
    }
}