using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine.UI
{

    public class AdventurePanel : UIPanel
    {

        private List<LevelUI> level_uis = new List<LevelUI>();

        private static AdventurePanel instance;

        protected override void Awake()
        {
            base.Awake();
            instance = this;
        }

        protected override void Start()
        {
            base.Start();
            level_uis.AddRange(GetComponentsInChildren<LevelUI>());
        }

        private void RefreshLevels()
        {
            foreach (LevelUI ulvl in level_uis)
                ulvl.Hide();

            int index = 0;
            foreach (LevelData level in LevelData.GetAll())
            {
                if (index < level_uis.Count)
                {
                    level_uis[index].SetLevel(level);
                    index++;
                }
            }
        }

        public void OnClickAdventureLevel(LevelData level)
        {
            string uid = GameTool.GenerateRandomID();
            GameClient.game_settings.level = level.id;
            GameClient.game_settings.scene = level.scene;
            GameClient.player_settings.deck = level.player_deck.id;
            GameClient.ai_settings.deck = level.ai_deck.id;
            GameClient.ai_settings.ai_level = level.ai_level;
            MainMenu.Get().StartGame(PlayMode.Adventure, uid);
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);
            RefreshLevels();
        }

        public static AdventurePanel Get()
        {
            return instance;
        }
    }
}