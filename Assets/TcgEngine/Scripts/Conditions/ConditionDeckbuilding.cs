using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 檢查 CardData 是否是有效的牌組構建卡（不是召喚令牌）的條件
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/CardDeckbuilding", order = 10)]
    public class ConditionDeckbuilding : ConditionData
    {
        [Header("Card is Deckbuilding")]
        public ConditionOperatorBool oper;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return CompareBool(target.CardData.deckbuilding, oper);
        }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, CardData target)
        {
            return CompareBool(target.deckbuilding, oper);
        }
    }
}