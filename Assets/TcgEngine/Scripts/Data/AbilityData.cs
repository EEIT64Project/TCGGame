using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Gameplay;

namespace TcgEngine
{
    /// <summary>
    /// 定義所有能力數據
    /// </summary>

    [CreateAssetMenu(fileName = "ability", menuName = "TcgEngine/AbilityData", order = 4)]
    public class AbilityData : ScriptableObject
    {
        public string id;

        [Header("Trigger")]
        public AbilityTrigger trigger;             //該能力何時觸發？
        public ConditionData[] conditions_trigger; //觸發該能力的卡上檢查的條件（通常是施法者）

        [Header("Target")]
        public AbilityTarget target;               //誰是目標？
        public ConditionData[] conditions_target;  //檢查目標的條件以確定其是否有效
        public FilterData[] filters_target;  //檢查目標的條件以確定其是否有效

        [Header("Effect")]
        public EffectData[] effects;              //這是做什麼的？
        public StatusData[] status;               //通過該能力添加的狀態  
        public int value;                         //傳遞給效果的值（造成 X 點傷害）
        public int duration;                      //傳遞到效果的持續時間（通常用於狀態，0=永久）

        [Header("Chain/Choices")]
        public AbilityData[] chain_abilities;    //在此之後將觸發的能力

        [Header("Activated Ability")]
        public int mana_cost;                   //激活技能的法力消耗
        public bool exhaust;                    //激活能力的行動成本

        [Header("FX")]
        public GameObject board_fx;
        public GameObject caster_fx;
        public GameObject target_fx;
        public AudioClip cast_audio;
        public AudioClip target_audio;
        public bool charge_target;

        [Header("Text")]
        public string title;
        [TextArea(5, 7)]
        public string desc;

        public static List<AbilityData> ability_list = new List<AbilityData>();

        public static void Load(string folder = "")
        {
            if (ability_list.Count == 0)
                ability_list.AddRange(Resources.LoadAll<AbilityData>(folder));
        }

        public string GetTitle()
        {
            return title;
        }

        public string GetDesc()
        {
            return desc;
        }

        public string GetDesc(CardData card)
        {
            string dsc = desc;
            dsc = dsc.Replace("<name>", card.title);
            dsc = dsc.Replace("<value>", value.ToString());
            dsc = dsc.Replace("<duration>", duration.ToString());
            return dsc;
        }

        //觸發能力的一般條件
        public bool AreTriggerConditionsMet(Game data, Card caster)
        {
            return AreTriggerConditionsMet(data, caster, caster); //觸發者是施法者
        }

        //有些能力是由另一張牌（PlayOther）引起的，否則大多數時候觸發者是施法者，檢查觸發者的情況
        public bool AreTriggerConditionsMet(Game data, Card caster, Card trigger_card)
        {
            foreach (ConditionData cond in conditions_trigger)
            {
                if (cond != null)
                {
                    if (!cond.IsTriggerConditionMet(data, this, caster))
                        return false;
                    if (!cond.IsTargetConditionMet(data, this, caster, trigger_card))
                        return false;
                }
            }
            return true;
        }

        //有些能力是由玩家的動作引起的（攻擊玩家時OnFight），檢查該玩家的狀況
        public bool AreTriggerConditionsMet(Game data, Card caster, Player trigger_player)
        {
            foreach (ConditionData cond in conditions_trigger)
            {
                if (cond != null)
                {
                    if (!cond.IsTriggerConditionMet(data, this, caster))
                        return false;
                    if (!cond.IsTargetConditionMet(data, this, caster, trigger_player))
                        return false;
                }
            }
            return true;
        }

        //檢查卡目標是否有效
        public bool AreTargetConditionsMet(Game data, Card caster, Card target_card)
        {
            foreach (ConditionData cond in conditions_target)
            {
                if (cond != null && !cond.IsTargetConditionMet(data, this, caster, target_card))
                    return false;
            }
            return true;
        }

        //檢查玩家目標是否有效
        public bool AreTargetConditionsMet(Game data, Card caster, Player target_player)
        {
            foreach (ConditionData cond in conditions_target)
            {
                if (cond != null && !cond.IsTargetConditionMet(data, this, caster, target_player))
                    return false;
            }
            return true;
        }

