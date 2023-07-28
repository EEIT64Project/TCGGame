using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;
using TcgEngine.Gameplay;

namespace TcgEngine.AI
{
    /// <summary>
    /// AI 的極小極大算法
    /// </summary>

    public class AILogic
    {
        //-------- AI Logic 參數 ------------------

        public int ai_depth = 3;                //提前檢查多少圈，次數越多，花費的時間就越長
        public int ai_depth_wide = 1;           //對於最初的幾個回合，會考慮更多的選擇
        public int actions_per_turn = 2;        //AI 每回合不會執行超過此數量的命令
        public int actions_per_turn_wide = 3;   //如上但深度較廣
        public int actions_per_node = 4;         //在單個節點中，無法評估超過此數量的AIActions，如果超過，僅使用得分最高的AIActions
        public int actions_per_node_wide = 7;    //如上但深度較廣

        //-----

        public int ai_player_id;                    //AIplayer_id（設定是1）
        public int ai_level = 10;                   //AI level

        private GameLogic game_logic;
        private Game game_data;
        private AIHeuristic heuristic;
        private Thread ai_thread;

        private NodeState first_node = null;
        private NodeState best_move = null;

        private bool running = false;
        private int nb_calculated = 0;
        private int reached_depth = 0;

        private System.Random random_gen;

        private Pool<NodeState> node_pool = new Pool<NodeState>();
        private Pool<Game> data_pool = new Pool<Game>();
        private Pool<AIAction> action_pool = new Pool<AIAction>();
        private Pool<List<AIAction>> list_pool = new Pool<List<AIAction>>();
        private ListSwap<Card> card_array = new ListSwap<Card>();

        public static AILogic Create(int player_id, int level)
        {
            AILogic job = new AILogic();
            job.ai_player_id = player_id;
            job.ai_level = level;

            job.heuristic = new AIHeuristic(player_id, level);
            job.game_logic = new GameLogic(true); //跳過 AI 計算的所有延遲

            return job;
        }

        public void RunAI(Game data)
        {
            if (running)
                return;

            game_data = Game.CloneNew(data);        //複製遊戲數據，保持原始數據不受影響
            game_logic.ClearResolve();                 //清除臨時內存
            game_logic.SetData(game_data);          //將數據分配給遊戲邏輯
            random_gen = new System.Random();       //重置隨機種子

            first_node = null;
            reached_depth = 0;
            nb_calculated = 0;

            Start();
        }

        public void Start()
        {
            running = true;

            //取消註釋這些行以在單獨的線程上運行（並註釋 Execute()），這樣更適合生產，因此在計算 AI 時不會凍結 UI
            ai_thread = new Thread(Execute);
            ai_thread.Start();

            //取消註釋此行以在主線程上運行（並註釋線程），這樣可以更好地進行調試，將能夠使用斷點、分析器和 Debug.Log
            //Execute();
        }

        public void Stop()
        {
            running = false;
            if (ai_thread != null && ai_thread.IsAlive)
                ai_thread.Abort();
        }

        public void Execute()
        {
            //創建第一個節點
            first_node = CreateNode(null, null, ai_player_id, 0, 0);
            first_node.hvalue = heuristic.CalculateHeuristic(game_data, first_node);
            first_node.alpha = int.MinValue;
            first_node.beta = int.MaxValue;

            Profiler.BeginSample("AI");
            System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();

            //計算第一個節點
            CalculateNode(game_data, first_node);

            Debug.Log("AI: Time " + watch.ElapsedMilliseconds + "ms Depth " + reached_depth + " Nodes " + nb_calculated);
            Profiler.EndSample();

            //保存最佳動作
            best_move = first_node.best_child;
            running = false;
        }

