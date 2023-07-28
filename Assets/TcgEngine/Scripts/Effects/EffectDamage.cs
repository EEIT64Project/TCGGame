using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 對卡牌或玩家造成傷害的效果（失去生命值）
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Damage", order = 10)]
    public class EffectDamage : EffectData
    {
        public TraitData bonus_damage;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            int damage = GetDamage(logic.GameData, caster, ability.value);
            target.hp -= damage;
            target.hp = Mathf.Clamp(target.hp, 0, target.hp_max);
            //在回合結束時，CheckForWinner 將檢查玩家是否死亡
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            int damage = GetDamage(logic.GameData, caster, ability.value);
            logic.DamageCard(caster, target, damage);
        }

        private int GetDamage(Game data, Card caster, int value)
        {
            Player player = data.GetPlayer(caster.player_id);
            int damage = value + caster.GetTraitValue(bonus_damage) + player.GetTraitValue(bonus_damage);
            return damage;
        }

        public override int GetAiValue(AbilityData ability)
        {
            return -1;
        }
    }
}