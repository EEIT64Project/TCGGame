using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 將一張卡轉變為另一張卡的效果
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Transform", order = 10)]
    public class EffectTransform : EffectData
    {
        public CardData transform_to;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            logic.TransformCard(target, transform_to);
        }
    }
}