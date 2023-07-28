using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 所有能力效果的基類，重寫 IsConditionMet 函數
    /// </summary>

    public class EffectData : ScriptableObject
    {
        public virtual void DoEffect(GameLogic logic, AbilityData ability, Card caster)
        {
            //服務器端遊戲邏輯
        }

        public virtual void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            //服務器端遊戲邏輯
        }

        public virtual void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            //服務器端遊戲邏輯
        }

        public virtual void DoEffect(GameLogic logic, AbilityData ability, Card caster, Slot target)
        {
            //服務器端遊戲邏輯
        }

        public virtual void DoEffect(GameLogic logic, AbilityData ability, Card caster, CardData target)
        {
            //服務器端遊戲邏輯
        }

        public virtual void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            //僅持續效果
        }

        public virtual void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            //僅持續效果
        }

        public virtual int GetAiValue(AbilityData ability)
        {
            return 0; //幫助AI知道這是正面還是負面的能力效果（返回1、0或-1）
        }
    }
}