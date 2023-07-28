﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 包含多個圖標來表示值
    /// 游戲過程中的法力條
    /// </summary>

    public class IconBar : MonoBehaviour
    {
        public int value = 0;
        public int max_value = 4;
        public bool auto_refresh = true;

        public Image[] icons;
        public Sprite sprite_full;
        public Sprite sprite_empty;

        void Awake()
        {

        }

        void Update()
        {
            if (auto_refresh)
                Refresh();
        }

        public void Refresh()
        {
            int index = 0;
            foreach (Image icon in icons)
            {
                icon.gameObject.SetActive(index < value || index < max_value);
                icon.sprite = (index < value) ? sprite_full : sprite_empty;
                index++;
            }
        }

        public void SetMat(Material mat)
        {
            foreach (Image icon in icons)
            {
                icon.material = mat;
            }
        }
    }
}