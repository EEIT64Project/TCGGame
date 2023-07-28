using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 重定向攻擊的效果（通常由 OnBeforeAttack 或 OnBeforeDefend 觸發）
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/AttackRedirect", order = 10)]
    public class EffectAttackRedirect : EffectData
    {
        public EffectAttackerType attacker_type;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            Card attacker = GetAttacker(logic.GetGameData(), caster);
            if (attacker != null)
            {
                logic.RedirectAttack(attacker, target);
            }
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Card attacker = GetAttacker(logic.GetGameData(), caster);
            if (attacker != null)
            {
                logic.RedirectAttack(attacker, target);
            }
        }

        public Card GetAttacker(Game gdata, Card caster)
        {
            if (attacker_type == EffectAttackerType.Self)
                return caster;
            if (attacker_type == EffectAttackerType.AbilityTriggerer)
                return gdata.ability_triggerer;
            if (attacker_type == EffectAttackerType.LastPlayed)
                return gdata.last_played;
            if (attacker_type == EffectAttackerType.LastPlayed)
                return gdata.last_target;
            return null;
        }
    }
}