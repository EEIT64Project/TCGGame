using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 將自定義統計數據設置為特定值的效果
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/SetStatCustom", order = 10)]
    public class EffectSetStatCustom : EffectData
    {
        public TraitData trait;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            target.SetTrait(trait.id, ability.value);
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            target.SetTrait(trait.id, ability.value);
        }

        public override void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            target.SetTrait(trait.id, ability.value);
        }

        public override void DoOngoingEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            target.SetTrait(trait.id, ability.value);
        }
    }
}