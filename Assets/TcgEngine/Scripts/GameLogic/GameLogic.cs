using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;

namespace TcgEngine.Gameplay
{
    /// <summary>
    /// Execute and resolves game rules and logic
    /// </summary>

    public class GameLogic
    {
        public UnityAction onGameStart;
        public UnityAction<Player> onGameEnd;          //Winner

        public UnityAction onTurnStart;
        public UnityAction onTurnPlay;
        public UnityAction onTurnEnd;

        public UnityAction<Card, Slot> onCardPlayed;      
        public UnityAction<Card, Slot> onCardSummoned;
        public UnityAction<Card, Slot> onCardMoved;
        public UnityAction<Card> onCardTransformed;
        public UnityAction<Card> onCardDiscarded;
        public UnityAction<int> onCardDrawn;
        public UnityAction<int> onRollValue;

        public UnityAction<AbilityData, Card> onAbilityStart;        
        public UnityAction<AbilityData, Card, Card> onAbilityTargetCard;  //Ability, Caster, Target
        public UnityAction<AbilityData, Card, Player> onAbilityTargetPlayer;
        public UnityAction<AbilityData, Card, Slot> onAbilityTargetSlot;
        public UnityAction<AbilityData, Card> onAbilityEnd;

        public UnityAction<Card, Card> onAttackStart;  //Attacker, Defender
        public UnityAction<Card, Card> onAttackEnd;     //Attacker, Defender
        public UnityAction<Card, Player> onAttackPlayerStart;
        public UnityAction<Card, Player> onAttackPlayerEnd;

        public UnityAction<Card, Card> onSecretTrigger;    //Secret, Triggerer
        public UnityAction<Card, Card> onSecretResolve;    //Secret, Triggerer

        public UnityAction onSelectorStart;
        public UnityAction onSelectorSelect;

        private Game game_data;

        private ResolveQueue resolve_queue;
        
        private System.Random random = new System.Random();

        private ListSwap<Card> card_array = new ListSwap<Card>();
        private ListSwap<Player> player_array = new ListSwap<Player>();
        private ListSwap<Slot> slot_array = new ListSwap<Slot>();
        private ListSwap<CardData> card_data_array = new ListSwap<CardData>();

        public GameLogic(bool is_instant)
        {
            //is_instant ignores all gameplay delays and process everything immediately, needed for AI prediction
            resolve_queue = new ResolveQueue(null, is_instant); 
        }

        public GameLogic(Game game)
        {
            game_data = game;
            resolve_queue = new ResolveQueue(game, false);
        }

        public virtual void SetData(Game game)
        {
            game_data = game;
            resolve_queue.SetData(game);
        }

        public virtual void Update(float delta)
        {
            resolve_queue.Update(delta);
        }

        //----- Turn Phases ----------

        public virtual void StartGame()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            //Choose first player
            game_data.state = GameState.Starting;
            game_data.first_player = random.NextDouble() < 0.5 ? 0 : 1;
            game_data.current_player = game_data.first_player;
            game_data.turn_count = 1;

            //Adventure settings
            LevelData level = null;
            if (game_data.settings.game_type == GameType.Adventure)
            {
                level = LevelData.Get(game_data.settings.level);
                if (level != null && level.first_player == LevelFirst.Player)
                    game_data.first_player = 0;
                if (level != null && level.first_player == LevelFirst.AI)
                    game_data.first_player = 1;
                game_data.current_player = game_data.first_player;
            }

            //Init each player
            foreach (Player player in game_data.players)
            {
                //Puzzle level deck
                DeckPuzzleData pdeck = null;
                if(level != null)
                    pdeck = DeckPuzzleData.Get(player.deck);

                //Hp / mana
                player.hp_max = pdeck != null ? pdeck.start_hp : GameplayData.Get().hp_start;
                player.hp = player.hp_max;
                player.mana_max = pdeck != null ? pdeck.start_mana : GameplayData.Get().mana_start;
                player.mana = player.mana_max;

                //Draw starting cards
                int dcards = pdeck != null ? pdeck.start_cards : GameplayData.Get().cards_start;
                DrawCard(player.player_id, dcards);

                //Add coin second player
                bool is_random = level == null || level.first_player == LevelFirst.Random;
                if (is_random && player.player_id != game_data.first_player && GameplayData.Get().second_bonus != null)
                {
                    Card card = Card.Create(GameplayData.Get().second_bonus, VariantData.GetDefault(), player.player_id);
                    player.cards_all[card.uid] = card;
                    player.cards_hand.Add(card);
                }
            }

            //Start state
            onGameStart?.Invoke();

            StartTurn();
        }
		
        public virtual void StartTurn()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            ClearTurnData();
            game_data.state = GameState.StartTurn;
            onTurnStart?.Invoke();

            Player player = game_data.GetActivePlayer();

            //Cards draw
            if (game_data.turn_count > 1 || player.player_id != game_data.first_player)
            {
                DrawCard(player.player_id, GameplayData.Get().cards_per_turn);
            }

            //Mana 
            player.mana_max += GameplayData.Get().mana_per_turn;
            player.mana_max = Mathf.Min(player.mana_max, GameplayData.Get().mana_max);
            player.mana = player.mana_max;

            //Turn timer and history
            game_data.turn_timer = GameplayData.Get().turn_duration;
            player.history_list.Clear();

            //Player poison
            if (player.HasStatusEffect(StatusType.Poisoned))
                player.hp -= player.GetStatusEffectValue(StatusType.Poisoned);

            if (player.hero != null)
                player.hero.Refresh();

