using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{
    /// <summary>
    /// 當無法執行動作時顯示提示供玩家參考
    /// </summary>

    public class WarningText : MonoBehaviour
    {
        public AudioClip warning_audio;
        public Text text;

        private CanvasGroup canvas_group;
        private Animator animator;

        private static WarningText instance;

        void Awake()
        {
            instance = this;
            canvas_group = GetComponent<CanvasGroup>();
            animator = GetComponent<Animator>();
            canvas_group.alpha = 0f;
        }

        void Update()
        {

        }

        public void Show(string txt)
        {
            text.text = txt;
            canvas_group.alpha = 1f;
            animator.SetTrigger("play");
            AudioTool.Get().PlaySFX("warning", warning_audio, 0.7f, false);
        }

        public static void ShowText(string txt)
        {
            WarningText w = WarningText.Get();
            w.Show(txt);
        }

        public static void ShowNotYourTurn()
        {
            ShowText("還不是你的回合");
        }

        public static void ShowExhausted()
        {
            ShowText("無法再執行動作");
        }

        public static void ShowNoMana()
        {
            ShowText("法力值不夠");
        }

        public static void ShowSpellImmune()
        {
            ShowText("目標法術免疫");
        }

        public static void ShowInvalidTarget()
        {
            ShowText("無效目標");
        }

        public static WarningText Get()
        {
            return instance;
        }
    }
}
