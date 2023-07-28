using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TcgEngine.Client;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    /// <summary>
    /// AI 玩家做出完全隨機的決定，最爛AI，for測試使用
    /// </summary>

    public class AIPlayerRandom : AIPlayer
    {
        private bool is_playing = false;
        private bool is_selecting = false;

        private System.Random rand = new System.Random();

        public AIPlayerRandom(GameLogic gameplay, int id, int level)
        {
            this.gameplay = gameplay;
            player_id = id;
        }

        public override void Update()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            if (game_data.state == GameState.Play)
            {
                if(!is_playing && game_data.selector == SelectorType.None && game_data.current_player == player_id)
                {
                    is_playing = true;
                    TimeTool.StartCoroutine(AiTurn());
                }

                if (!is_selecting && game_data.selector != SelectorType.None && game_data.selector_player == player_id)
                {
                    if (game_data.selector == SelectorType.SelectTarget)
                    {
                        //AI 選擇目標
                        is_selecting = true;
                        TimeTool.StartCoroutine(AiSelectTarget());
                    }

                    if (game_data.selector == SelectorType.SelectorCard)
                    {
                        //AI 選擇目標
                        is_selecting = true;
                        TimeTool.StartCoroutine(AiSelectCard());
                    }

                    if (game_data.selector == SelectorType.SelectorChoice)
                    {
                        //AI 選擇目標
                        is_selecting = true;
                        TimeTool.StartCoroutine(AiSelectChoice());
                    }
                }
            }
        }

        private IEnumerator AiTurn()
        {
            yield return new WaitForSeconds(1f);

            PlayCard();

            yield return new WaitForSeconds(0.5f);

            PlayCard();

            yield return new WaitForSeconds(0.5f);

            PlayCard();

            yield return new WaitForSeconds(0.5f);

            Attack();

            yield return new WaitForSeconds(0.5f);

            Attack();

            yield return new WaitForSeconds(0.5f);

            AttackPlayer();

            yield return new WaitForSeconds(0.5f);

            EndTurn();

            is_playing = false;
        }

        private IEnumerator AiSelectCard()
        {
            yield return new WaitForSeconds(0.5f);

            SelectCard();

            yield return new WaitForSeconds(0.5f);

            CancelSelect();
            is_selecting = false;
        }

        private IEnumerator AiSelectTarget()
        {
            yield return new WaitForSeconds(0.5f);

            SelectTarget();

            yield return new WaitForSeconds(0.5f);

            CancelSelect();
            is_selecting = false;
        }

        private IEnumerator AiSelectChoice()
        {
            yield return new WaitForSeconds(0.5f);

            SelectChoice();

            yield return new WaitForSeconds(0.5f);

            CancelSelect();
            is_selecting = false;
        }

        //----------

        public void PlayCard()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);
            if (player.cards_hand.Count > 0 && game_data.IsPlayerActionTurn(player))
            {
                Card random = player.GetRandomCard(player.cards_hand, rand);
                Slot slot = player.GetRandomSlot(rand);

                if (random != null && random.CardData.IsRequireTarget())
                    slot = game_data.GetRandomSlot(rand); //法術可以針對任何位置，而不僅僅是玩家這邊

                if (random != null)
                    gameplay.PlayCard(random, slot);
            }
        }

        public void Attack()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);
            if (player.cards_board.Count > 0 && game_data.IsPlayerActionTurn(player))
            {
                Card random = player.GetRandomCard(player.cards_board, rand);
                Card rtarget = game_data.GetRandomBoardCard(rand);
                if (random != null && rtarget != null)
                    gameplay.AttackTarget(random, rtarget);
            }
        }

        public void AttackPlayer()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);
            Player oplayer = game_data.GetRandomPlayer(rand);
            if (player.cards_board.Count > 0 && game_data.IsPlayerActionTurn(player))
            {
                Card random = player.GetRandomCard(player.cards_board, rand);
                if (random != null && oplayer != null && oplayer != player)
                    gameplay.AttackPlayer(random, oplayer);
            }
        }

        public void SelectCard()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            Player player = game_data.GetPlayer(player_id);
            AbilityData ability = AbilityData.Get(game_data.selector_ability_id);
            Card caster = game_data.GetCard(game_data.selector_caster_uid);
            if (player != null && ability != null && caster != null)
            {
                List<Card> card_list = ability.GetCardTargets(game_data, caster);
                if (card_list.Count > 0)
                {
                    Card card = card_list[rand.Next(0, card_list.Count)];
                    gameplay.SelectCard(card);
                }
            }
        }

        public void SelectTarget()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            if (game_data.selector != SelectorType.None)
            {
                int target_player = player_id;
                AbilityData ability = AbilityData.Get(game_data.selector_ability_id);
                if (ability != null && ability.target == AbilityTarget.SelectTarget)
                    target_player = (player_id == 0 ? 1 : 0);

                Player tplayer = game_data.GetPlayer(target_player);
                if (tplayer.cards_board.Count > 0)
                {
                    Card random = tplayer.GetRandomCard(tplayer.cards_board, rand);
                    if (random != null)
                        gameplay.SelectCard(random);
                }
            }
        }

        public void SelectChoice()
        {
            if (!CanPlay())
                return;

            Game game_data = gameplay.GetGameData();
            if (game_data.selector != SelectorType.None)
            {
                AbilityData ability = AbilityData.Get(game_data.selector_ability_id);
                if (ability != null && ability.chain_abilities.Length > 0)
                {
                    int choice = rand.Next(0, ability.chain_abilities.Length);
                    gameplay.SelectChoice(choice);
                }
            }
        }

        public void CancelSelect()
        {
            if (CanPlay())
            {
                gameplay.CancelSelection();
            }
        }

        public void EndTurn()
        {
            if (CanPlay())
            {
                gameplay.EndTurn();
            }
        }
    }

}