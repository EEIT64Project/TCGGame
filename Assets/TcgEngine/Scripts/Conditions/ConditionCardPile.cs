using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 檢查卡牌在哪一疊的條件（套牌/棄牌/手牌/牌面/秘密）
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardPile", order = 10)]
    public class ConditionCardPile : ConditionData
    {
        [Header("Card is in pile")]
        public PileType type;
        public ConditionOperatorBool oper;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            if (target == null)
                return false;

            if (type == PileType.Hand)
            {
                return CompareBool(data.IsInHand(target), oper);
            }

            if (type == PileType.Board)
            {
                return CompareBool(data.IsOnBoard(target), oper);
            }

            if (type == PileType.Deck)
            {
                return CompareBool(data.IsInDeck(target), oper);
            }

            if (type == PileType.Discard)
            {
                return CompareBool(data.IsInDiscard(target), oper);
            }

            if (type == PileType.Secret)
            {
                return CompareBool(data.IsInSecret(target), oper);
            }

            if (type == PileType.Temp)
            {
                return CompareBool(data.IsInTemp(target), oper);
            }

            return false;
        }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return false; //區分玩家
        }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return type == PileType.Board && target != Slot.None; //插槽始終在面板上
        }
    }
}