        //添加所有可能order的列表並在所有orders中進行搜索
        private void CalculateNode(Game data, NodeState node)
        {
            Profiler.BeginSample("Add Orders");
            Player player = data.GetPlayer(data.current_player);
            List<AIAction> action_list = list_pool.Create();

            int max_actions = node.tdepth < ai_depth_wide ? actions_per_turn_wide : actions_per_turn;
            if (node.taction < max_actions)
            {
                if (data.selector == SelectorType.None)
                {
                    //打出卡牌
                    for (int c = 0; c < player.cards_hand.Count; c++)
                    {
                        Card card = player.cards_hand[c];
                        AddActions(action_list, data, node, GameAction.PlayCard, card);
                    }

                    //面板上的動作
                    for (int c = 0; c < player.cards_board.Count; c++)
                    {
                        Card card = player.cards_board[c];
                        AddActions(action_list, data, node, GameAction.Attack, card);
                        AddActions(action_list, data, node, GameAction.AttackPlayer, card);
                        AddActions(action_list, data, node, GameAction.CastAbility, card);
                        //AddActions(action_list, data, node, GameAction.Move, card);        //取消註釋以考慮移動操作
                    }

                    if (player.hero != null)
                        AddActions(action_list, data, node, GameAction.CastAbility, player.hero);
                }
                else
                {
                    AddSelectActions(action_list, data, node);
                }
            }

            //結束回合 (如果人工智能仍然可以攻擊玩家，或者人工智能沒有消耗任何法力，則不要添加動作)
            bool is_full_mana = HasAction(action_list, GameAction.PlayCard) && player.mana >= player.mana_max;
            bool can_attack_player = HasAction(action_list, GameAction.AttackPlayer);
            bool can_end = !can_attack_player && !is_full_mana && data.selector == SelectorType.None;
            if (action_list.Count == 0 || can_end)
            {
                AIAction actiont = CreateAction(GameAction.EndTurn);
                action_list.Add(actiont);
            }

            //刪除低分動作
            FilterActions(data, node, action_list);
            Profiler.EndSample();

            //執行有效動作並蒐索子節點
            for (int o = 0; o < action_list.Count; o++)
            {
                AIAction action = action_list[o];
                if (action.valid && node.alpha < node.beta)
                {
                    CalculateChildNode(data, node, action);
                }
            }

            action_list.Clear();
            list_pool.Dispose(action_list);
        }

        //對每個動作標記有效/無效，如果動作太多，將只保留得分最高的動作
        private void FilterActions(Game data, NodeState node, List<AIAction> action_list)
        {
            int count_valid = 0;
            for (int o = 0; o < action_list.Count; o++)
            {
                AIAction action = action_list[o];
                action.sort = heuristic.CalculateActionSort(data, action);
                action.valid = action.sort <= 0 || action.sort >= node.sort_min;
                if (action.valid)
                    count_valid++;
            }

            int max_actions = node.tdepth < ai_depth_wide ? actions_per_node_wide : actions_per_node;
            int max_actions_skip = max_actions + 2; //如果只是刪除1-2個動作，則無需計算所有分數
            if (count_valid <= max_actions_skip)
                return; //無需過濾

            //計算分數
            for (int o = 0; o < action_list.Count; o++)
            {
                AIAction action = action_list[o];
                if (action.valid)
                {
                    action.score = heuristic.CalculateActionScore(data, action);
                }
            }

            //排序並使低分動作無效
            action_list.Sort((AIAction a, AIAction b) => { return b.score.CompareTo(a.score); });
            for (int o = 0; o < action_list.Count; o++)
            {
                AIAction action = action_list[o];
                action.valid = action.valid && o < max_actions;
            }
        }

