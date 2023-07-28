﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    //包含跨網絡同步的所有遊戲狀態數據

    [System.Serializable]
    public class Game
    {
        public int nb_players = 2;
        public GameSettings settings;
        public string game_uid;

        //遊戲狀態
        public int first_player = 0;
        public int current_player = -1;
        public int turn_count = 0;
        public float turn_timer = 0f;

        public GameState state = GameState.Connecting;

        //玩家
        public Player[] players;

        //選擇器
        public SelectorType selector = SelectorType.None;
        public int selector_player = 0;
        public string selector_ability_id;
        public string selector_caster_uid;

        //其他值
        public Card last_played;
        public Card last_target;
        public Card last_killed;
        public Card ability_triggerer;
        public int rolled_value;

        //其他陣列 
        public HashSet<string> ability_played = new HashSet<string>();
        public HashSet<string> cards_attacked = new HashSet<string>();

        public Game() { }
        
        public Game(string uid, int nb_players)
        {
            this.game_uid = uid;
            this.nb_players = nb_players;
            players = new Player[nb_players];
            for (int i = 0; i < nb_players; i++)
                players[i] = new Player(i);
            settings = GameSettings.Default;
        }

        public virtual bool AreAllPlayersReady()
        {
            int ready = 0;
            foreach (Player player in players)
            {
                if (player.IsReady())
                    ready++;
            }
            return ready >= nb_players;
        }

        public virtual bool AreAllPlayersConnected()
        {
            int ready = 0;
            foreach (Player player in players)
            {
                if (player.IsConnected())
                    ready++;
            }
            return ready >= nb_players;
        }

        //檢查是否輪到其玩家
        public virtual bool IsPlayerTurn(Player player)
        {
            return IsPlayerActionTurn(player) || IsPlayerSelectorTurn(player);
        }

        public virtual bool IsPlayerActionTurn(Player player)
        {
            return player != null && current_player == player.player_id 
                && state == GameState.Play && selector == SelectorType.None;
        }

        public virtual bool IsPlayerSelectorTurn(Player player)
        {
            return player != null && selector_player == player.player_id 
                && state == GameState.Play && selector != SelectorType.None;
        }

        //檢查某張牌是否允許在空槽上打出
        public virtual bool CanPlayCard(Card card, Slot slot, bool skip_cost = false)
        {
            if (card == null)
                return false;

            Player player = GetPlayer(card.player_id);
            if (!skip_cost && !player.CanPayMana(card))
                return false; //無法支付法力
            if (!player.HasCard(player.cards_hand, card))
                return false; // 卡不在手

            if (card.CardData.IsBoardCard())
            {
                if (!slot.IsValid() || IsCardOnSlot(slot))
                    return false;   //槽位已被佔用
                if (Slot.GetP(card.player_id) != slot.p)
                    return false; //不能打在對手方
                return true;
            }
            if (card.CardData.IsRequireTarget())
            {
                return IsPlayTargetValid(card, slot); //檢查插槽上的播放目標
            }
            return true;
        }

        //檢查是否允許將卡移動到插槽
        public virtual bool CanMoveCard(Card card, Slot slot, bool skip_cost = false)
        {
            if (card == null || !slot.IsValid())
                return false;

            if (!card.CanMove(skip_cost))
                return false; //卡不能移動

            if (Slot.GetP(card.player_id) != slot.p)
                return false; //牌打錯了位置

            if (card.slot == slot)
                return false; //無法移動到同一個插槽

            Card slot_card = GetSlotCard(slot);
            if (slot_card != null)
                return false; //那裡已經有卡了

            return true;
        }

        //檢查是否允許卡牌攻擊玩家
        public virtual bool CanAttackTarget(Card attacker, Player target, bool skip_cost = false)
        {
            if(attacker == null || target == null)
                return false;

            if (!attacker.CanAttack(skip_cost))
                return false; //卡牌無法攻擊

            if (attacker.player_id == target.player_id)
                return false; //無法攻擊同一玩家

            if (!IsOnBoard(attacker) || !attacker.CardData.IsCharacter())
                return false; //卡未帶上面板

            if (target.HasStatusEffect(StatusType.Protected) && !attacker.HasStatus(StatusType.Flying))
                return false; //受到嘲諷保護

            return true;
        }

        //攻擊檢查判定（是否允許攻擊）
        public virtual bool CanAttackTarget(Card attacker, Card target, bool skip_cost = false)
        {
            if (attacker == null || target == null)
                return false;

            if (!attacker.CanAttack(skip_cost))
                return false; //卡牌無法攻擊

            if (attacker.player_id == target.player_id)
                return false; //玩家無法攻擊自己

            if (!IsOnBoard(attacker) || !IsOnBoard(target))
                return false; //卡牌沒有在面板上

            if (!attacker.CardData.IsCharacter() || !target.CardData.IsBoardCard())
                return false; //只有生物牌可以攻擊

            if (target.HasStatus(StatusType.Stealth))
                return false; //潛行生物無法被攻擊

            if (target.HasStatus(StatusType.Protected) && !attacker.HasStatus(StatusType.Flying))
                return false; //相鄰卡牌受到保護

            return true;
        }

        public virtual bool CanCastAbility(Card card, AbilityData ability)
        {
            if (card == null || !card.CanDoActivatedAbilities())
                return false; //此卡牌無法施放能力

            if (ability.trigger != AbilityTrigger.Activate)
                return false; //無效能力

            Player player = GetPlayer(card.player_id);
            if (!player.CanPayAbility(card, ability))
                return false; //此能力無法消耗法力值

            if (!ability.AreTriggerConditionsMet(this, card))
                return false; //不滿足條件

            return true;
        }

        //檢查玩家播放目標是否有效，播放目標是咒語需要直接拖到另一張卡上時的目標
        public virtual bool IsPlayTargetValid(Card caster, Player target, bool ai_check = false)
        {
            if (caster == null || target == null)
                return false;

            foreach (AbilityData ability in caster.CardData.abilities)
            {
                if (ability && ability.trigger == AbilityTrigger.OnPlay && ability.target == AbilityTarget.PlayTarget)
                {
                    bool can_target = ai_check ? ability.CanAiTarget(this, caster, target) : ability.CanTarget(this, caster, target);
                    if (!can_target)
                        return false;
                }
            }
            return true;
        }

        //檢查卡牌打出目標是否有效，打出目標是咒語需要直接拖到另一張卡上時的目標
        public virtual bool IsPlayTargetValid(Card caster, Card target, bool ai_check = false)
        {
            if (caster == null || target == null)
                return false;

            foreach (AbilityData ability in caster.CardData.abilities)
            {
                if (ability && ability.trigger == AbilityTrigger.OnPlay && ability.target == AbilityTarget.PlayTarget)
                {
                    bool can_target = ai_check ? ability.CanAiTarget(this, caster, target) : ability.CanTarget(this, caster, target);
                    if (!can_target)
                        return false;
                }
            }
            return true;
        }

        //檢查插槽播放目標是否有效，播放目標是咒語需要直接拖到另一張卡上時的目標
        public virtual bool IsPlayTargetValid(Card caster, Slot target, bool ai_check = false)
        {
            if (caster == null || target == null)
                return false;

            if (target.IsPlayerSlot())
                return IsPlayTargetValid(caster, GetPlayer(target.p)); //插槽 0,0，表示我們正在瞄準一個玩家

            Card slot_card = GetSlotCard(target);
            if (slot_card != null)
                return IsPlayTargetValid(caster, slot_card, ai_check); //插槽有卡，檢查該卡上的播放目標

            foreach (AbilityData ability in caster.CardData.abilities)
            {
                if (ability && ability.trigger == AbilityTrigger.OnPlay && ability.target == AbilityTarget.PlayTarget)
                {
                    bool can_target = ai_check ? ability.CanAiTarget(this, caster, target) : ability.CanTarget(this, caster, target);
                    if (!can_target)
                        return false;
                }
            }
            return true;
        }

        public Player GetPlayer(int id)
        {
            if (id >= 0 && id < players.Length)
                return players[id];
            return null;
        }

        public Player GetActivePlayer()
        {
            return GetPlayer(current_player);
        }

        public Player GetOpponentPlayer(int id)
        {
            int oid = id == 0 ? 1 : 0;
            return GetPlayer(oid);
        }

        public Card GetCard(string card_uid)
        {
            foreach (Player player in players)
            {
                Card acard = player.GetCard(card_uid);
                if (acard != null)
                    return acard;
            }
            return null;
        }

        public Card GetBoardCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_board)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        public Card GetHandCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_hand)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        public Card GetDeckCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_deck)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        public Card GetDiscardCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_discard)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        public Card GetSecretCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_secret)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        public Card GetTempCard(string card_uid)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_temp)
                {
                    if (card != null && card.uid == card_uid)
                        return card;
                }
            }
            return null;
        }

        public Card GetSlotCard(Slot slot)
        {
            foreach (Player player in players)
            {
                foreach (Card card in player.cards_board)
                {
                    if (card != null && card.slot == slot)
                        return card;
                }
            }
            return null;
        }
        
        public virtual Player GetRandomPlayer(System.Random rand)
        {
            Player player = GetPlayer(rand.NextDouble() < 0.5 ? 1 : 0);
            return player;
        }

        public virtual Card GetRandomBoardCard(System.Random rand)
        {
            Player player = GetRandomPlayer(rand);
            return player.GetRandomCard(player.cards_board, rand);
        }

        public virtual Slot GetRandomSlot(System.Random rand)
        {
            Player player = GetRandomPlayer(rand);
            return player.GetRandomSlot(rand);
        }

        public bool IsInHand(Card card)
        {
            return card != null && GetHandCard(card.uid) != null;
        }

        public bool IsOnBoard(Card card)
        {
            return card != null && GetBoardCard(card.uid) != null;
        }

        public bool IsInDeck(Card card)
        {
            return card != null && GetDeckCard(card.uid) != null;
        }

        public bool IsInDiscard(Card card)
        {
            return card != null && GetDiscardCard(card.uid) != null;
        }

        public bool IsInSecret(Card card)
        {
            return card != null && GetSecretCard(card.uid) != null;
        }

        public bool IsInTemp(Card card)
        {
            return card != null && GetTempCard(card.uid) != null;
        }

        public bool IsCardOnSlot(Slot slot)
        {
            return GetSlotCard(slot) != null;
        }

        public bool HasStarted()
        {
            return state != GameState.Connecting;
        }

        public bool HasEnded()
        {
            return state == GameState.GameEnded;
        }

        //與複製相同，但也實例化變量（慢得多）
        public static Game CloneNew(Game source)
        {
            Game game = new Game();
            Clone(source, game);
            return game;
        }

        //將所有變量複製到另一個變量中，主要由人工智能在構建預測分支時使用
        public static void Clone(Game source, Game dest)
        {
            dest.game_uid = source.game_uid;
            dest.nb_players = source.nb_players;
            dest.settings = source.settings;

            dest.first_player = source.first_player;
            dest.current_player = source.current_player;
            dest.turn_count = source.turn_count;
            dest.turn_timer = source.turn_timer;
            dest.state = source.state;

            if (dest.players == null)
            {
                dest.players = new Player[source.players.Length];
                for(int i=0; i< source.players.Length; i++)
                    dest.players[i] = new Player(i);
            }

            for (int i = 0; i < source.players.Length; i++)
                Player.Clone(source.players[i], dest.players[i]);

            dest.selector = source.selector;
            dest.selector_player = source.selector_player;
            dest.selector_caster_uid = source.selector_caster_uid;
            dest.selector_ability_id = source.selector_ability_id;
            dest.rolled_value = source.rolled_value;

            //一些值被註釋以進行優化，如果想要更準確更慢的AI，可以取消註釋
            //Card.CloneNull(source.last_played, ref dest.last_played);
            //Card.CloneNull(source.last_killed, ref dest.last_killed);
            //Card.CloneNull(source.last_target, ref dest.last_target);
            Card.CloneNull(source.ability_triggerer, ref dest.ability_triggerer);

            //CloneHash(source.ability_played, dest.ability_played);
            //CloneHash(source.cards_attacked, dest.cards_attacked);
        }

        public static void CloneHash(HashSet<string> source, HashSet<string> dest)
        {
            dest.Clear();
            foreach (string str in source)
                dest.Add(str);
        }
    }

    [System.Serializable]
    public enum GameState
    {
        Connecting = 0, //玩家未連接
        Starting = 1,  //玩家已準備好並已連接，遊戲正在設置

        StartTurn = 10, //回合開始效果
        Play = 20,      //播放步驟
        EndTurn = 30,   //回合結束效果

        GameEnded = 99,
    }

    [System.Serializable]
    public enum SelectorType
    {
        None = 0,
        SelectTarget = 10,
        SelectorCard = 20,
        SelectorChoice = 30,
    }
}