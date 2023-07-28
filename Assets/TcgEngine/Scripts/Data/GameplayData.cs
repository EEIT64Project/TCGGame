using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.AI;

namespace TcgEngine
{
    /// <summary>
    /// 通用遊戲設置，起始統計數據、牌組限制、場景和 AI 級別
    /// </summary>

    [CreateAssetMenu(fileName = "GameplayData", menuName = "TcgEngine/GameplayData", order = 0)]
    public class GameplayData : ScriptableObject
    {
        [Header("Gameplay")]
        public int hp_start = 20;
        public int mana_start = 1;
        public int mana_per_turn = 1;
        public int mana_max = 10;
        public int cards_start = 5;
        public int cards_per_turn = 1;
        public int cards_max = 10;
        public float turn_duration = 30f;
        public CardData second_bonus;

        [Header("Deckbuilding")]
        public int deck_size = 30;
        public int deck_duplicate_max = 2;

        [Header("Buy/Sell")]
        public float sell_ratio = 0.8f;

        [Header("AI")]
        public AIType ai_type;              //人工智能算法
        public int ai_level = 10;           //AI level, 10=最強, 1=最弱

        [Header("Decks")]
        public DeckData[] free_decks;       //test環境下的牌組
        public DeckData[] starter_decks;    //當API啟用時，每個玩家可以選擇其中之一
        public DeckData[] ai_decks;         //單機模式時，AI會隨機選擇其中之一

        [Header("Scenes")]
        public string[] arena_list;         //遊戲場景列表

        [Header("Test")]
        public DeckData test_deck;          //用於直接從 Unity 遊戲場景啟動遊戲時
        public DeckData test_deck_ai;       //用於直接從 Unity 遊戲場景啟動遊戲時
        public bool ai_vs_ai;

        public int GetPlayerLevel(int xp)
        {
            return Mathf.FloorToInt(xp / 1000f) + 1;
        }

        public string GetRandomArena()
        {
            if (arena_list.Length > 0)
                return arena_list[Random.Range(0, arena_list.Length)];
            return "Game";
        }

        public string GetRandomAIDeck()
        {
            if (ai_decks.Length > 0)
                return ai_decks[Random.Range(0, ai_decks.Length)].id;
            return "";
        }

        public static GameplayData Get()
        {
            return DataLoader.Get().data;
        }
    }
}