        //檢查槽目標是否有效
        public bool AreTargetConditionsMet(Game data, Card caster, Slot target_slot)
        {
            foreach (ConditionData cond in conditions_target)
            {
                if (cond != null && !cond.IsTargetConditionMet(data, this, caster, target_slot))
                    return false;
            }
            return true;
        }

        //檢查卡數據目標是否有效
        public bool AreTargetConditionsMet(Game data, Card caster, CardData target_card)
        {
            foreach (ConditionData cond in conditions_target)
            {
                if (cond != null && !cond.IsTargetConditionMet(data, this, caster, target_card))
                    return false;
            }
            return true;
        }

        //CanTarget 與 AreTargetConditionsMet 類似，但僅適用於板上的目標，並具有額外的僅板條件
        public bool CanTarget(Game data, Card caster, Card target)
        {
            if (target.HasStatus(StatusType.Stealth))
                return false; //隱身

            if (target.HasStatus(StatusType.SpellImmunity))
                return false; //法術免疫

            bool condition_match = AreTargetConditionsMet(data, caster, target);
            return condition_match;
        }

        //可以目標檢查附加限制，用於 SelectTarget 或 PlayTarget 功能
        public bool CanTarget(Game data, Card caster, Player target)
        {
            bool condition_match = AreTargetConditionsMet(data, caster, target);
            return condition_match;
        }

        public bool CanTarget(Game data, Card caster, Slot target)
        {
            return AreTargetConditionsMet(data, caster, target); //插槽無附加條件
        }

        //人工智能根據效果是否積極而有額外的限制
        public bool CanAiTarget(Game data, Card caster, Card target_card)
        {
            return CanTarget(data, caster, target_card) && CanAiTarget(caster.player_id, target_card.player_id);
        }

        //人工智能根據效果是否積極而有額外的限制
        public bool CanAiTarget(Game data, Card caster, Player target_player)
        {
            return CanTarget(data, caster, target_player) && CanAiTarget(caster.player_id, target_player.player_id);
        }

        public bool CanAiTarget(Game data, Card caster, Slot target_slot)
        {
            return CanTarget(data, caster, target_slot); //無附加條件
        }

        public bool CanAiTarget(int caster_pid, int target_pid)
        {
            int ai_value = GetAiValue();
            if (ai_value > 0 && caster_pid != target_pid)
                return false; //正面效果，不針對他人
            if (ai_value < 0 && caster_pid == target_pid)
                return false; //負面影響，不要針對自己
            return true;
        }

        //檢查目標數組過濾後是否有目標，用於支持CardSelector中的過濾
        public bool IsCardSelectionValid(Game data, Card caster, Card target, ListSwap<Card> card_array = null)
        {
            List<Card> targets = GetCardTargets(data, caster, card_array);
            return targets.Contains(target); //過濾後卡片仍在數組中
        }

        public void DoEffects(GameLogic logic, Card caster)
        {
            foreach(EffectData effect in effects)
                effect?.DoEffect(logic, this, caster);
        }

        public void DoEffects(GameLogic logic, Card caster, Card target)
        {
            foreach (EffectData effect in effects)
                effect?.DoEffect(logic, this, caster, target);
            foreach(StatusData stat in status)
                target.AddStatus(stat, value, duration);
        }

        public void DoEffects(GameLogic logic, Card caster, Player target)
        {
            foreach (EffectData effect in effects)
                effect?.DoEffect(logic, this, caster, target);
            foreach (StatusData stat in status)
                target.AddStatus(stat, value, duration);
        }

        public void DoEffects(GameLogic logic, Card caster, Slot target)
        {
            foreach (EffectData effect in effects)
                effect?.DoEffect(logic, this, caster, target);
        }

        public void DoEffects(GameLogic logic, Card caster, CardData target)
        {
            foreach (EffectData effect in effects)
                effect?.DoEffect(logic, this, caster, target);
        }

        public void DoOngoingEffects(GameLogic logic, Card caster, Card target)
        {
            foreach (EffectData effect in effects)
                effect?.DoOngoingEffect(logic, this, caster, target);
            foreach (StatusData stat in status)
                target.AddOngoingStatus(stat, value);
        }

