using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.AI
{
    /// <summary>
    /// AI決策的數值和計算
    /// Heuristic：代表面板上狀態的得分，高分有利於AI，低分有利於對手
    /// 動作得分：表示單個動作的得分，如果單個節點中的動作太多，則優先考慮動作
    /// Action Sort Order：確定動作單輪執行順序的值，以避免不同順序搜索相同的內容，按升序執行
    /// </summary>

    public class AIHeuristic
    {
        //---------- Heuristic 參數 -------------

        public int board_card_value = 10;       //面板上的持卡分數
        public int hand_card_value = 5;         //手牌上的持卡分數
        public int kill_value = 5;              //殺死卡牌的分數

        public int player_hp_value = 4;         //每個玩家HP得分
        public int card_attack_value = 3;       //面板上的卡牌攻擊得分
        public int card_hp_value = 2;           //面板上每張卡牌的HP得分
        public int card_status_value = 5;       //卡上每個狀態的分數（乘以 StatusData 的 h 值）

        //-----------

        private int ai_player_id;           //該AI的ID，設定玩家為0，AI為1
        private int ai_level;               //AI等級（設定10級最好，1級最差）
        private int heuristic_modifier;     //較低級別人工智能的隨機函式
        private System.Random random_gen;

        public AIHeuristic(int player_id, int level)
        {
            ai_player_id = player_id;
            ai_level = level;
            heuristic_modifier = GetHeuristicModifier();
            random_gen = new System.Random();
        }

        //計算完整函式（winscore + fullscore）
        public int CalculateHeuristic(Game data, NodeState node)
        {
            Player aiplayer = data.GetPlayer(ai_player_id);
            Player oplayer = data.GetOpponentPlayer(ai_player_id);
            int winscore = CalculateWinHeuristic(data, node, aiplayer, oplayer);
            return CalculateHeuristic(data, node, aiplayer, oplayer, winscore);
        }

        //如果已經預先計算了勝利分數，則計算完整的函式
        public int CalculateHeuristic(Game data, NodeState node, int winscore)
        {
            Player aiplayer = data.GetPlayer(ai_player_id);
            Player oplayer = data.GetOpponentPlayer(ai_player_id);
            return CalculateHeuristic(data, node, aiplayer, oplayer, winscore);
        }

        //如果已經預先計算了勝利分數，則計算完整的函式
        //返回 -10000 到 10000 之間的值（不要將其與勝利混淆）
        public int CalculateHeuristic(Game data, NodeState node, Player aiplayer, Player oplayer, int winscore)
        {
            int score = winscore;

            //面板狀態
            score += aiplayer.cards_board.Count * board_card_value;
            score += aiplayer.cards_hand.Count * hand_card_value;
            score += aiplayer.kill_count * kill_value;
            score += aiplayer.hp * player_hp_value;

            score -= oplayer.cards_board.Count * board_card_value;
            score -= oplayer.cards_hand.Count * hand_card_value;
            score -= oplayer.kill_count * kill_value;
            score -= oplayer.hp * player_hp_value;


            foreach (Card card in aiplayer.cards_board)
            {
                score += card.GetAttack() * card_attack_value;
                score += card.GetHP() * card_hp_value;

                foreach (CardStatus status in card.status)
                    score += status.StatusData.hvalue * card_status_value;
                foreach (CardStatus status in card.ongoing_status)
                    score += status.StatusData.hvalue * card_status_value;
            }
            foreach (Card card in oplayer.cards_board)
            {
                score -= card.GetAttack() * card_attack_value;
                score -= card.GetHP() * card_hp_value;

                foreach (CardStatus status in card.status)
                    score -= status.StatusData.hvalue * card_status_value;
                foreach (CardStatus status in card.ongoing_status)
                    score -= status.StatusData.hvalue * card_status_value;
            }

            if (heuristic_modifier > 0)
                score += random_gen.Next(-heuristic_modifier, heuristic_modifier);

            return score;
        }

        //僅計算勝利分數，如果已經有勝利，可以停止搜索路徑
        public int CalculateWinHeuristic(Game data, NodeState node)
        {
            Player aiplayer = data.GetPlayer(ai_player_id);
            Player oplayer = data.GetOpponentPlayer(ai_player_id);
            int score = CalculateWinHeuristic(data, node, aiplayer, oplayer);
            return score;
        }

        //僅計算勝利分數，如果已經有勝利，可以停止搜索路徑
        //返回大於 50000 或小於 -50000
        private int CalculateWinHeuristic(Game data, NodeState node, Player aiplayer, Player oplayer)
        {
            int score = 0;

            //Victories
            if (aiplayer.IsDead())
                score = -100000 + node.tdepth * 1000;
            if (oplayer.IsDead())
                score = 100000 - node.tdepth * 1000;

            return score;
        }

        //這計算的是單個動作的分數，非面板狀態
        //當單個節點中可能有太多操作時，只有具有最佳操作得分的操作才會被評估
        //確保返回正值
        public int CalculateActionScore(Game data, AIAction order)
        {
            if (order.type == GameAction.EndTurn)
                return 0; //Other orders are better

            if (order.type == GameAction.CancelSelect)
                return 0; //Other orders are better

            if (order.type == GameAction.CastAbility)
            {
                return 200;
            }

            if (order.type == GameAction.Attack)
            {
                Card card = data.GetCard(order.card_uid);
                Card target = data.GetCard(order.target_uid);
                int ascore = card.GetAttack() >= target.GetHP() ? 300 : 100;
                int oscore = target.GetAttack() >= card.GetHP() ? -200 : 0;
                return ascore + oscore + card.GetAttack() * 5 + target.GetAttack() * 5;
            }
            if (order.type == GameAction.AttackPlayer)
            {
                Card card = data.GetCard(order.card_uid);
                Player player = data.GetPlayer(order.target_player_id);
                int ascore = card.GetAttack() >= player.hp ? 500 : 200;
                return ascore + (card.GetAttack() * 10) - player.hp;
            }
            if (order.type == GameAction.PlayCard)
            {
                Player player = data.GetPlayer(ai_player_id);
                Card card = data.GetCard(order.card_uid);
                if (card.CardData.IsBoardCard())
                    return 200 + (card.GetMana() * 5) - (30 * player.cards_board.Count);
                else
                    return 200 + (card.GetMana() * 5);
            }

            if (order.type == GameAction.Move)
            {
                return 150;
            }

            return 100; //其他order比結束/取消更好
        }

        //同一回合內，動作只能按排序順序執行，確保返回大於0的正值，否則不會排序
        //可以防止計算 A->B->C B->C->A C->A->B 等的所有可能性。
        //如果兩個 AIAction 具有相同的排序值，或者存儲值為 0，ai 將測試所有排序變化（較慢）
        //讓AI可以在 1 回合內執行多個動作
        //暫定隨機排序
        public int CalculateActionSort(Game data, AIAction order)
        {
            if (order.type == GameAction.EndTurn)
                return 0; //結束回合動作一定可以執行，0表示任意順序
            if (data.selector != SelectorType.None)
                return 0; //選擇能力的操作不受排序影響

            Card card = data.GetCard(order.card_uid);
            Card target = order.target_uid != null ? data.GetCard(order.target_uid) : null;
            bool is_spell = !card.CardData.IsBoardCard();

            int type_sort = 0;
            if (order.type == GameAction.PlayCard && is_spell)
                type_sort = 1; //積極使用咒語類
            if (order.type == GameAction.CastAbility)
                type_sort = 2; //卡牌能力
            if (order.type == GameAction.Move)
                type_sort = 3; //移動
            if (order.type == GameAction.Attack)
                type_sort = 4; //攻擊
            if (order.type == GameAction.AttackPlayer)
                type_sort = 5; //攻擊玩家
            if (order.type == GameAction.PlayCard && !is_spell)
                type_sort = 7; //生物

            int card_sort = card.Hash % 100;
            int target_sort = target != null ? (target.Hash % 100) : 0;
            int sort = type_sort * 10000 + card_sort * 100 + target_sort + 1;
            return sort;
        }

        //較低級別的人工智能在函式中添加一個隨機數
        private int GetHeuristicModifier()
        {
            if (ai_level >= 10)
                return 0;
            if (ai_level == 9)
                return 5;
            if (ai_level == 8)
                return 10;
            if (ai_level == 7)
                return 20;
            if (ai_level == 6)
                return 30;
            if (ai_level == 5)
                return 40;
            if (ai_level == 4)
                return 50;
            if (ai_level == 3)
                return 75;
            if (ai_level == 2)
                return 100;
            if (ai_level <= 1)
                return 200;
            return 0;
        }

        //檢查該節點是否代表獲勝的玩家之一
        public bool IsWin(NodeState node)
        {
            return node.hvalue > 50000 || node.hvalue < -50000;
        }

    }
}
