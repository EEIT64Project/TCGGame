using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 丟棄手牌的效果
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Discard", order = 10)]
    public class EffectDiscard : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            logic.DrawDiscardCard(target.player_id, ability.value);
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            logic.DiscardCard(target);
        }

        public override int GetAiValue(AbilityData ability)
        {
            return -1;
        }
    }
}