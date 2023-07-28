using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.AI;

namespace TcgEngine
{
    /// <summary>
    /// 將能力的目標類別與實際目標（卡牌、玩家或插槽）進行比較的條件
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/Player", order = 10)]
    public class ConditionTarget : ConditionData
    {
        [Header("Target is of type")]
        public ConditionTargetType type;
        public ConditionOperatorBool oper;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return CompareBool(type == ConditionTargetType.Card, oper); //是否卡牌
        }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return CompareBool(type == ConditionTargetType.Player, oper); //是否玩家
        }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return CompareBool(type == ConditionTargetType.Slot, oper); //是否玩家
        }
    }

    public enum ConditionTargetType
    {
        None = 0,
        Card = 10,
        Player = 20,
        Slot = 30,
    }
}