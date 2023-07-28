using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    /// <summary>
    /// AI玩家基類，其他AI繼承此
    /// </summary>

    public abstract class AIPlayer 
    {
        public int player_id;
        public int ai_level = 3;

        protected GameLogic gameplay;

        public virtual void Update()
        {
            //遊戲服務器調用腳本更新AI
            //覆蓋此啟動AI
        }

        public bool CanPlay()
        {
            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);
            bool can_play = game_data.IsPlayerTurn(player);
            return can_play && !gameplay.IsResolving();
        }

        public static AIPlayer Create(AIType type, GameLogic gameplay, int id, int level = 0)
        {
            if (type == AIType.Random)
                return new AIPlayerRandom(gameplay, id, level);
            if (type == AIType.MiniMax)
                return new AIPlayerMM(gameplay, id, level);
            return null;
        }
    }

    public enum AIType
    {
        Random = 0,      //最笨的AI，只會做隨機動作，測試卡牌用
        MiniMax = 10,    //使用 Minimax 算法和 alpha-beta 剪枝增強人工智能
    }
}