            //Refresh Cards and Status Effects
            for (int i = player.cards_board.Count - 1; i >= 0; i--)
            {
                Card card = player.cards_board[i];

                if(!card.HasStatus(StatusType.Sleep))
                    card.Refresh();

                if (card.HasStatus(StatusType.Poisoned))
                    DamageCard(card, card.GetStatusValue(StatusType.Poisoned));
            }

            //Ongoing Abilities
            UpdateOngoingAbilities();

            //StartTurn Abilities
            TriggerPlayerCardsAbilityType(player, AbilityTrigger.StartOfTurn);

            resolve_queue.AddCallback(StartPlayPhase);
            resolve_queue.ResolveAll(0.2f);
        }

        public virtual void StartNextTurn()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            game_data.current_player = (game_data.current_player + 1) % game_data.nb_players;
            
            if (game_data.current_player == game_data.first_player)
                game_data.turn_count++;

            CheckForWinner();
            StartTurn();
        }

        public virtual void StartPlayPhase()
        {
            if (game_data.state == GameState.GameEnded)
                return;

            game_data.state = GameState.Play;
            onTurnPlay?.Invoke();
        }

        public virtual void EndTurn()
        {
            if (game_data.state != GameState.Play)
                return;

            game_data.selector = SelectorType.None;
            game_data.state = GameState.EndTurn;

            //Reduce status effects with duration
            foreach (Player aplayer in game_data.players)
            {
                foreach (Card card in aplayer.cards_board)
                {
                    card.ReduceStatusDurations();
                }
            }

            //End of turn abilities
            Player player = game_data.GetActivePlayer();
            TriggerPlayerCardsAbilityType(player, AbilityTrigger.EndOfTurn);

            onTurnEnd?.Invoke();

            resolve_queue.AddCallback(StartNextTurn);
            resolve_queue.ResolveAll(0.2f);
        }

        //End game with winner
        public virtual void EndGame(int winner)
        {
            if (game_data.state != GameState.GameEnded)
            {
                game_data.state = GameState.GameEnded;
                game_data.current_player = winner; //Winner player
                Player player = game_data.GetPlayer(winner);
                onGameEnd?.Invoke(player);
            }
        }

        //Progress to the next step/phase 
        public virtual void NextStep()
        {
            if (game_data.selector != SelectorType.None)
            {
                CancelSelection();
            }
            else if (game_data.state == GameState.Play)
            {
                EndTurn();
            }
        }

        //Check if a player is winning the game, if so end the game
        //Change or edit this function for a new win condition
        protected virtual void CheckForWinner()
        {
            int count_alive = 0;
            Player alive = null;
            foreach (Player player in game_data.players)
            {
                if (!player.IsDead())
                {
                    alive = player;
                    count_alive++;
                }
            }

            if (count_alive == 0)
            {
                EndGame(-1); //Everyone is dead, Draw
            }
            else if (count_alive == 1)
            {
                EndGame(alive.player_id); //Player win
            }
        }

        protected virtual void ClearTurnData()
        {
            game_data.selector = SelectorType.None;
            resolve_queue.Clear();
            card_array.Clear();
            player_array.Clear();
            slot_array.Clear();
            card_data_array.Clear();
            game_data.last_played = null;
            game_data.last_killed = null;
            game_data.last_target = null;
            game_data.ability_triggerer = null;
            game_data.ability_played.Clear();
            game_data.cards_attacked.Clear();
        }

        //--- Setup ------

        //Set deck using a Deck in Resources
        public virtual void SetPlayerDeck(int player_id, DeckData deck)
        {
            Player player = game_data.GetPlayer(player_id);
            player.cards_all.Clear();
            player.cards_deck.Clear();
            player.deck = deck.id;

            VariantData variant = VariantData.GetDefault();
            if (deck.hero != null)
            {
                player.hero = Card.Create(deck.hero, variant, player.player_id);
                player.cards_all[player.hero.uid] = player.hero;
            }

            foreach (CardData card in deck.cards)
            {
                if (card != null)
                {
                    Card acard = Card.Create(card, variant, player.player_id);
                    player.cards_all[acard.uid] = acard;
                    player.cards_deck.Add(acard);
                }
            }

            DeckPuzzleData puzzle = deck as DeckPuzzleData;

            //Board cards
            if (puzzle != null)
            {
                foreach (DeckCardSlot card in puzzle.board_cards)
                {
                    Card acard = Card.Create(card.card, variant, player.player_id);
                    acard.slot = new Slot(card.slot, Slot.GetP(player_id));
                    player.cards_all[acard.uid] = acard;
                    player.cards_board.Add(acard);
                }
            }

            //Shuffle deck
            if(puzzle == null || !puzzle.dont_shuffle_deck)
                ShuffleDeck(player.cards_deck);
        }

        //Set deck using custom deck in save file or database
        public virtual void SetPlayerDeck(int player_id, UserDeckData deck)
        {
            SetPlayerDeck(player_id, deck.tid, deck.hero, deck.cards);
        }

        public virtual void SetPlayerDeck(int player_id, string deck_id, string hero, string[] cards)
        {
            Player player = game_data.GetPlayer(player_id);

            player.cards_all.Clear();
            player.cards_deck.Clear();
            player.deck = deck_id;

            CardData hdata = UserCardData.GetCardData(hero);
            VariantData hvariant = UserCardData.GetCardVariant(hero);
            if (hdata != null && !string.IsNullOrEmpty(hero))
            {
                player.hero = Card.Create(hdata, hvariant, player.player_id);
                player.cards_all[player.hero.uid] = player.hero;
            }

            foreach (string tid in cards)
            {
                CardData icard = UserCardData.GetCardData(tid);
                VariantData variant = UserCardData.GetCardVariant(tid);

                Card acard = Card.Create(icard, variant, player.player_id);
                player.cards_all[acard.uid] = acard;
                player.cards_deck.Add(acard);
            }

            //Shuffle deck
            ShuffleDeck(player.cards_deck);
        }

        //---- Gameplay Actions --------------

        public virtual void PlayCard(Card card, Slot slot, bool skip_cost = false)
        {
            if (card == null)
                return;

            Player player = game_data.GetPlayer(card.player_id);
            
            if (game_data.CanPlayCard(card, slot, skip_cost))
            {
                //Cost
                if (!skip_cost)
                    player.PayMana(card);

                //Play card
                player.RemoveCardFromAllGroups(card);
                card.Cleanse();

                //Add to board
                CardData icard = card.CardData;
                if (icard.IsBoardCard())
                {
                    player.cards_board.Add(card);
                    card.slot = slot;
                    card.SetCard(icard, card.VariantData);      //Reset all stats to default
                    card.exhausted = true; //Cant attack first turn
                }
                else if (icard.IsSecret())
                {
                    player.cards_secret.Add(card);
                }
                else
                {
                    player.cards_discard.Add(card);
                    card.slot = slot; //Save slot in case spell has PlayTarget
                }

                //History
                if(!icard.IsSecret())
                    player.AddHistory(GameAction.PlayCard, card);

                //Update ongoing effects
                game_data.last_played = card;
                UpdateOngoingAbilities();

                //Trigger abilities
                TriggerSecrets(AbilityTrigger.OnPlayOther, card); //After playing card
                TriggerCardAbilityType(AbilityTrigger.OnPlay, card);
                TriggerOtherCardsAbilityType(AbilityTrigger.OnPlayOther, card);

                onCardPlayed?.Invoke(card, slot);
                resolve_queue.ResolveAll(0.3f);
            }
        }

        public virtual void MoveCard(Card card, Slot slot, bool skip_cost = false)
        {
            if (card == null)
                return;

            Player player = game_data.GetPlayer(card.player_id);
            Card slot_card = game_data.GetSlotCard(slot);
            if (slot_card != null || !slot.IsValid())
                return; //Cant move to already occipied slot

            if (game_data.CanMoveCard(card, slot, skip_cost))
            {
                card.slot = slot;

                //Moving doesn't really have any effect in demo so can be done indefinitely
                //if(!skip_cost)
                    //card.exhausted = true;
                //card.RemoveStatus(StatusEffect.Stealth);
                //player.AddHistory(GameAction.Move, card);

                UpdateOngoingAbilities();

                onCardMoved?.Invoke(card, slot);
                resolve_queue.ResolveAll(0.2f);
            }
        }

        public virtual void CastAbility(Card card, AbilityData iability)
        {
            if (card == null || iability == null)
                return;

            Player player = game_data.GetPlayer(card.player_id);
            CardData icard = card.CardData;
            if (icard != null)
            {
                if (iability != null && game_data.CanCastAbility(card, iability))
                {
                    if (iability.target != AbilityTarget.SelectTarget)
                        player.AddHistory(GameAction.CastAbility, card, iability);
                    card.RemoveStatus(StatusType.Stealth);
                    TriggerCardAbility(iability, card);
                    resolve_queue.ResolveAll();
                }
            }
        }

        public virtual void AttackTarget(Card attacker, Card target, bool skip_cost = false)
        {
            if (attacker == null || target == null)
                return;

            if (!game_data.CanAttackTarget(attacker, target, skip_cost))
                return;

            Player player = game_data.GetPlayer(attacker.player_id);
            player.AddHistory(GameAction.Attack, attacker, target);

            //Trigger before attack abilities
            TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, attacker, target);
            TriggerCardAbilityType(AbilityTrigger.OnBeforeDefend, target, attacker);
            TriggerSecrets(AbilityTrigger.OnBeforeAttack, attacker);
            TriggerSecrets(AbilityTrigger.OnBeforeDefend, target);

            //Resolve attack
            resolve_queue.AddAttack(attacker, target, ResolveAttack, skip_cost);
            resolve_queue.ResolveAll();
        }

        protected virtual void ResolveAttack(Card attacker, Card target, bool skip_cost)
        {
            onAttackStart?.Invoke(attacker, target);

            attacker.RemoveStatus(StatusType.Stealth);
            UpdateOngoingAbilities();

            resolve_queue.AddAttack(attacker, target, ResolveAttackHit, skip_cost);
            resolve_queue.ResolveAll(0.3f);
        }

        protected virtual void ResolveAttackHit(Card attacker, Card target, bool skip_cost)
        {
            //Count attack damage
            int datt1 = attacker.GetAttack();
            int datt2 = target.GetAttack();

            //Damage Cards
            DamageCard(attacker, target, datt1);
            DamageCard(target, attacker, datt2);

            //Save attack and exhaust
            if (!skip_cost)
                ExhaustBattle(attacker);

            //Recalculate bonus
            UpdateOngoingAbilities();

            //Abiltiies
            bool att_board = game_data.IsOnBoard(attacker);
            bool def_board = game_data.IsOnBoard(target);
            if (att_board)
                TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, attacker, target);
            if (def_board)
                TriggerCardAbilityType(AbilityTrigger.OnAfterDefend, target, attacker);
            if (att_board)
                TriggerSecrets(AbilityTrigger.OnAfterAttack, attacker);
            if (def_board)
                TriggerSecrets(AbilityTrigger.OnAfterDefend, target);

            onAttackEnd?.Invoke(attacker, target);
            CheckForWinner();

            resolve_queue.ResolveAll(0.2f);
        }

        public virtual void AttackPlayer(Card attacker, Player target, bool skip_cost = false)
        {
            if (attacker == null || target == null)
                return;

            if (!game_data.CanAttackTarget(attacker, target, skip_cost))
                return;

            Player player = game_data.GetPlayer(attacker.player_id);
            player.AddHistory(GameAction.AttackPlayer, attacker, target);

            //Resolve abilities
            TriggerSecrets(AbilityTrigger.OnBeforeAttack, attacker);
            TriggerCardAbilityType(AbilityTrigger.OnBeforeAttack, attacker, target);

            //Resolve attack
            resolve_queue.AddAttack(attacker, target, ResolveAttackPlayer, skip_cost);
            resolve_queue.ResolveAll();
        }

        protected virtual void ResolveAttackPlayer(Card attacker, Player target, bool skip_cost)
        {
            onAttackPlayerStart?.Invoke(attacker, target);

            attacker.RemoveStatus(StatusType.Stealth);
            UpdateOngoingAbilities();

            resolve_queue.AddAttack(attacker, target, ResolveAttackPlayerHit, skip_cost);
            resolve_queue.ResolveAll(0.3f);
        }

        protected virtual void ResolveAttackPlayerHit(Card attacker, Player target, bool skip_cost)
        {
            //Damage player
            int datt1 = attacker.GetAttack();
            target.hp -= datt1;
            target.hp = Mathf.Clamp(target.hp, 0, target.hp_max);

            //Save attack and exhaust
            if (!skip_cost)
                ExhaustBattle(attacker);

            //Recalculate bonus
            UpdateOngoingAbilities();

            if (game_data.IsOnBoard(attacker))
                TriggerCardAbilityType(AbilityTrigger.OnAfterAttack, attacker, target);

            TriggerSecrets(AbilityTrigger.OnAfterAttack, attacker);
            
            onAttackPlayerEnd?.Invoke(attacker, target);
            CheckForWinner();

            resolve_queue.ResolveAll(0.2f);
        }

        //Exhaust after battle
        public virtual void ExhaustBattle(Card attacker)
        {
            bool attacked_before = game_data.cards_attacked.Contains(attacker.uid);
            game_data.cards_attacked.Add(attacker.uid);
            bool attack_again = attacker.HasStatus(StatusType.Fury) && !attacked_before;
            attacker.exhausted = !attack_again;
        }

        //Redirect attack to a new target
        public virtual void RedirectAttack(Card attacker, Card new_target)
        {
            foreach (AttackQueueElement att in resolve_queue.GetAttackQueue())
            {
                if (att.attacker.uid == attacker.uid)
                {
                    att.target = new_target;
                    att.ptarget = null;
                    att.callback = ResolveAttack;
                    att.pcallback = null;
                }
            }
        }

        public virtual void RedirectAttack(Card attacker, Player new_target)
        {
            foreach (AttackQueueElement att in resolve_queue.GetAttackQueue())
            {
                if (att.attacker.uid == attacker.uid)
                {
                    att.ptarget = new_target;
                    att.target = null;
                    att.pcallback = ResolveAttackPlayer;
                    att.callback = null;
                }
            }
        }

        public virtual void ShuffleDeck(List<Card> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                Card temp = cards[i];
                int randomIndex = random.Next(i, cards.Count);
                cards[i] = cards[randomIndex];
                cards[randomIndex] = temp;
            }
        }

        public virtual void DrawCard(int player_id, int nb = 1)
        {
            Player player = game_data.GetPlayer(player_id);
            for (int i = 0; i < nb; i++)
            {
                if (player.cards_deck.Count > 0 && player.cards_hand.Count < GameplayData.Get().cards_max)
                {
                    Card card = player.cards_deck[0];
                    player.cards_deck.RemoveAt(0);
                    player.cards_hand.Add(card);
                }
            }

            onCardDrawn?.Invoke(nb);
        }

        //Put a card from deck into discard
        public virtual void DrawDiscardCard(int player_id, int nb = 1)
        {
            Player player = game_data.GetPlayer(player_id);
            for (int i = 0; i < nb; i++)
            {
                if (player.cards_deck.Count > 0)
                {
                    Card card = player.cards_deck[0];
                    player.cards_deck.RemoveAt(0);
                    player.cards_discard.Add(card);
                }
            }
        }

        //Summon copy of an exiting card
        public virtual Card SummonCopy(int player_id, Card copy, Slot slot)
        {
            CardData icard = copy.CardData;
            return SummonCard(player_id, icard, copy.VariantData, slot);
        }

        //Summon copy of an exiting card into hand
        public virtual Card SummonCopyHand(int player_id, Card copy)
        {
            CardData icard = copy.CardData;
            return SummonCardHand(player_id, icard, copy.VariantData);
        }

        //Create a new card and send it to the board
        public virtual Card SummonCard(int player_id, CardData card, VariantData variant, Slot slot)
        {
            if (!slot.IsValid())
                return null;

            if (game_data.GetSlotCard(slot) != null)
                return null;

            Card acard = SummonCardHand(player_id, card, variant);
            PlayCard(acard, slot, true);

            onCardSummoned?.Invoke(acard, slot);

            return acard;
        }

        //Create a new card and send it to your hand
        public virtual Card SummonCardHand(int player_id, CardData card, VariantData variant)
        {
            string uid = "s_" + GameTool.GenerateRandomID();
            Player player = game_data.GetPlayer(player_id);
            Card acard = Card.Create(card, variant, player.player_id, uid);
            player.cards_all[acard.uid] = acard;
            player.cards_hand.Add(acard);
            return acard;
        }

        //Transform card into another one
        public virtual Card TransformCard(Card card, CardData transform_to)
        {
            card.SetCard(transform_to, card.VariantData);

            onCardTransformed?.Invoke(card);

            return card;
        }

        //Change owner of a card
        public virtual void ChangeOwner(Card card, Player owner)
        {
            if (card.player_id != owner.player_id)
            {
                Player powner = game_data.GetPlayer(card.player_id);
                powner.RemoveCardFromAllGroups(card);
                powner.cards_all.Remove(card.uid);
                owner.cards_all[card.uid] = card;
                card.player_id = owner.player_id;
            }
        }

        //Heal a card
        public virtual void HealCard(Card target, int value)
        {
            if (target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return;

            target.damage -= value;
            target.damage = Mathf.Max(target.damage, 0);
        }

        //Generic damage that doesnt come from another card
        public virtual void DamageCard(Card target, int value)
        {
            if(target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return; //Invincible

            if (target.HasStatus(StatusType.SpellImmunity))
                return; //Spell immunity

            target.damage += value;

            if (target.GetHP() <= 0)
                DiscardCard(target);
        }

        //Damage a card with attacker/caster
        public virtual void DamageCard(Card attacker, Card target, int value)
        {
            if (attacker == null || target == null)
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return; //Invincible

            if (target.HasStatus(StatusType.SpellImmunity) && attacker.CardData.type != CardType.Character)
                return; //Spell immunity

            //Shell
            bool doublelife = target.HasStatus(StatusType.Shell);
            if (doublelife)
            {
                target.RemoveStatus(StatusType.Shell);
                return;
            }

            //Armor
            if (target.HasStatus(StatusType.Armor))
                value = Mathf.Max(value - target.GetStatusValue(StatusType.Armor), 0);

            int extra = value - target.GetHP();
            target.damage += value;

            //Trample
            Player tplayer = game_data.GetPlayer(target.player_id);
            if (extra > 0 && attacker.HasStatus(StatusType.Trample))
                tplayer.hp -= extra;

            //Remove sleep on damage
            target.RemoveStatus(StatusType.Sleep);

            //Deathtouch
            if (value > 0 && attacker.HasStatus(StatusType.Deathtouch) && target.CardData.type == CardType.Character)
                KillCard(attacker, target);

            //Kill on 0 hp
            if (target.GetHP() <= 0)
                KillCard(attacker, target);
        }

        //A card that kills another card
        public virtual void KillCard(Card attacker, Card target)
        {
            if (attacker == null || target == null)
                return;

            if (!game_data.IsOnBoard(target))
                return;

            if (target.HasStatus(StatusType.Invincibility))
                return;

            Player pattacker = game_data.GetPlayer(attacker.player_id);
            if (attacker.player_id != target.player_id)
                pattacker.kill_count++;

            game_data.last_killed = target;
            DiscardCard(target);

            TriggerCardAbilityType(AbilityTrigger.OnKill, attacker, target);
        }

        //Send card into discard
        public virtual void DiscardCard(Card card)
        {
            if (card == null)
                return;

            if (game_data.IsInDiscard(card))
                return; //Already discarded

            CardData icard = card.CardData;
            Player player = game_data.GetPlayer(card.player_id);
            bool was_on_board = game_data.IsOnBoard(card);

            //Remove card from board and add to discard
            player.RemoveCardFromAllGroups(card);
            player.cards_discard.Add(card);

            if (was_on_board)
            {
                //Trigger on death abilities
                TriggerCardAbilityType(AbilityTrigger.OnDeath, card);
                TriggerOtherCardsAbilityType(AbilityTrigger.OnDeathOther, card);
            }

            card.Cleanse();
            onCardDiscarded?.Invoke(card);
        }

        public int RollRandomValue(int dice)
        {
            return RollRandomValue(1, dice + 1);
        }

        public virtual int RollRandomValue(int min, int max)
        {
            game_data.rolled_value = random.Next(min, max);
            onRollValue?.Invoke(game_data.rolled_value);
            resolve_queue.SetDelay(1f);
            return game_data.rolled_value;
        }

        //--- Abilities --

        public virtual void TriggerCardAbilityType(AbilityTrigger type, Card caster, Card triggerer = null)
        {
            foreach (AbilityData iability in caster.CardData.abilities)
            {
                if (iability && iability.trigger == type)
                {
                    TriggerCardAbility(iability, caster, triggerer);
                }
            }
        }

        public virtual void TriggerOtherCardsAbilityType(AbilityTrigger type, Card triggerer)
        {
            foreach (Player oplayer in game_data.players)
            {
                if(oplayer.hero != null)
                    TriggerCardAbilityType(type, oplayer.hero, triggerer);

                foreach (Card card in oplayer.cards_board)
                    TriggerCardAbilityType(type, card, triggerer);
            }
        }

        public virtual void TriggerPlayerCardsAbilityType(Player player, AbilityTrigger type)
        {
            if (player.hero != null)
                TriggerCardAbilityType(type, player.hero, player.hero);

            foreach (Card card in player.cards_board)
                TriggerCardAbilityType(type, card, card);
        }

        public virtual void TriggerCardAbilityType(AbilityTrigger type, Card caster, Player triggerer)
        {
            foreach (AbilityData iability in caster.CardData.abilities)
            {
                if (iability && iability.trigger == type)
                {
                    TriggerCardAbility(iability, caster, triggerer);
                }
            }
        }
        
        public virtual void TriggerCardAbility(AbilityData iability, Card caster, Card triggerer = null)
        {
            Card trigger_card = triggerer != null ? triggerer : caster; //Triggerer is the caster if not set
            if (!caster.HasStatus(StatusType.Silenced) && iability.AreTriggerConditionsMet(game_data, caster, trigger_card))
            {
                resolve_queue.AddAbility(iability, caster, triggerer, ResolveCardAbility);
            }
        }

        public virtual void TriggerCardAbility(AbilityData iability, Card caster, Player triggerer)
        {
            if (!caster.HasStatus(StatusType.Silenced) && iability.AreTriggerConditionsMet(game_data, caster, triggerer))
            {
                resolve_queue.AddAbility(iability, caster, caster, ResolveCardAbility);
            }
        }

        //Resolve a card ability, may stop to ask for target
        protected virtual void ResolveCardAbility(AbilityData iability, Card caster, Card triggerer)
        {
            if (!caster.CanDoAbilities())
                return; //Silenced card cant cast

            //Debug.Log("Trigger Ability " + iability.id + " : " + caster.card_id);

            Player player = game_data.GetPlayer(caster.player_id);
            onAbilityStart?.Invoke(iability, caster);
            game_data.ability_triggerer = triggerer;

            bool is_selector = ResolveCardAbilitySelector(iability, caster);
            if (is_selector)
                return; //Wait for player to select

            ResolveCardAbilityPlayTarget(iability, caster);
            ResolveCardAbilityPlayers(iability, caster);
            ResolveCardAbilityCards(iability, caster);
            ResolveCardAbilitySlots(iability, caster);
            ResolveCardAbilityCardData(iability, caster);
            ResolveCardAbilityNoTarget(iability, caster);
            AfterAbilityResolved(iability, caster);
        }

        protected virtual bool ResolveCardAbilitySelector(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.SelectTarget)
            {
                //Wait for target
                GoToSelectTarget(iability, caster);
                return true;
            }
            else if (iability.target == AbilityTarget.CardSelector)
            {
                GoToSelectorCard(iability, caster);
                return true;
            }
            else if (iability.target == AbilityTarget.ChoiceSelector)
            {
                GoToSelectorChoice(iability, caster);
                return true;
            }
            return false;
        }

        protected virtual void ResolveCardAbilityPlayTarget(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.PlayTarget)
            {
                Slot slot = caster.slot;
                Card slot_card = game_data.GetSlotCard(slot);
                if (slot.IsPlayerSlot())
                {
                    Player tplayer = game_data.GetPlayer(slot.p);
                    if (iability.CanTarget(game_data, caster, tplayer))
                        ResolveEffectTarget(iability, caster, tplayer);
                }
                else if (slot_card != null)
                {
                    if (iability.CanTarget(game_data, caster, slot_card))
                        ResolveEffectTarget(iability, caster, slot_card);
                }
                else
                {
                    if (iability.CanTarget(game_data, caster, slot))
                        ResolveEffectTarget(iability, caster, slot);
                }
            }
        }

        protected virtual void ResolveCardAbilityPlayers(AbilityData iability, Card caster)
        {
            //Get Player Targets based on conditions
            List<Player> targets = iability.GetPlayerTargets(game_data, caster, player_array);

            //Resolve effects
            foreach (Player target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilityCards(AbilityData iability, Card caster)
        {
            //Get Cards Targets based on conditions
            List<Card> targets = iability.GetCardTargets(game_data, caster, card_array);

            //Resolve effects
            foreach (Card target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilitySlots(AbilityData iability, Card caster)
        {
            //Get Slot Targets based on conditions
            List<Slot> targets = iability.GetSlotTargets(game_data, caster, slot_array);

            //Resolve effects
            foreach (Slot target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilityCardData(AbilityData iability, Card caster)
        {
            //Get Cards Targets based on conditions
            List<CardData> targets = iability.GetCardDataTargets(game_data, caster, card_data_array);

            //Resolve effects
            foreach (CardData target in targets)
            {
                ResolveEffectTarget(iability, caster, target);
            }
        }

        protected virtual void ResolveCardAbilityNoTarget(AbilityData iability, Card caster)
        {
            if (iability.target == AbilityTarget.None)
                iability.DoEffects(this, caster);
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Player target)
        {
            iability.DoEffects(this, caster, target);

            onAbilityTargetPlayer?.Invoke(iability, caster, target);
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Card target)
        {
            iability.DoEffects(this, caster, target);

            onAbilityTargetCard?.Invoke(iability, caster, target);
            game_data.last_target = target;
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, Slot target)
        {
            iability.DoEffects(this, caster, target);

            onAbilityTargetSlot?.Invoke(iability, caster, target);
        }

        protected virtual void ResolveEffectTarget(AbilityData iability, Card caster, CardData target)
        {
            iability.DoEffects(this, caster, target);
        }

        protected virtual void AfterAbilityResolved(AbilityData iability, Card caster)
        {
            Player player = game_data.GetPlayer(caster.player_id);

            //Add to played
            game_data.ability_played.Add(iability.id);

            //Pay cost
            if (iability.trigger == AbilityTrigger.Activate)
            {
                player.mana -= iability.mana_cost;
                caster.exhausted = caster.exhausted || iability.exhaust;
            }

            //Recalculate and clear
            UpdateOngoingAbilities();
            CheckForWinner();

            //Chain ability
            if (iability.target != AbilityTarget.ChoiceSelector && game_data.state != GameState.GameEnded)
            {
                foreach (AbilityData chain_ability in iability.chain_abilities)
                {
                    if (chain_ability != null)
                    {
                        TriggerCardAbility(chain_ability, caster);
                    }
                }
            }

            onAbilityEnd?.Invoke(iability, caster);
        }

        //This function is called often to update status/stats affected by ongoing abilities
        //It basically first reset the bonus to 0 (CleanOngoing) and then recalculate it to make sure it it still present
        //Only cards in hand and on board are updated in this way
        public virtual void UpdateOngoingAbilities()
        {
            Profiler.BeginSample("Update Ongoing");
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                player.CleanOngoing();

                for (int c = 0; c < player.cards_board.Count; c++)
                    player.cards_board[c].CleanOngoing();

                for (int c = 0; c < player.cards_hand.Count; c++)
                    player.cards_hand[c].CleanOngoing();
            }

            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                UpdateOngoingAbilities(player, player.hero);  //Remove this line if hero is on the board

                for (int c = 0; c < player.cards_board.Count; c++)
                {
                    Card card = player.cards_board[c];
                    UpdateOngoingAbilities(player, card);
                }
            }

            //Stats bonus
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                for(int c=0; c<player.cards_board.Count; c++)
                {
                    Card card = player.cards_board[c];

                    //Taunt effect
                    if (card.HasStatus(StatusType.Protection))
                    {
                        player.AddOngoingStatus(StatusType.Protected, 0);

                        for (int tc = 0; tc < player.cards_board.Count; tc++)
                        {
                            Card tcard = player.cards_board[tc];
                            if (!tcard.HasStatus(StatusType.Protection) && !tcard.HasStatus(StatusType.Protected))
                            {
                                tcard.AddOngoingStatus(StatusType.Protected, 0);
                            }
                        }
                    }

                    //Status bonus
                    foreach (CardStatus status in card.status)
                        AddOngoingStatusBonus(card, status);
                    foreach (CardStatus status in card.ongoing_status)
                        AddOngoingStatusBonus(card, status);
                }
            }

            //Kill stuff with 0 hp
            for (int p = 0; p < game_data.players.Length; p++)
            {
                Player player = game_data.players[p];
                for (int i = player.cards_board.Count - 1; i >= 0; i--)
                {
                    Card card = player.cards_board[i];
                    if (card.GetHP() <= 0)
                        DiscardCard(card);
                }
            }

            Profiler.EndSample();
        }

        protected virtual void UpdateOngoingAbilities(Player player, Card card)
        {
            if (card == null || !card.CanDoAbilities())
                return;

            CardData icaster = card.CardData;
            for (int a = 0; a < icaster.abilities.Length; a++)
            {
                AbilityData ability = icaster.abilities[a];
                if (ability != null && ability.trigger == AbilityTrigger.Ongoing && ability.AreTriggerConditionsMet(game_data, card))
                {
                    if (ability.target == AbilityTarget.Self)
                    {
                        if (ability.AreTargetConditionsMet(game_data, card, card))
                        {
                            ability.DoOngoingEffects(this, card, card);
                        }
                    }

                    if (ability.target == AbilityTarget.PlayerSelf)
                    {
                        if (ability.AreTargetConditionsMet(game_data, card, player))
                        {
                            ability.DoOngoingEffects(this, card, player);
                        }
                    }

                    if (ability.target == AbilityTarget.AllPlayers || ability.target == AbilityTarget.PlayerOpponent)
                    {
                        for (int tp = 0; tp < game_data.players.Length; tp++)
                        {
                            if (ability.target == AbilityTarget.AllPlayers || tp != player.player_id)
                            {
                                Player oplayer = game_data.players[tp];
                                if (ability.AreTargetConditionsMet(game_data, card, oplayer))
                                {
                                    ability.DoOngoingEffects(this, card, oplayer);
                                }
                            }
                        }
                    }

                    if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsHand || ability.target == AbilityTarget.AllCardsBoard)
                    {
                        for (int tp = 0; tp < game_data.players.Length; tp++)
                        {
                            //Looping on all cards is very slow, since there are no ongoing effects that works out of board/hand we loop on those only
                            Player tplayer = game_data.players[tp];

                            //Hand Cards
                            if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsHand)
                            {
                                for (int tc = 0; tc < tplayer.cards_hand.Count; tc++)
                                {
                                    Card tcard = tplayer.cards_hand[tc];
                                    if (ability.AreTargetConditionsMet(game_data, card, tcard))
                                    {
                                        ability.DoOngoingEffects(this, card, tcard);
                                    }
                                }
                            }

                            //Board Cards
                            if (ability.target == AbilityTarget.AllCardsAllPiles || ability.target == AbilityTarget.AllCardsBoard)
                            {
                                for (int tc = 0; tc < tplayer.cards_board.Count; tc++)
                                {
                                    Card tcard = tplayer.cards_board[tc];
                                    if (ability.AreTargetConditionsMet(game_data, card, tcard))
                                    {
                                        ability.DoOngoingEffects(this, card, tcard);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected virtual void AddOngoingStatusBonus(Card card, CardStatus status)
        {
            if (status.type == StatusType.AttackBonus)
                card.attack_ongoing += status.value;
            if (status.type == StatusType.HPBonus)
                card.hp_ongoing += status.value;
        }

        //---- Secrets ------------

        public virtual bool TriggerSecrets(AbilityTrigger secret_trigger, Card trigger_card)
        {
            if (trigger_card.HasStatus(StatusType.SpellImmunity))
                return false; //Spell Immunity, triggerer is the one that trigger the trap, target is the one attacked, so usually the player who played the trap, so we dont check the target

            bool success = false;
            for(int p=0; p < game_data.players.Length; p++ )
            {
                if (p != trigger_card.player_id)
                {
                    Player other_player = game_data.players[p];
                    for (int i = other_player.cards_secret.Count - 1; i >= 0; i--)
                    {
                        Card card = other_player.cards_secret[i];
                        CardData icard = card.CardData;
                        if (icard.type == CardType.Secret && !card.exhausted)
                        {
                            if (icard.AreSecretConditionsMet(secret_trigger, game_data, card, trigger_card))
                            {
                                resolve_queue.AddSecret(secret_trigger, card, trigger_card, ResolveSecret);
                                resolve_queue.SetDelay(0.5f);
                                card.exhausted = true;
                                success = true;

                                if (onSecretTrigger != null)
                                    onSecretTrigger.Invoke(card, trigger_card);

                                return success; //Trigger only 1 secret per trigger
                            }
                        }
                    }
                }
            }
            return success;
        }

        protected virtual void ResolveSecret(AbilityTrigger secret_trigger, Card secret_card, Card trigger)
        {
            CardData icard = secret_card.CardData;
            Player player = game_data.GetPlayer(secret_card.player_id);
            if (icard.type == CardType.Secret)
            {
                Player tplayer = game_data.GetPlayer(trigger.player_id);
                tplayer.AddHistory(GameAction.SecretTriggered, secret_card, trigger);

                TriggerCardAbilityType(secret_trigger, secret_card, trigger);
                DiscardCard(secret_card);

                if (onSecretResolve != null)
                    onSecretResolve.Invoke(secret_card, trigger);
            }
        }

        //---- Resolve Selector -----

        public virtual void SelectCard(Card target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || target == null || ability == null)
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(game_data, caster, target))
                    return; //Can't target that target

                Player player = game_data.GetPlayer(caster.player_id);
                player.AddHistory(GameAction.CastAbility, caster, ability, target);
                game_data.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }

            if (game_data.selector == SelectorType.SelectorCard)
            {
                if (!ability.IsCardSelectionValid(game_data, caster, target, card_array))
                    return; //Supports conditions and filters

                game_data.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }
        }

        public virtual void SelectPlayer(Player target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || target == null || ability == null)
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if (!ability.CanTarget(game_data, caster, target))
                    return; //Can't target that target

                Player player = game_data.GetPlayer(caster.player_id);
                player.AddHistory(GameAction.CastAbility, caster, ability, target);
                game_data.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }
        }

        public virtual void SelectSlot(Slot target)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || ability == null || !target.IsValid())
                return;

            if (game_data.selector == SelectorType.SelectTarget)
            {
                if(!ability.CanTarget(game_data, caster, target))
                    return; //Conditions not met

                Player player = game_data.GetPlayer(caster.player_id);
                player.AddHistory(GameAction.CastAbility, caster, ability, target);
                game_data.selector = SelectorType.None;
                ResolveEffectTarget(ability, caster, target);
                AfterAbilityResolved(ability, caster);
                resolve_queue.ResolveAll();
            }
        }

        public virtual void SelectChoice(int choice)
        {
            if (game_data.selector == SelectorType.None)
                return;

            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);

            if (caster == null || ability == null || choice < 0)
                return;

            if (game_data.selector == SelectorType.SelectorChoice && ability.target == AbilityTarget.ChoiceSelector)
            {
                if (choice >= 0 && choice < ability.chain_abilities.Length)
                {
                    AbilityData achoice = ability.chain_abilities[choice];
                    if (achoice != null && achoice.AreTriggerConditionsMet(game_data, caster))
                    {
                        game_data.selector = SelectorType.None;
                        AfterAbilityResolved(ability, caster);
                        ResolveCardAbility(achoice, caster, caster);
                        resolve_queue.ResolveAll();
                    }
                }
            }
        }

        public virtual void CancelSelection()
        {
            if (game_data.selector != SelectorType.None)
            {
                //End selection
                game_data.selector = SelectorType.None;
                onSelectorSelect?.Invoke();
            }
        }

        //-----Trigger Selector-----

        protected virtual void GoToSelectTarget(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectTarget;
            game_data.selector_player = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            onSelectorStart?.Invoke();
        }

        protected virtual void GoToSelectorCard(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectorCard;
            game_data.selector_player = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            onSelectorStart?.Invoke();
        }

        protected virtual void GoToSelectorChoice(AbilityData iability, Card caster)
        {
            game_data.selector = SelectorType.SelectorChoice;
            game_data.selector_player = caster.player_id;
            game_data.selector_ability_id = iability.id;
            game_data.selector_caster_uid = caster.uid;
            onSelectorStart?.Invoke();
        }

        //-------------

        public virtual void ClearResolve()
        {
            resolve_queue.Clear();
        }

        public virtual bool IsResolving()
        {
            return resolve_queue.IsResolving();
        }

        public virtual bool IsGameStarted()
        {
            return game_data.HasStarted();
        }

        public virtual bool IsGameEnded()
        {
            return game_data.HasEnded();
        }

        public virtual Game GetGameData()
        {
            return game_data;
        }

        public System.Random GetRandom()
        {
            return random;
        }

        public Game GameData { get { return game_data; } }
        public ResolveQueue ResolveQueue { get { return resolve_queue; } }
    }
}