using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 清除玩家卡的臨時數組
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/ClearTemp ", order = 10)]
    public class EffectClearTemp : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster)
        {
            Player player = logic.GameData.GetPlayer(caster.player_id);
            player.cards_temp.Clear();
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Player player = logic.GameData.GetPlayer(caster.player_id);
            player.cards_temp.Clear();
        }
    }
}