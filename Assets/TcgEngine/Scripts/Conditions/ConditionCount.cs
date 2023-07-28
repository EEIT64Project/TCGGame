using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    public enum ConditionPlayerType
    {
        Self = 0,
        Opponent = 1,
        Both = 2,
    }

    /// <summary>
    /// 觸發條件，計算您選擇的牌堆中的牌數（套牌/棄牌/手牌/面板...）
    /// 也可只計算特定類型/陣營/特質的卡牌
    /// </summary>

    [CreateAssetMenu(fileName = "condition", menuName = "TcgEngine/Condition/Count", order = 10)]
    public class ConditionCount : ConditionData
    {
        [Header("Count cards of type")]
        public ConditionPlayerType type;
        public PileType pile;
        public ConditionOperatorInt oper;
        public int value;

        [Header("Traits")]
        public CardType has_type;
        public TeamData has_team;
        public TraitData has_trait;

        public override bool IsTriggerConditionMet(Game data, AbilityData ability, Card caster)
        {
            int count = 0;
            if (type == ConditionPlayerType.Self || type == ConditionPlayerType.Both)
            {
                Player player =  data.GetPlayer(caster.player_id);
                count += CountPile(player, pile);
            }
            if (type == ConditionPlayerType.Opponent || type == ConditionPlayerType.Both)
            {
                Player player = data.GetOpponentPlayer(caster.player_id);
                count += CountPile(player, pile);
            }
            return CompareInt(count, oper, value);
        }

        private int CountPile(Player player, PileType pile)
        {
            List<Card> card_pile = null;

            if (pile == PileType.Hand)
                card_pile = player.cards_hand;

            if (pile == PileType.Board)
                card_pile = player.cards_board;

            if (pile == PileType.Deck)
                card_pile = player.cards_deck;

            if (pile == PileType.Discard)
                card_pile = player.cards_discard;

            if (pile == PileType.Secret)
                card_pile = player.cards_secret;

            if (pile == PileType.Temp)
                card_pile = player.cards_temp;

            if (card_pile != null)
            {
                int count = 0;
                foreach (Card card in card_pile)
                {
                    if (IsTrait(card))
                        count++;
                }
                return count;
            }
            return 0;
        }

        private bool IsTrait(Card card)
        {
            bool is_type = card.CardData.type == has_type || has_type == CardType.None;
            bool is_team = card.CardData.team == has_team || has_team == null;
            bool is_trait = card.HasTrait(has_trait) || has_trait == null;
            return (is_type && is_team && is_trait);
        }
    }
}