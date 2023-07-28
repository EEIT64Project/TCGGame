using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;

namespace TcgEngine
{

    /// <summary>
    /// 該腳本啟動加載所有遊戲數據
    /// </summary>

    public class DataLoader : MonoBehaviour
    {
        public GameplayData data;
        public AssetData assets;

        private HashSet<string> card_ids = new HashSet<string>();
        private HashSet<string> ability_ids = new HashSet<string>();
        private HashSet<string> deck_ids = new HashSet<string>();

        private static DataLoader instance;

        void Awake()
        {
            instance = this;
            LoadData();
        }

        public void LoadData()
        {
            //為了加快加載速度，在每個 Load() 函數內添加一個相對於 Resources 文件夾的路徑
            //例如 CardData.Load("Cards");僅加載 Resources/Cards 文件夾內的數據
            CardData.Load();
            TeamData.Load();
            RarityData.Load();
            TraitData.Load();
            VariantData.Load();
            PackData.Load();
            LevelData.Load();
            DeckData.Load();
            AbilityData.Load();
            StatusData.Load();
            AvatarData.Load();
            CardbackData.Load();

            CheckCardData();
            CheckAbilityData();
            CheckDeckData();
        }

        //確保數據有效
        private void CheckCardData()
        {
            card_ids.Clear();
            foreach (CardData card in CardData.GetAll())
            {
                if (string.IsNullOrEmpty(card.id))
                    Debug.LogError(card.name + " id is empty");
                if (card_ids.Contains(card.id))
                    Debug.LogError("Dupplicate Card ID: " + card.id);

                if (card.team == null)
                    Debug.LogError(card.id + " team is null");
                if (card.rarity == null)
                    Debug.LogError(card.id + " rarity is null");

                foreach (TraitData trait in card.traits)
                {
                    if (trait == null)
                        Debug.LogError(card.id + " has null trait");
                }

                if (card.stats != null)
                {
                    foreach (TraitStat stat in card.stats)
                    {
                        if (stat.trait == null)
                            Debug.LogError(card.id + " has null stat trait");
                    }
                }

                foreach (AbilityData ability in card.abilities)
                {
                    if(ability == null)
                        Debug.LogError(card.id + " has null ability");
                }

                card_ids.Add(card.id);
            }
        }

        //確保數據有效
        private void CheckAbilityData()
        {
            ability_ids.Clear();
            foreach (AbilityData ability in AbilityData.GetAll())
            {
                if (string.IsNullOrEmpty(ability.id))
                    Debug.LogError(ability.name + " id is empty");
                if (ability_ids.Contains(ability.id))
                    Debug.LogError("Dupplicate Ability ID: " + ability.id);

                foreach (AbilityData chain in ability.chain_abilities)
                {
                    if (chain == null)
                        Debug.LogError(ability.id + " has null chain ability");
                }

                ability_ids.Add(ability.id);
            }
        }

        //確保數據有效
        private void CheckDeckData()
        {
            deck_ids.Clear();
            foreach (DeckData deck in DeckData.GetAll())
            {
                if (string.IsNullOrEmpty(deck.id))
                    Debug.LogError(deck.name + " id is empty");
                if (deck_ids.Contains(deck.id))
                    Debug.LogError("Dupplicate Deck ID: " + deck.id);

                foreach (CardData card in deck.cards)
                {
                    if (card == null)
                        Debug.LogError(deck.id + " has null card");
                }

                deck_ids.Add(deck.id);
            }
        }

        public static DataLoader Get()
        {
            return instance;
        }
    }
}