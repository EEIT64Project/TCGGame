using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 耗盡或未耗盡一張卡牌的效果（意味著它不能再執行操作或將能夠執行另一操作）
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Exhaust", order = 10)]
    public class EffectExhaust : EffectData
    {
        public bool exhausted;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            target.exhausted = exhausted;
        }

        public override int GetAiValue(AbilityData ability)
        {
            return exhausted ? -1 : 1;
        }
    }
}