using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 免費打出一張手牌的效果
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Play", order = 10)]
    public class EffectPlay : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Game game = logic.GetGameData();
            Player player = game.GetPlayer(caster.player_id);
            Slot slot = player.GetRandomEmptySlot(logic.GetRandom());

            player.RemoveCardFromAllGroups(target);
            player.cards_hand.Add(target);

            if (slot != Slot.None)
            {
                logic.PlayCard(target, slot, true);
            }
        }
    }
}