using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// Effect that adds or removes card/player custom stats
    /// </summary>
    
    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/AddStatCustom", order = 10)]
    public class EffectAddStatCustom : EffectData
    {
        public TraitData trait;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            target.AddTrait(trait.id, ability.value);
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            target.AddTrait(trait.id, ability.value);
        }

        public override void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            target.AddOngoingTrait(trait.id, ability.value);
        }

        public override void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            target.AddOngoingTrait(trait.id, ability.value);
        }
    }
}