        //為parent創建子節點，並計算
        private void CalculateChildNode(Game data, NodeState parent, AIAction action)
        {
            if (action.type == GameAction.None)
                return;

            int player_id = data.current_player;

            //複製數據，以便可以在新節點中更新它
            Profiler.BeginSample("Clone Data");
            Game ndata = data_pool.Create();
            Game.Clone(data, ndata); //Clone
            game_logic.ClearResolve();
            game_logic.SetData(ndata);
            Profiler.EndSample();

            //執行移動並更新數據
            Profiler.BeginSample("Execute AIAction");
            DoAIAction(ndata, action, player_id);
            Profiler.EndSample();

            //更新深度
            bool new_turn = action.type == GameAction.EndTurn;
            int next_tdepth = parent.tdepth;
            int next_taction = parent.taction + 1;

            if (new_turn)
            {
                next_tdepth = parent.tdepth + 1;
                next_taction = 0;
            }

            //創建節點
            Profiler.BeginSample("Create Node");
            NodeState child_node = CreateNode(parent, action, player_id, next_tdepth, next_taction);
            parent.childs.Add(child_node);
            Profiler.EndSample();

            //計算獲勝條件的快速函式
            child_node.hvalue = heuristic.CalculateWinHeuristic(ndata, child_node);

            //設置下一個AIActions的最小排序，如果是新回合，則重置為0
            child_node.sort_min = new_turn ? 0 : Mathf.Max(action.sort, child_node.sort_min);

            //如果獲勝或達到最大深度，則停止更深的搜索
            if (!heuristic.IsWin(child_node) && child_node.tdepth < ai_depth)
            {
                //計算子節點
                CalculateNode(ndata, child_node);
            }
            else
            {
                //計算完整的函式
                child_node.hvalue = heuristic.CalculateHeuristic(ndata, child_node, child_node.hvalue);
            }

            //更新parents hvalue、alpha、beta 和最佳子節點
            if (player_id == ai_player_id)
            {
                //AI player
                if (parent.best_child == null || child_node.hvalue > parent.hvalue)
                {
                    parent.best_child = child_node;
                    parent.hvalue = child_node.hvalue;
                    parent.alpha = Mathf.Max(parent.alpha, parent.hvalue);
                }
            }
            else
            {
                //對手
                if (parent.best_child == null || child_node.hvalue < parent.hvalue)
                {
                    parent.best_child = child_node;
                    parent.hvalue = child_node.hvalue;
                    parent.beta = Mathf.Min(parent.beta, parent.hvalue);
                }
            }

            //用於調試，跟踪節點/深度計數
            nb_calculated++;
            if (child_node.tdepth > reached_depth)
                reached_depth = child_node.tdepth;

            //處理完這個遊戲數據，將其處理掉。
            //不處理 NodeState (node_pool)，因為想稍後檢索完整的路徑
            data_pool.Dispose(ndata);
        }

        private NodeState CreateNode(NodeState parent, AIAction action, int player_id, int turn_depth, int turn_action)
        {
            NodeState nnode = node_pool.Create();
            nnode.current_player = player_id;
            nnode.tdepth = turn_depth;
            nnode.taction = turn_action;
            nnode.parent = parent;
            nnode.last_action = action;
            nnode.alpha = parent != null ? parent.alpha : int.MinValue;
            nnode.beta = parent != null ? parent.beta : int.MaxValue;
            nnode.hvalue = 0;
            nnode.sort_min = 0;
            return nnode;
        }

