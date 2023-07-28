﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 比較卡牌或玩家的自定義統計數據
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/StatCustom", order = 10)]
    public class ConditionStatCustom : ConditionData
    {
        [Header("Card stat is")]
        public TraitData trait;
        public ConditionOperatorInt oper;
        public int value;

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return CompareInt(target.GetTraitValue(trait.id), oper, value);
        }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return CompareInt(target.GetTraitValue(trait.id), oper, value);
        }
    }
}