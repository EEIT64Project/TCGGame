using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 抽卡效果
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Draw", order = 10)]
    public class EffectDraw : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            logic.DrawCard(target.player_id, ability.value);
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            logic.DrawCard(target.player_id, ability.value);
        }

        public override int GetAiValue(AbilityData ability)
        {
            return 1;
        }
    }
}