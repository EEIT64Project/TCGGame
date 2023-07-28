using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// SlotDist 是從施法者到目標的行進距離
    /// 與 SlotRange 不同，SlotRange 只分別檢查每個 X、Y、P
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/SlotDist", order = 11)]
    public class ConditionSlotDist : ConditionData
    {
        [Header("Slot Distance")]
        public int distance = 1;
        public bool diagonals;
        
        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return IsTargetConditionMet(data, ability, caster, target.slot);
        }

        public override bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            Slot cslot = caster.slot;
            if (diagonals)
                return cslot.IsInDistance(target, distance);
            return cslot.IsInDistanceStraight(target, distance);
        }
    }
}