using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 消除狀態的效果，
    /// 如果公共字段為空，將刪除所有狀態
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/ClearStatus", order = 10)]
    public class EffectClearStatus : EffectData
    {
        public StatusData status;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            if (status != null)
                target.RemoveStatus(status.effect);
            else
                target.status_effects.Clear();
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            if (status != null)
                target.RemoveStatus(status.effect);
            else
                target.status.Clear();
        }
    }
}