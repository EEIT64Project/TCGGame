using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 獲得/失去法力的效果（玩家）
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Mana", order = 10)]
    public class EffectMana : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            target.mana += ability.value;
            target.mana = Mathf.Clamp(target.mana, 0, GameplayData.Get().mana_max);
        }

        public override int GetAiValue(AbilityData ability)
        {
            return 1;
        }
    }
}