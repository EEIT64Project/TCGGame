using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 從 CardData 創建新卡的效果
    /// 用於發現效果
    /// </summary>

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Create", order = 10)]
    public class EffectCreate : EffectData
    {
        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, CardData target)
        {
            Player player = logic.GameData.GetPlayer(caster.player_id);
            Card card = Card.Create(target, caster.VariantData, caster.player_id);
            player.cards_all[card.uid] = card;
            player.cards_temp.Add(card);
        }
    }
}