        //將卡牌所有可能的動作添加到動作列表中
        private void AddActions(List<AIAction> actions, Game data, NodeState node, ushort type, Card card)
        {
            Player player = data.GetPlayer(data.current_player);

            if (data.selector != SelectorType.None)
                return;

            if (card.HasStatus(StatusType.Paralysed))
                return;

            if (type == GameAction.PlayCard)
            {
                if (card.CardData.IsBoardCard())
                {
                    //這張牌打在哪裡並不重要
                    Slot slot = Slot.None;
                    List<Slot> slots = player.GetEmptySlots();
                    if (slots.Count > 0)
                        slot = slots[random_gen.Next(slots.Count)];

                    if (data.CanPlayCard(card, slot))
                    {
                        AIAction action = CreateAction(type, card);
                        action.slot = slot;
                        actions.Add(action);
                    }
                }
                else if (card.CardData.IsRequireTarget())
                {
                    for (int p = 0; p < data.players.Length; p++)
                    {
                        Player tplayer = data.players[p];
                        Slot tslot = new Slot(tplayer.player_id);
                        if (data.CanPlayCard(card, tslot) && data.IsPlayTargetValid(card, tplayer, true))
                        {
                            AIAction action = CreateAction(type, card);
                            action.slot = tslot;
                            action.target_player_id = tplayer.player_id;
                            actions.Add(action);
                        }
                    }
                    foreach (Slot slot in Slot.GetAll())
                    {
                        if (data.CanPlayCard(card, slot) && data.IsPlayTargetValid(card, slot, true))
                        {
                            Card slot_card = data.GetSlotCard(slot);
                            AIAction action = CreateAction(type, card);
                            action.slot = slot;
                            action.target_uid = slot_card != null ? slot_card.uid : null;
                            actions.Add(action);
                        }
                    }
                }
                else if (data.CanPlayCard(card, Slot.None))
                {
                    AIAction action = CreateAction(type, card);
                    actions.Add(action);
                }
            }

            if (type == GameAction.Attack)
            {
                if (card.CanAttack())
                {
                    for (int p = 0; p < data.players.Length; p++)
                    {
                        if (p != player.player_id)
                        {
                            Player oplayer = data.players[p];
                            for (int tc = 0; tc < oplayer.cards_board.Count; tc++)
                            {
                                Card target = oplayer.cards_board[tc];
                                if (data.CanAttackTarget(card, target))
                                {
                                    AIAction action = CreateAction(type, card);
                                    action.target_uid = target.uid;
                                    actions.Add(action);
                                }
                            }
                        }
                    }
                }
            }

            if (type == GameAction.AttackPlayer)
            {
                if (card.CanAttack())
                {
                    for (int p = 0; p < data.players.Length; p++)
                    {
                        if (p != player.player_id)
                        {
                            Player oplayer = data.players[p];
                            if (data.CanAttackTarget(card, oplayer))
                            {
                                AIAction action = CreateAction(type, card);
                                action.target_player_id = oplayer.player_id;
                                actions.Add(action);
                            }
                        }
                    }
                }
            }

            if (type == GameAction.CastAbility)
            {
                for (int a = 0; a < card.CardData.abilities.Length; a++)
                {
                    AbilityData ability = card.CardData.abilities[a];
                    if (ability.trigger == AbilityTrigger.Activate && data.CanCastAbility(card, ability))
                    {
                        AIAction action = CreateAction(type, card);
                        action.ability_id = ability.id;
                        actions.Add(action);
                    }
                }
            }

            if (type == GameAction.Move)
            {
                foreach (Slot slot in Slot.GetAll(player.player_id))
                {
                    if (data.CanMoveCard(card, slot))
                    {
                        AIAction action = CreateAction(type, card);
                        action.slot = slot;
                        actions.Add(action);
                    }
                }
            }
        }