        public void DoOngoingEffects(GameLogic logic, Card caster, Player target)
        {
            foreach (EffectData effect in effects)
                effect?.DoOngoingEffect(logic, this, caster, target);
            foreach (StatusData stat in status)
                target.AddOngoingStatus(stat, value);
        }

        public bool HasEffect<T>() where T : EffectData
        {
            foreach (EffectData eff in effects)
            {
                if (eff != null && eff is T)
                    return true;
            }
            return false;
        }

        public int GetAiValue()
        {
            int total = 0;
            foreach (EffectData eff in effects)
                total += eff != null ? eff.GetAiValue(this) : 0;
            foreach (StatusData astatus in status)
                total += astatus != null ? astatus.hvalue : 0;
            foreach (AbilityData ability in chain_abilities)
                total += ability != null ? ability.GetAiValue() : 0;
            return total;
        }

        private void AddValidCards(Game data, Card caster, List<Card> source, List<Card> targets)
        {
            foreach (Card card in source)
            {
                if (AreTargetConditionsMet(data, caster, card))
                    targets.Add(card);
            }
        }

        //返回卡片目標，內存數組用於優化並避免分配新內存
        public List<Card> GetCardTargets(Game data, Card caster, ListSwap<Card> memory_array = null)
        {
            if (memory_array == null)
                memory_array = new ListSwap<Card>(); //運行緩慢

            List<Card> targets = memory_array.Get();

            if (target == AbilityTarget.Self)
            {
                if (AreTargetConditionsMet(data, caster, caster))
                    targets.Add(caster);
            }

            if (target == AbilityTarget.AllCardsBoard || target == AbilityTarget.SelectTarget)
            {
                foreach (Player player in data.players)
                {
                    foreach (Card card in player.cards_board)
                    {
                        if (AreTargetConditionsMet(data, caster, card))
                            targets.Add(card);
                    }
                }
            }

            if (target == AbilityTarget.AllCardsHand)
            {
                foreach (Player player in data.players)
                {
                    foreach (Card card in player.cards_hand)
                    {
                        if (AreTargetConditionsMet(data, caster, card))
                            targets.Add(card);
                    }
                }
            }

            if (target == AbilityTarget.AllCardsAllPiles || target == AbilityTarget.CardSelector)
            {
                foreach (Player player in data.players)
                {
                    AddValidCards(data, caster, player.cards_deck, targets);
                    AddValidCards(data, caster, player.cards_discard, targets);
                    AddValidCards(data, caster, player.cards_hand, targets);
                    AddValidCards(data, caster, player.cards_secret, targets);
                    AddValidCards(data, caster, player.cards_board, targets);
                    AddValidCards(data, caster, player.cards_temp, targets);
                }
            }

            if (target == AbilityTarget.LastPlayed)
            {
                Card target = data.last_played;
                if (target != null && AreTargetConditionsMet(data, caster, target))
                    targets.Add(target);
            }

            if (target == AbilityTarget.LastKilled)
            {
                Card target = data.last_killed;
                if (target != null && AreTargetConditionsMet(data, caster, target))
                    targets.Add(target);
            }

            if (target == AbilityTarget.LastTargeted)
            {
                Card target = data.last_target;
                if (target != null && AreTargetConditionsMet(data, caster, target))
                    targets.Add(target);
            }

            if (target == AbilityTarget.AbilityTriggerer)
            {
                Card target = data.ability_triggerer;
                if (target != null && AreTargetConditionsMet(data, caster, target))
                    targets.Add(target);
            }

            //過濾目標
            if (filters_target != null && targets.Count > 0)
            {
                foreach (FilterData filter in filters_target)
                {
                    if (filter != null)
                        targets = filter.FilterTargets(data, this, caster, targets, memory_array.GetOther(targets));
                }
            }

            return targets;
        }

