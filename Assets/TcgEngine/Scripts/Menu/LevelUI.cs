using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TcgEngine.UI
{

    public class LevelUI : MonoBehaviour
    {
        public Text title;
        public Text subtitle;
        public DeckDisplay deck;
        public GameObject completed;

        private LevelData level;

        void Start()
        {
            Button btn = GetComponent<Button>();
            btn.onClick.AddListener(OnClick);
        }

        public void SetLevel(LevelData level)
        {
            this.level = level;
            title.text = level.title;
            subtitle.text = "LEVEL " + level.level;
            deck.SetDeck(level.player_deck);
            gameObject.SetActive(true);

            UserData udata = Authenticator.Get().GetUserData();
            completed.SetActive(udata.HasReward(level.id));
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnClick()
        {
            AdventurePanel.Get().OnClickAdventureLevel(level);
        }
    }
}