        //添加所有可能的動作以進行選擇
        private void AddSelectActions(List<AIAction> actions, Game data, NodeState node)
        {
            if (data.selector == SelectorType.None)
                return;

            Player player = data.GetPlayer(data.selector_player);
            Card caster = data.GetCard(data.selector_caster_uid);
            AbilityData ability = AbilityData.Get(data.selector_ability_id);
            if (player == null || caster == null || ability == null)
                return;

            if (ability.target == AbilityTarget.SelectTarget)
            {
                for (int p = 0; p < data.players.Length; p++)
                {
                    Player tplayer = data.players[p];
                    if (ability.CanAiTarget(data, caster, tplayer))
                    {
                        AIAction action = CreateAction(GameAction.SelectPlayer, caster);
                        action.target_player_id = tplayer.player_id;
                        actions.Add(action);
                    }

                    foreach (Slot slot in Slot.GetAll())
                    {
                        Card tcard = data.GetSlotCard(slot);
                        if (tcard != null && ability.CanAiTarget(data, caster, tcard))
                        {
                            AIAction action = CreateAction(GameAction.SelectCard, caster);
                            action.target_uid = tcard.uid;
                            actions.Add(action);
                        }
                        else if (tcard == null && ability.CanAiTarget(data, caster, slot))
                        {
                            AIAction action = CreateAction(GameAction.SelectSlot, caster);
                            action.slot = slot;
                            actions.Add(action);
                        }
                    }
                }
            }

            if (ability.target == AbilityTarget.CardSelector)
            {
                for (int p = 0; p < data.players.Length; p++)
                {
                    List<Card> cards = ability.GetCardTargets(data, caster, card_array);
                    foreach (Card tcard in cards)
                    {
                        AIAction action = CreateAction(GameAction.SelectCard, caster);
                        action.target_uid = tcard.uid;
                        actions.Add(action);
                    }
                }
            }

            if (ability.target == AbilityTarget.ChoiceSelector)
            {
                for(int i=0; i<ability.chain_abilities.Length; i++)
                {
                    AbilityData choice = ability.chain_abilities[i];
                    if (choice != null && choice.AreTriggerConditionsMet(data, caster))
                    {
                        AIAction action = CreateAction(GameAction.SelectChoice, caster);
                        action.value = i;
                        actions.Add(action);
                    }
                }
            }

            //添加取消選項（如果沒有有效選項）
            if (actions.Count == 0)
            {
                AIAction caction = CreateAction(GameAction.CancelSelect, caster);
                actions.Add(caction);
            }
        }

        private AIAction CreateAction(ushort type)
        {
            AIAction action = action_pool.Create();
            action.Clear();
            action.type = type;
            action.valid = true;
            return action;
        }

        private AIAction CreateAction(ushort type, Card card)
        {
            AIAction action = action_pool.Create();
            action.Clear();
            action.type = type;
            action.card_uid = card.uid;
            action.valid = true;
            return action;
        }

        //模擬AI動作
        private void DoAIAction(Game data, AIAction action, int player_id)
        {
            Player player = data.GetPlayer(player_id);

            if (action.type == GameAction.PlayCard)
            {
                Card card = player.GetHandCard(action.card_uid);
                game_logic.PlayCard(card, action.slot);
            }

            if (action.type == GameAction.Move)
            {
                Card card = player.GetBoardCard(action.card_uid);
                game_logic.MoveCard(card, action.slot);
            }

            if (action.type == GameAction.Attack)
            {
                Card card = player.GetBoardCard(action.card_uid);
                Card target = data.GetBoardCard(action.target_uid);
                game_logic.AttackTarget(card, target);
            }

            if (action.type == GameAction.AttackPlayer)
            {
                Card card = player.GetBoardCard(action.card_uid);
                Player tplayer = data.GetPlayer(action.target_player_id);
                game_logic.AttackPlayer(card, tplayer);
            }

            if (action.type == GameAction.CastAbility)
            {
                Card card = player.GetCard(action.card_uid);
                AbilityData ability = AbilityData.Get(action.ability_id);
                game_logic.CastAbility(card, ability);
            }

            if (action.type == GameAction.SelectCard)
            {
                Card target = data.GetCard(action.target_uid);
                game_logic.SelectCard(target);
            }

            if (action.type == GameAction.SelectPlayer)
            {
                Player target = data.GetPlayer(action.target_player_id);
                game_logic.SelectPlayer(target);
            }

            if (action.type == GameAction.SelectSlot)
            {
                game_logic.SelectSlot(action.slot);
            }

            if (action.type == GameAction.SelectChoice)
            {
                game_logic.SelectChoice(action.value);
            }

            if (action.type == GameAction.CancelSelect)
            {
                game_logic.CancelSelection();
            }

            if (action.type == GameAction.EndTurn)
            {
                game_logic.EndTurn();
            }
        }

