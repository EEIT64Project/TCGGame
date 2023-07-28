using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 添加或刪除卡/玩家自定義統計數據的效果
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