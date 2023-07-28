using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    //召喚一張全新卡牌的效果（不在任何人的牌組中）
    //並將其放置在面板上

    [CreateAssetMenu(fileName = "effect", menuName = "TcgEngine/Effect/Summon", order = 10)]
    public class EffectSummon : EffectData
    {
        public CardData summon;

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Player target)
        {
            logic.SummonCardHand(target.player_id, summon, caster.VariantData); //召喚到手牌
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Card target)
        {
            logic.SummonCard(caster.player_id, summon, caster.VariantData, target.slot); //假設目標剛剛被殺死，所以槽位是空的
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, Slot target)
        {
            logic.SummonCard(caster.player_id, summon, caster.VariantData, target);
        }

        public override void DoEffect(GameLogic logic, AbilityData ability, Card caster, CardData target)
        {
            logic.SummonCardHand(caster.player_id, target, caster.VariantData);
        }
    }
}