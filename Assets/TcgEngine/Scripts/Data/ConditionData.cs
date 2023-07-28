using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    /// <summary>
    /// 所有能力條件的基類，重寫 IsConditionMet 函數
    /// </summary>

    public class ConditionData : ScriptableObject
    {
        public virtual bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            return true; //Override this，適用於任何目標，始終選中
        }

        public virtual bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Card target)
        {
            return true; //Override this，目標卡
        }

        public virtual bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Player target)
        {
            return true; //Override this，條件定位玩家
        }

        public virtual bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, Slot target)
        {
            return true; //Override this，定位槽
        }

        public virtual bool IsTargetConditionMet(Game data, AbilityData ability, Card caster, CardData target)
        {
            return true; //Override this，以獲得創建新卡的效果
        }

        public bool CompareBool(bool condition, ConditionOperatorBool oper)
        {
            if (oper == ConditionOperatorBool.IsFalse)
                return !condition;
            return condition;
        }

        public bool CompareInt(int ival1, ConditionOperatorInt oper, int ival2)
        {
            if (oper == ConditionOperatorInt.Equal)
            {
                return ival1 == ival2;
            }
            if (oper == ConditionOperatorInt.NotEqual)
            {
                return ival1 != ival2;
            }
            if (oper == ConditionOperatorInt.GreaterEqual)
            {
                return ival1 >= ival2;
            }
            if (oper == ConditionOperatorInt.LessEqual)
            {
                return ival1 <= ival2;
            }
            if (oper == ConditionOperatorInt.Greater)
            {
                return ival1 > ival2;
            }
            if (oper == ConditionOperatorInt.Less)
            {
                return ival1 < ival2; ;
            }
            return false;
        }
    }

    public enum ConditionOperatorInt
    {
        Equal,
        NotEqual,
        GreaterEqual,
        LessEqual,
        Greater,
        Less,
    }

    public enum ConditionOperatorBool
    {
        IsTrue,
        IsFalse,
    }
}