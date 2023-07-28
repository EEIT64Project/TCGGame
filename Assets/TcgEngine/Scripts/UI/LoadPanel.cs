using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TcgEngine.Client;

namespace TcgEngine.UI
{
    /// <summary>
    /// Loading panel that appears at the begining of a match, waiting for players to connect
    /// </summary>

    public class LoadPanel : UIPanel
    {
        public Text load_txt;

        private static LoadPanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected override void Start()
        {
            base.Start();

            GameClient.Get().onConnectGame += OnConnect;
            GameClient.Get().onPlayerReady += OnReady;
            GameClient.Get().onGameStart += OnStart;

            SetLoadText("正在連線至伺服器...");
        }

        private void OnConnect()
        {
            SetLoadText("傳送玩家資料中...");
        }

        private void OnStart()
        {
            SetLoadText("");
        }

        private void OnReady(int player_id)
        {
            if (player_id == GameClient.Get().GetPlayerID())
            {
                SetLoadText("正在等待對手...");
            }
        }

        private void SetLoadText(string text)
        {
            if (IsOnline())
            {
                if (load_txt != null)
                    load_txt.text = text;
                if (!string.IsNullOrWhiteSpace(text))
                    Debug.Log(text);
            }
        }

        public bool IsOnline()
        {
            return GameClient.game_settings.IsOnline();
        }

        public static LoadPanel Get()
        {
            return instance;
        }
    }
}
