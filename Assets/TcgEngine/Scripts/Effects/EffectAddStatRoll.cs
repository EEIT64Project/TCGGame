using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 通過擲骰子的值添加或刪除基本卡牌/玩家統計數據（例如生命值、攻擊力、法力值）的效果
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/AddStatRoll", order = 10)]
    public class EffectAddStatRoll : EffectData
    {
        public EffectStatType type;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            Game data = logic.GetGameData();

            if (type == EffectStatType.HP)
            {
                target.hp += data.rolled_value;
                target.hp_max += data.rolled_value;
            }

            if (type == EffectStatType.Mana)
            {
                target.mana += data.rolled_value;
                target.mana_max += data.rolled_value;
                target.mana = Mathf.Clamp(target.mana, 0, GameplayData.Get().mana_max);
                target.mana_max = Mathf.Clamp(target.mana_max, 0, GameplayData.Get().mana_max);
            }
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Game data = logic.GetGameData();

            if (type == EffectStatType.Attack)
                target.attack += data.rolled_value;
            if (type == EffectStatType.HP)
                target.hp += data.rolled_value;
            if (type == EffectStatType.Mana)
                target.mana += data.rolled_value;
        }
    }
}