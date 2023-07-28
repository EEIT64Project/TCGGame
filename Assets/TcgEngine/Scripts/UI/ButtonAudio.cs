using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 按鈕音效
    /// </summary>

    public class ButtonAudio : MonoBehaviour, IPointerEnterHandler
    {
        public AudioClip click_audio;
        public AudioClip hover_audio;

        void Start()
        {
            Button button = GetComponent<Button>();

            if (button != null)
                button.onClick.AddListener(OnClick);
        }

        void OnClick()
        {
            AudioTool.Get().PlaySFX("ui", click_audio);
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            HoverAudio();
        }


        void HoverAudio()
        {
            AudioTool.Get().PlaySFX("ui", hover_audio);
        }


    }
}