        //返回玩家目標，memory_array用於優化並避免分配新內存
        public List<Player> GetPlayerTargets(Game data, Card caster, ListSwap<Player> memory_array = null)
        {
            if (memory_array == null)
                memory_array = new ListSwap<Player>(); //運行緩慢

            List<Player> targets = memory_array.Get();

            if (target == AbilityTarget.PlayerSelf)
            {
                Player player = data.GetPlayer(caster.player_id);
                targets.Add(player);
            }
            else if (target == AbilityTarget.PlayerOpponent)
            {
                for (int tp = 0; tp < data.players.Length; tp++)
                {
                    if (tp != caster.player_id)
                    {
                        Player oplayer = data.players[tp];
                        targets.Add(oplayer);
                    }
                }
            }
            else if (target == AbilityTarget.AllPlayers)
            {
                targets.AddRange(data.players);
            }

            //過濾目標
            if (filters_target != null && targets.Count > 0)
            {
                foreach (FilterData filter in filters_target)
                {
                    if (filter != null)
                        targets = filter.FilterTargets(data, this, caster, targets, memory_array.GetOther(targets));
                }
            }

            return targets;
        }

        //返回槽目標，memory_array用於優化並避免分配新內存
        public List<Slot> GetSlotTargets(Game data, Card caster, ListSwap<Slot> memory_array = null)
        {
            if (memory_array == null)
                memory_array = new ListSwap<Slot>(); //運行緩慢

            List<Slot> targets = memory_array.Get();

            if (target == AbilityTarget.AllSlots)
            {
                List<Slot> slots = Slot.GetAll();
                foreach (Slot slot in slots)
                {
                    if (AreTargetConditionsMet(data, caster, slot))
                        targets.Add(slot);
                }
            }

            //過濾目標
            if (filters_target != null && targets.Count > 0)
            {
                foreach (FilterData filter in filters_target)
                {
                    if (filter != null)
                        targets = filter.FilterTargets(data, this, caster, targets, memory_array.GetOther(targets));
                }
            }

            return targets;
        }

        public List<CardData> GetCardDataTargets(Game data, Card caster, ListSwap<CardData> memory_array = null)
        {
            if (memory_array == null)
                memory_array = new ListSwap<CardData>(); //運行緩慢

            List<CardData> targets = memory_array.Get();

            if (target == AbilityTarget.AllCardData)
            {
                foreach (CardData card in CardData.GetAll())
                {
                    if (AreTargetConditionsMet(data, caster, card))
                        targets.Add(card);
                }
            }

            return targets;
        }

        public bool IsSelector()
        {
            return target == AbilityTarget.SelectTarget || target == AbilityTarget.CardSelector || target == AbilityTarget.ChoiceSelector;
        }

        public static AbilityData Get(string id)
        {
            foreach (AbilityData ability in GetAll())
            {
                if (ability.id == id)
                    return ability;
            }
            return null;
        }

        public static List<AbilityData> GetAll()
        {
            return ability_list;
        }
    }


    public enum AbilityTrigger
    {
        None = 0,

        Ongoing = 2,  //始終處於活動狀態（不適用於所有效果）
        Activate = 5, //動作

        OnPlay = 10,  //打出時
        OnPlayOther = 12,  //當另一張牌打出時

        StartOfTurn = 20, //每一回合
        EndOfTurn = 22, //每一回合

        OnBeforeAttack = 30, //攻擊時、傷害前
        OnAfterAttack = 31, //攻擊時、受傷後如果還活著
        OnBeforeDefend = 32, //受到攻擊時、受到傷害之前
        OnAfterDefend = 33, //受到攻擊時、受傷後如果還活著
        OnKill = 35,        //在攻擊中殺死另一張卡時

        OnDeath = 40, //臨終時
        OnDeathOther = 42, //當另一個生物死去時
    }

    public enum AbilityTarget
    {
        None = 0,
        Self = 1,

        PlayerSelf = 4,
        PlayerOpponent = 5,
        AllPlayers = 7,

        AllCardsBoard = 10,
        AllCardsHand = 11,
        AllCardsAllPiles = 12,
        AllSlots = 15,
        AllCardData = 17,       //僅適用於卡創建效果

        PlayTarget = 20,        //在施展法術的同時選擇的目標（僅限法術）      
        AbilityTriggerer = 25,   //觸發陷阱的卡牌

        SelectTarget = 30,        //選擇面板上的卡牌、玩家或插槽
        CardSelector = 40,          //卡選擇器菜單
        ChoiceSelector = 50,        //選擇選擇器菜單

        LastPlayed = 70,            //最後打出的牌
        LastTargeted = 72,          //最後一張以能力為目標的卡牌
        LastKilled = 74,            //最後一張被殺死的牌

    }

}
