using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 治療卡牌或玩家的效果（生命值）
    /// 它無法恢復超過原始HP，可使用 AddStats 來超越原始HP
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Heal", order = 10)]
    public class EffectHeal : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            target.hp += ability.value;
            target.hp = Mathf.Clamp(target.hp, 0, target.hp_max);
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            logic.HealCard(target, ability.value);
        }

        public override int GetAiValue(AbilityData ability)
        {
            return 1;
        }
    }
}