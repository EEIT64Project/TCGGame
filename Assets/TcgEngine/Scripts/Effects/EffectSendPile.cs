using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    //將目標牌發送到您選擇的一堆（牌組/棄牌/手牌）
    //因為它需要一個插槽，所以無法使用發送到面板，使用 EffectPlay 來發送到面板
    //也不要從面板上發送到丟棄，因為不會觸發 OnKill 效果，而是使用 EffectDestroy

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/SendPile", order = 10)]
    public class EffectSendPile : EffectData
    {
        public PileType pile;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            Game data = logic.GetGameData();
            Player player = data.GetPlayer(target.player_id);

            if (pile == PileType.Deck)
            {
                player.RemoveCardFromAllGroups(target);
                player.cards_deck.Add(target);
            }

            if (pile == PileType.Hand)
            {
                player.RemoveCardFromAllGroups(target);
                player.cards_hand.Add(target);
            }

            if (pile == PileType.Discard)
            {
                player.RemoveCardFromAllGroups(target);
                player.cards_discard.Add(target);
            }

            if (pile == PileType.Temp)
            {
                player.RemoveCardFromAllGroups(target);
                player.cards_temp.Add(target);
            }
        }
    }

    public enum PileType
    {
        None = 0,
        Board = 10,
        Hand = 20,
        Deck = 30,
        Discard = 40,
        Secret = 50,
        Temp = 90,
    }

}
