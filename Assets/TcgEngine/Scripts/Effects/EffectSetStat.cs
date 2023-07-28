using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 將基本統計數據（hp/attack/mana）設置為特定值的效果
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/SetStat", order = 10)]
    public class EffectSetStat : EffectData
    {
        public EffectStatType type;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            if (type == EffectStatType.HP)
            {
                target.hp = ability.value;
            }

            if (type == EffectStatType.Mana)
            {
                target.mana = ability.value;
                target.mana = Mathf.Clamp(target.mana, 0, GameplayData.Get().mana_max);
            }
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (type == EffectStatType.Attack)
                target.attack = ability.value;
            if (type == EffectStatType.Mana)
                target.mana = ability.value;
            if (type == EffectStatType.HP)
            {
                target.hp = ability.value;
                target.damage = 0;
            }
        }

        public override void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (type == EffectStatType.Attack)
                target.attack = ability.value;
            if (type == EffectStatType.HP)
                target.hp = ability.value;
            if (type == EffectStatType.Mana)
                target.mana = ability.value;
        }

        public override int GetAiValue(AbilityData ability)
        {
            if (type == EffectStatType.Mana)
                return 0; //法力值不明確，取決於目標（對玩家有利，對卡牌不利）

            if (ability.value <= 3)
                return -1; //設置為低值
            if (ability.value >= 7)
                return 1; //設置為高值
            return 0;
        }
    }
}