        private bool HasAction(List<AIAction> list, ushort type)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].type == type)
                    return true;
            }
            return false;
        }

        //----Return values----

        public bool IsRunning()
        {
            return running;
        }

        public string GetNodePath()
        {
            return GetNodePath(first_node);
        }

        public string GetNodePath(NodeState node)
        {
            string path = "Prediction: HValue: " + node.hvalue + "\n";
            NodeState current = node;
            AIAction move;

            while (current != null)
            {
                move = current.last_action;
                if (move != null)
                    path += "Player " + current.current_player + ": " + move.GetText(game_data) + "\n";
                current = current.best_child;
            }
            return path;
        }

        public void ClearMemory()
        {
            game_data = null;
            first_node = null;
            best_move = null;

            foreach (NodeState node in node_pool.GetAllActive())
                node.Clear();
            foreach (AIAction order in action_pool.GetAllActive())
                order.Clear();

            data_pool.DisposeAll();
            node_pool.DisposeAll();
            action_pool.DisposeAll();
            list_pool.DisposeAll();

            System.GC.Collect(); //從 AI 中釋放內存
        }

        public int GetNbNodesCalculated()
        {
            return nb_calculated;
        }

        public int GetDepthReached()
        {
            return reached_depth;
        }

        public NodeState GetBest()
        {
            return best_move;
        }

        public NodeState GetFirst()
        {
            return first_node;
        }

        public AIAction GetBestAction()
        {
            return best_move != null ? best_move.last_action : null;
        }

        public bool IsBestFound()
        {
            return best_move != null;
        }
    }

    public class NodeState
    {
        public int tdepth;      //深度（圈數）
        public int taction;     //當前回合有多少orders
        public int sort_min;    //排序最小值，低於該值的順序將被忽略，以避免同時計算路徑 A -> B 和路徑 B -> A
        public int hvalue;      //函式價值，人工智能試圖最大化它，對手試圖最小化它
        public int alpha;       //AI玩家達到的最高函式，用於優化並忽略一些分支
        public int beta;        //對手玩家達到的最低函式，用於優化並忽略某些分支

        public AIAction last_action = null;
        public int current_player;

        public NodeState parent;
        public NodeState best_child = null;
        public List<NodeState> childs = new List<NodeState>();

        public NodeState() { }

        public NodeState(NodeState parent, int player_id, int turn_depth, int turn_action, int turn_sort)
        {
            this.parent = parent;
            this.current_player = player_id;
            this.tdepth = turn_depth;
            this.taction = turn_action;
            this.sort_min = turn_sort;
        }

        public void Clear()
        {
            last_action = null;
            best_child = null;
            parent = null;
            childs.Clear();
        }
    }

    public class AIAction
    {
        public ushort type;

        public string card_uid;
        public string target_uid;
        public int target_player_id;
        public string ability_id;
        public Slot slot;
        public int value;

        public int score;           //評分以確定哪些orders被削減和忽略
        public int sort;            //Orders 必須按排序順序執行
        public bool valid;          //如果為 false，則該order將被忽略

        public AIAction() { }
        public AIAction(ushort t) { type = t; }

        public string GetText(Game data)
        {
            string txt = GameAction.GetString(type);
            Card card = data.GetCard(card_uid);
            Card target = data.GetCard(target_uid);
            if (card != null)
                txt += " card " + card.card_id;
            if (target != null)
                txt += " target " + target.card_id;
            if (slot != Slot.None)
                txt += " slot " + slot.x + "-" + slot.p;
            if (ability_id != null)
                txt += " ability " + ability_id;
            if (value > 0)
                txt += " value " + value;
            return txt;
        }

        public void Clear()
        {
            type = 0;
            valid = false;
            card_uid = null;
            target_uid = null;
            ability_id = null;
            target_player_id = -1;
            slot = Slot.None;
            value = -1;
            score = 0;
            sort = 0;
        }

        public static AIAction None { get { AIAction a = new AIAction(); a.type = 0; return a; } }
    }
}
