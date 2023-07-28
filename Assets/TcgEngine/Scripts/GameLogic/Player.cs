using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    //表示玩家在遊戲過程中的當前狀態（僅數據）

    [System.Serializable]
    public class Player
    {
        public int player_id;
        public string username;
        public string avatar;
        public string cardback;
        public string deck;
        public bool is_ai = false;
        public int ai_level;

        public bool connected = false; //連接到服務器和遊戲
        public bool ready = false;     //已發送所有玩家數據，準備開始比賽

        public int hp;
        public int hp_max;
        public int mana = 0;
        public int mana_max = 0;
        public int kill_count = 0;

        public Dictionary<string, Card> cards_all = new Dictionary<string, Card>();
        public Card hero = null;

        public List<Card> cards_deck = new List<Card>();
        public List<Card> cards_hand = new List<Card>();
        public List<Card> cards_board = new List<Card>();
        public List<Card> cards_discard = new List<Card>();
        public List<Card> cards_secret = new List<Card>();
        public List<Card> cards_temp = new List<Card>();

        public List<CardTrait> traits = new List<CardTrait>();
        public List<CardTrait> ongoing_traits = new List<CardTrait>();

        public List<CardStatus> ongoing_status = new List<CardStatus>();
        public List<CardStatus> status_effects = new List<CardStatus>();

        public List<OrderHistory> history_list = new List<OrderHistory>();

        public Player(int id) { this.player_id = id; }

        public bool IsReady() { return ready && cards_all.Count > 0; }
        public bool IsConnected() { return connected || is_ai; }

        public virtual void CleanOngoing() { ongoing_status.Clear(); ongoing_traits.Clear(); }

        //---- 卡牌 ---------

        public void AddCard(List<Card> card_list, Card card)
        {
            card_list.Add(card);
        }

        public void RemoveCard(List<Card> card_list, Card card)
        {
            card_list.Remove(card);
        }

        public virtual void RemoveCardFromAllGroups(Card card)
        {
            cards_deck.Remove(card);
            cards_hand.Remove(card);
            cards_board.Remove(card);
            cards_deck.Remove(card);
            cards_discard.Remove(card);
            cards_secret.Remove(card);
            cards_temp.Remove(card);
        }
        
        public virtual Card GetRandomCard(List<Card> card_list, System.Random rand)
        {
            if (card_list.Count > 0)
                return card_list[rand.Next(0, card_list.Count)];
            return null;
        }

        public bool HasCard(List<Card> card_list, Card card)
        {
            return card_list.Contains(card);
        }

        public Card GetHandCard(string uid)
        {
            foreach (Card card in cards_hand)
            {
                if (card.uid == uid)
                    return card;
            }
            return null;
        }

        public Card GetBoardCard(string uid)
        {
            foreach (Card card in cards_board)
            {
                if (card.uid == uid)
                    return card;
            }
            return null;
        }

        public Card GetDeckCard(string uid)
        {
            foreach (Card card in cards_deck)
            {
                if (card.uid == uid)
                    return card;
            }
            return null;
        }

        public Card GetDiscardCard(string uid)
        {
            foreach (Card card in cards_discard)
            {
                if (card.uid == uid)
                    return card;
            }
            return null;
        }

        public Card GetSlotCard(Slot slot)
        {
            foreach (Card card in cards_board)
            {
                if (card != null && card.slot == slot)
                    return card;
            }
            return null;
        }

        public Card GetCard(string uid)
        {
            if (uid != null)
            {
                bool valid = cards_all.TryGetValue(uid, out Card card);
                if (valid)
                    return card;
            }
            return null;
        }

        public bool IsOnBoard(Card card)
        {
            return card != null && GetBoardCard(card.uid) != null;
        }


        //---- 面板空槽 ---------

        public Slot GetRandomSlot(System.Random rand)
        {
            return Slot.GetRandom(player_id, rand);
        }

        public virtual Slot GetRandomEmptySlot(System.Random rand)
        {
            List<Slot> valid = GetEmptySlots();
            if (valid.Count > 0)
                return valid[rand.Next(0, valid.Count)];
            return Slot.None;
        }

        public List<Slot> GetEmptySlots()
        {
            List<Slot> valid = new List<Slot>();
            foreach (Slot slot in Slot.GetAll(player_id))
            {
                Card slot_card = GetSlotCard(slot);
                if (slot_card == null)
                    valid.Add(slot);
            }
            return valid;
        }

        //------ 自定義特徵/統計數據 ---------

        public void SetTrait(string id, int value)
        {
            CardTrait trait = GetTrait(id);
            if (trait != null)
            {
                trait.value = value;
            }
            else
            {
                trait = new CardTrait(id, value);
                traits.Add(trait);
            }
        }

        public void AddTrait(string id, int value)
        {
            CardTrait trait = GetTrait(id);
            if (trait != null)
                trait.value += value;
            else
                SetTrait(id, value);
        }

        public void AddOngoingTrait(string id, int value)
        {
            CardTrait trait = GetOngoingTrait(id);
            if (trait != null)
            {
                trait.value += value;
            }
            else
            {
                trait = new CardTrait(id, value);
                ongoing_traits.Add(trait);
            }
        }

        public void RemoveTrait(string id)
        {
            for (int i = traits.Count - 1; i >= 0; i--)
            {
                if (traits[i].id == id)
                    traits.RemoveAt(i);
            }
        }

        public CardTrait GetTrait(string id)
        {
            foreach (CardTrait trait in traits)
            {
                if (trait.id == id)
                    return trait;
            }
            return null;
        }

        public CardTrait GetOngoingTrait(string id)
        {
            foreach (CardTrait trait in ongoing_traits)
            {
                if (trait.id == id)
                    return trait;
            }
            return null;
        }

        public List<CardTrait> GetAllTraits()
        {
            List<CardTrait> all_traits = new List<CardTrait>();
            all_traits.AddRange(traits);
            all_traits.AddRange(ongoing_traits);
            return all_traits;
        }

        public int GetTraitValue(TraitData trait)
        {
            if (trait != null)
                return GetTraitValue(trait.id);
            return 0;
        }

        public virtual int GetTraitValue(string id)
        {
            int val = 0;
            CardTrait stat1 = GetTrait(id);
            CardTrait stat2 = GetOngoingTrait(id);
            if (stat1 != null)
                val += stat1.value;
            if (stat2 != null)
                val += stat2.value;
            return val;
        }

        public bool HasTrait(TraitData trait)
        {
            if (trait != null)
                return HasTrait(trait.id);
            return false;
        }

        public bool HasTrait(string id)
        {
            foreach (CardTrait trait in traits)
            {
                if (trait.id == id)
                    return true;
            }
            return false;
        }

        //---- 狀態 ---------

        public void AddStatus(StatusData status, int value, int duration)
        {
            if (status != null)
                AddStatus(status.effect, value, duration);
        }

        public void AddOngoingStatus(StatusData status, int value)
        {
            if (status != null)
                AddOngoingStatus(status.effect, value);
        }

        public void AddStatus(StatusType effect, int value, int duration)
        {
            if (effect != StatusType.None)
            {
                CardStatus status = GetStatus(effect);
                if (status == null)
                {
                    status = new CardStatus(effect, value, duration);
                    status_effects.Add(status);
                }
                else
                {
                    status.value += value;
                    status.duration = Mathf.Max(status.duration, duration);
                    status.permanent = status.permanent || duration == 0;
                }
            }
        }

        public void AddOngoingStatus(StatusType effect, int value)
        {
            if (effect != StatusType.None)
            {
                CardStatus status = GetOngoingStatus(effect);
                if (status == null)
                {
                    status = new CardStatus(effect, value, 0);
                    ongoing_status.Add(status);
                }
                else
                {
                    status.value += value;
                }
            }
        }

        public void RemoveStatus(StatusType effect)
        {
            for (int i = status_effects.Count - 1; i >= 0; i--)
            {
                if (status_effects[i].type == effect)
                    status_effects.RemoveAt(i);
            }
        }

        public CardStatus GetStatus(StatusType effect)
        {
            foreach (CardStatus status in status_effects)
            {
                if (status.type == effect)
                    return status;
            }
            return null;
        }

        public CardStatus GetOngoingStatus(StatusType effect)
        {
            foreach (CardStatus status in ongoing_status)
            {
                if (status.type == effect)
                    return status;
            }
            return null;
        }

        public List<CardStatus> GetAllStatus()
        {
            List<CardStatus> all_status = new List<CardStatus>();
            all_status.AddRange(status_effects);
            all_status.AddRange(ongoing_status);
            return all_status;
        }

        public bool HasStatusEffect(StatusType effect)
        {
            return GetStatus(effect) != null || GetOngoingStatus(effect) != null;
        }

        public virtual int GetStatusEffectValue(StatusType effect)
        {
            CardStatus status1 = GetStatus(effect);
            CardStatus status2 = GetOngoingStatus(effect);
            return status1.value + status2.value;
        }

        //---- 歷史資訊 ---------

        public void AddHistory(ushort type, Card card)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, Card target)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.target_uid = target.uid;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, Player target)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.target_id = target.player_id;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, AbilityData ability)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.ability_id = ability.id;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, AbilityData ability, Card target)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.ability_id = ability.id;
            order.target_uid = target.uid;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, AbilityData ability, Player target)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.ability_id = ability.id;
            order.target_id = target.player_id;
            history_list.Add(order);
        }

        public void AddHistory(ushort type, Card card, AbilityData ability, Slot target)
        {
            OrderHistory order = new OrderHistory();
            order.type = type;
            order.card_id = card.card_id;
            order.card_uid = card.uid;
            order.ability_id = ability.id;
            order.slot = target;
            history_list.Add(order);
        }


        //---- 動作檢查 ---------

        public virtual bool CanPayMana(Card card)
        {
            return mana >= card.GetMana();
        }

        public virtual void PayMana(Card card)
        {
            mana -= card.GetMana();
        }

        public virtual bool CanPayAbility(Card card, AbilityData ability)
        {
            bool exhaust = !card.exhausted || !ability.exhaust;
            return exhaust && mana >= ability.mana_cost;
        }

        public virtual bool IsDead()
        {
            if (cards_hand.Count == 0 && cards_board.Count == 0 && cards_deck.Count == 0)
                return true;
            if (hp <= 0)
                return true;
            return false;
        }

        //--------------------

        //將所有玩家變量複製到另一個變量中，主要由 AI 在構建預測分支時使用
        public static void Clone(Player source, Player dest)
        {
            dest.player_id = source.player_id;
            dest.is_ai = source.is_ai;
            dest.ai_level = source.ai_level;
            //dest.username = source.username;
            //dest.avatar = source.avatar;
            //dest.deck = source.deck;
            //dest.connected = source.connected;
            //dest.ready = source.ready;

            dest.hp = source.hp;
            dest.hp_max = source.hp_max;
            dest.mana = source.mana;
            dest.mana_max = source.mana_max;
            dest.kill_count = source.kill_count;

            Card.CloneNull(source.hero, ref dest.hero);
            Card.CloneDict(source.cards_all, dest.cards_all);
            Card.CloneListRef(dest.cards_all, source.cards_board, dest.cards_board);
            Card.CloneListRef(dest.cards_all, source.cards_hand, dest.cards_hand);
            Card.CloneListRef(dest.cards_all, source.cards_deck, dest.cards_deck);
            Card.CloneListRef(dest.cards_all, source.cards_discard, dest.cards_discard);
            Card.CloneListRef(dest.cards_all, source.cards_secret, dest.cards_secret);
            Card.CloneListRef(dest.cards_all, source.cards_temp, dest.cards_temp);

            CardStatus.CloneList(source.status_effects, dest.status_effects);
            CardStatus.CloneList(source.ongoing_status, dest.ongoing_status);
        }
    }

    [System.Serializable]
    public class OrderHistory
    {
        public ushort type;
        public string card_id;
        public string card_uid;
        public string target_uid;
        public string ability_id;
        public int target_id;
        public Slot slot;
    }
}