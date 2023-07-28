using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using System.Threading.Tasks;

namespace TcgEngine.Client
{
    /// <summary>
    /// 遊戲客戶端的主要腳本，只能在遊戲場景中
    /// 將連接到服務器，然後連接到該服務器上的遊戲（帶有 uid），然後發送遊戲設置
    /// 在遊戲期間，將發送玩家執行的所有操作並接收遊戲刷新
    /// </summary>

    public class GameClient : MonoBehaviour
    {
        //--- 這些設置在菜單場景中設置，遊戲開始時將發送到服務器

        public static GameSettings game_settings = GameSettings.Default;
        public static PlayerSettings player_settings = PlayerSettings.Default;
        public static PlayerSettings ai_settings = PlayerSettings.DefaultAI;
        public static string observe_user = null; //它應該觀察哪個用戶，如果不是 obs，則為 null

        //-----

        public UnityAction onConnectServer;
        public UnityAction onConnectGame;
        public UnityAction<int> onPlayerReady;
        public UnityAction onGameStart;
        public UnityAction<int> onGameEnd;              //勝利者 player_id
        public UnityAction<int> onNewTurn;              //當前玩家 player_id
        public UnityAction<Card, Slot> onCardPlayed;
        public UnityAction<Card, Slot> onCardMoved;
        public UnityAction<Slot> onCardSummoned;
        public UnityAction<Card> onCardTransformed;
        public UnityAction<Card> onCardDiscarded;
        public UnityAction<int> onCardDraw;
        public UnityAction<int> onValueRolled;

        public UnityAction<AbilityData, Card> onAbilityStart;
        public UnityAction<AbilityData, Card, Card> onAbilityTargetCard;      //能力、施法者、目標
        public UnityAction<AbilityData, Card, Player> onAbilityTargetPlayer;
        public UnityAction<AbilityData, Card, Slot> onAbilityTargetSlot;
        public UnityAction<AbilityData, Card> onAbilityEnd;
        public UnityAction<Card, Card> onSecretTrigger;    //秘密、觸發者
        public UnityAction<Card, Card> onSecretResolve;    //秘密、觸發者

        public UnityAction<Card, Card> onAttackStart;   //攻擊者、防守者
        public UnityAction<Card, Card> onAttackEnd;     //攻擊者、防守者
        public UnityAction<Card, Player> onAttackPlayerStart;
        public UnityAction<Card, Player> onAttackPlayerEnd;

        public UnityAction<int, string> onChatMsg;  //玩家 ID、訊息
        public UnityAction< string> onServerMsg;  //訊息
        public UnityAction onRefreshAll;

        private int player_id = 0; //在此設備上玩遊戲的玩家；
        private Game game_data;

        private bool observe_mode = false;
        private int observe_player_id = 0;
        private float timer = 0f;


        private Dictionary<ushort, RefreshEvent> registered_commands = new Dictionary<ushort, RefreshEvent>();

        private static GameClient _instance;

        protected virtual void Awake()
        {
            _instance = this;
            Application.targetFrameRate = 120;
        }

        protected virtual void Start()
        {
            RegisterRefresh(GameAction.Connected, OnConnectedToGame);
            RegisterRefresh(GameAction.PlayerReady, OnPlayerReady);
            RegisterRefresh(GameAction.GameStart, OnGameStart);
            RegisterRefresh(GameAction.GameEnd, OnGameEnd);
            RegisterRefresh(GameAction.NewTurn, OnNewTurn);
            RegisterRefresh(GameAction.CardPlayed, OnCardPlayed);
            RegisterRefresh(GameAction.CardMoved, OnCardMoved);
            RegisterRefresh(GameAction.CardSummoned, OnCardSummoned);
            RegisterRefresh(GameAction.CardTransformed, OnCardTransformed);
            RegisterRefresh(GameAction.CardDiscarded, OnCardDiscarded);
            RegisterRefresh(GameAction.CardDrawn, OnCardDraw);
            RegisterRefresh(GameAction.ValueRolled, OnValueRolled);

            RegisterRefresh(GameAction.AttackStart, OnAttackStart);
            RegisterRefresh(GameAction.AttackEnd, OnAttackEnd);
            RegisterRefresh(GameAction.AttackPlayerStart, OnAttackPlayerStart);
            RegisterRefresh(GameAction.AttackPlayerEnd, OnAttackPlayerEnd);

            RegisterRefresh(GameAction.AbilityTrigger, OnAbilityTrigger);
            RegisterRefresh(GameAction.AbilityTargetCard, OnAbilityTargetCard);
            RegisterRefresh(GameAction.AbilityTargetPlayer, OnAbilityTargetPlayer);
            RegisterRefresh(GameAction.AbilityTargetSlot, OnAbilityTargetSlot);
            RegisterRefresh(GameAction.AbilityEnd, OnAbilityAfter);

            RegisterRefresh(GameAction.SecretTriggered, OnSecretTrigger);
            RegisterRefresh(GameAction.SecretResolved, OnSecretResolve);

            RegisterRefresh(GameAction.ChatMessage, OnChat);
            RegisterRefresh(GameAction.ServerMessage, OnServerMsg);
            RegisterRefresh(GameAction.RefreshAll, OnRefreshAll);

            TcgNetwork.Get().onConnect += OnConnectServer;
            TcgNetwork.Get().Messaging.ListenMsg("refresh", OnReceiveRefresh);

            ConnectToAPI();
            ConnectToServer();
        }

        protected virtual void OnDestroy()
        {
            TcgNetwork.Get().onConnect -= OnConnectServer;
            TcgNetwork.Get().Messaging.UnListenMsg("refresh");
        }

        protected virtual void Update()
        {
            //如果一段時間後無法連接，請退出遊戲場景
            if (game_data == null || game_data.state == GameState.Connecting || game_data.state == GameState.Starting)
            {
                timer += Time.deltaTime;
                if (!game_settings.IsHost() && timer > 10f)
                {
                    SceneNav.GoTo("Menu");
                }
            }
        }

        //--------------------

        public virtual void ConnectToAPI()
        {
            //應該已經從菜單連接到 API
            //如果未連接，則以測試模式啟動（代表遊戲場景是直接從 Unity 啟動的）
            if (!Authenticator.Get().IsSignedIn())
            {
                Authenticator.Get().LoginTest("Player");

                player_settings.deck = new PlayerDeckSettings(GameplayData.Get().test_deck);
                ai_settings.deck = new PlayerDeckSettings(GameplayData.Get().test_deck_ai);
                ai_settings.ai_level = GameplayData.Get().ai_level;
            }

            //根據您的api數據設置頭像、卡背
            UserData udata = Authenticator.Get().UserData;
            if (udata != null)
            {
                player_settings.avatar = udata.GetAvatar();
                player_settings.cardback = udata.GetCardback();
            }
        }

        public virtual async void ConnectToServer()
        {
            await Task.Yield(); //等待初始化完成

            if (TcgNetwork.Get().IsActive())
                return; // 已經連接

            if (game_settings.IsHost())
                TcgNetwork.Get().StartHost(NetworkData.Get().port);
            else
                TcgNetwork.Get().StartClient(game_settings.GetUrl(), NetworkData.Get().port);
        }

        public virtual async void ConnectToGame(string uid)
        {
            await Task.Yield(); //等待初始化完成

            if (!TcgNetwork.Get().IsActive())
                return; //未連接到服務器

            MsgPlayerConnect nplayer = new MsgPlayerConnect();
            nplayer.user_id = Authenticator.Get().UserID;
            nplayer.username = Authenticator.Get().Username;
            nplayer.game_uid = uid;
            nplayer.nb_players = game_settings.nb_players;
            nplayer.observer = game_settings.game_type == GameType.Observer;

            Messaging.SendObject("connect", ServerID, nplayer, NetworkDelivery.Reliable);
        }

        public virtual void SendGameSettings()
        {
            if (game_settings.IsOffline())
            {
                //單人模式，發送您的設置和 AI 設置
                SendGameplaySettings(game_settings);
                SendPlayerSettingsAI(ai_settings);
                SendPlayerSettings(player_settings);
            }
            else
            {
                //在線模式，僅發送您自己的設置
                SendGameplaySettings(game_settings);
                SendPlayerSettings(player_settings);
            }
        }

        public virtual void Disconnect()
        {
            TcgNetwork.Get().Disconnect();
        }

        private void RegisterRefresh(ushort tag, UnityAction<SerializedData> callback)
        {
            RefreshEvent cmdevt = new RefreshEvent();
            cmdevt.tag = tag;
            cmdevt.callback = callback;
            registered_commands.Add(tag, cmdevt);
        }

        public void OnReceiveRefresh(ulong client_id, FastBufferReader reader)
        {
            reader.ReadValueSafe(out ushort type);
            bool found = registered_commands.TryGetValue(type, out RefreshEvent command);
            if (found)
            {
                command.callback.Invoke(new SerializedData(reader));
            }
        }

        //--------------------------

        public void SendPlayerSettings(PlayerSettings psettings)
        {
            SendAction(GameAction.PlayerSettings, psettings);
        }

        public void SendPlayerSettingsAI(PlayerSettings psettings)
        {
            SendAction(GameAction.PlayerSettingsAI, psettings);
        }

        public void SendGameplaySettings(GameSettings settings)
        {
            SendAction(GameAction.GameSettings, settings);
        }

        public void PlayCard(Card card, Slot slot)
        {
            MsgPlayCard mdata = new MsgPlayCard();
            mdata.card_uid = card.uid;
            mdata.slot = slot;
            SendAction(GameAction.PlayCard, mdata);
        }

        public void AttackTarget(Card card, Card target)
        {
            MsgAttack mdata = new MsgAttack();
            mdata.attacker_uid = card.uid;
            mdata.target_uid = target.uid;
            SendAction(GameAction.Attack, mdata);
        }

        public void AttackPlayer(Card card, Player target)
        {
            MsgAttackPlayer mdata = new MsgAttackPlayer();
            mdata.attacker_uid = card.uid;
            mdata.target_id = target.player_id;
            SendAction(GameAction.AttackPlayer, mdata);
        }

        public void Move(Card card, Slot slot)
        {
            MsgPlayCard mdata = new MsgPlayCard();
            mdata.card_uid = card.uid;
            mdata.slot = slot;
            SendAction(GameAction.Move, mdata);
        }

        public void CastAbility(Card card, AbilityData ability)
        {
            MsgCastAbility mdata = new MsgCastAbility();
            mdata.caster_uid = card.uid;
            mdata.ability_id = ability.id;
            mdata.target_uid = "";
            SendAction(GameAction.CastAbility, mdata);
        }

        public void SelectCard(Card card)
        {
            MsgCard mdata = new MsgCard();
            mdata.card_uid = card.uid;
            SendAction(GameAction.SelectCard, mdata);
        }

        public void SelectPlayer(Player player)
        {
            MsgPlayer mdata = new MsgPlayer();
            mdata.player_id = player.player_id;
            SendAction(GameAction.SelectPlayer, mdata);
        }

        public void SelectSlot(Slot slot)
        {
            SendAction(GameAction.SelectSlot, slot);
        }

        public void SelectChoice(int c)
        {
            MsgInt choice = new MsgInt();
            choice.value = c;
            SendAction(GameAction.SelectChoice, choice);
        }

        public void CancelSelection()
        {
            SendAction(GameAction.CancelSelect);
        }

        public void SendChatMsg(string msg)
        {
            MsgChat chat = new MsgChat();
            chat.msg = msg;
            chat.player_id = player_id;
            SendAction(GameAction.ChatMessage, chat);
        }

        public void EndTurn()
        {
            SendAction(GameAction.EndTurn);
        }

        public void Resign()
        {
            SendAction(GameAction.Resign);
        }

        public void SetObserverMode(int player_id)
        {
            observe_mode = true;
            observe_player_id = player_id;
        }

        public void SetObserverMode(string username)
        {
            observe_player_id = 0; //未找到observe_user的默認值

            Game data = GetGameData();
            foreach (Player player in data.players)
            {
                if (player.username == username)
                {
                    observe_player_id = player.player_id;
                }
            }
        }

        public void SendAction<T>(ushort type, T data) where T : INetworkSerializable
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(type);
            writer.WriteNetworkSerializable(data);
            Messaging.Send("action", ServerID, writer, NetworkDelivery.Reliable);
            writer.Dispose();
        }

        public void SendAction(ushort type, int data)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(type);
            writer.WriteValueSafe(data);
            Messaging.Send("action", ServerID, writer, NetworkDelivery.Reliable);
            writer.Dispose();
        }

        public void SendAction(ushort type)
        {
            FastBufferWriter writer = new FastBufferWriter(128, Unity.Collections.Allocator.Temp, TcgNetwork.MsgSizeMax);
            writer.WriteValueSafe(type);
            Messaging.Send("action", ServerID, writer, NetworkDelivery.Reliable);
            writer.Dispose();
        }

        //--- 接收刷新 ----------------------

        protected virtual void OnConnectServer()
        {
            ConnectToGame(game_settings.game_uid);
            onConnectServer?.Invoke();
        }

        protected virtual void OnConnectedToGame(SerializedData sdata)
        {
            MsgAfterConnected msg = sdata.Get<MsgAfterConnected>();
            player_id = msg.player_id;
            game_data = msg.game_data;
            observe_mode = player_id < 0; //如果是觀察者通常會返回-1

            if (observe_mode)
                SetObserverMode(observe_user);

            if (onConnectGame != null)
                onConnectGame.Invoke();

            SendGameSettings();
        }

        protected virtual void OnPlayerReady(SerializedData sdata)
        {
            MsgInt msg = sdata.Get<MsgInt>();
            int pid = msg.value;

            if (onPlayerReady != null)
                onPlayerReady.Invoke(pid);
        }

        private void OnGameStart(SerializedData sdata)
        {
            onGameStart?.Invoke();
        }

        private void OnGameEnd(SerializedData sdata)
        {
            MsgPlayer msg = sdata.Get<MsgPlayer>();
            onGameEnd?.Invoke(msg.player_id);
        }

        private void OnNewTurn(SerializedData sdata)
        {
            MsgPlayer msg = sdata.Get<MsgPlayer>();
            onNewTurn?.Invoke(msg.player_id);
        }

        private void OnCardPlayed(SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            Card card = game_data.GetCard(msg.card_uid);
            onCardPlayed?.Invoke(card, msg.slot);
        }

        private void OnCardSummoned(SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            onCardSummoned?.Invoke(msg.slot);
        }

        private void OnCardMoved(SerializedData sdata)
        {
            MsgPlayCard msg = sdata.Get<MsgPlayCard>();
            Card card = game_data.GetCard(msg.card_uid);
            onCardMoved?.Invoke(card, msg.slot);
        }

        private void OnCardTransformed(SerializedData sdata)
        {
            MsgCard msg = sdata.Get<MsgCard>();
            Card card = game_data.GetCard(msg.card_uid);
            onCardTransformed?.Invoke(card);
        }

        private void OnCardDiscarded(SerializedData sdata)
        {
            MsgCard msg = sdata.Get<MsgCard>();
            Card card = game_data.GetCard(msg.card_uid);
            onCardDiscarded?.Invoke(card);
        }

        private void OnCardDraw(SerializedData sdata)
        {
            MsgInt msg = sdata.Get<MsgInt>();
            onCardDraw?.Invoke(msg.value);
        }

        private void OnValueRolled(SerializedData sdata)
        {
            MsgInt msg = sdata.Get<MsgInt>();
            onValueRolled?.Invoke(msg.value);
        }

        private void OnAttackStart(SerializedData sdata)
        {
            MsgAttack msg = sdata.Get<MsgAttack>();
            Card attacker = game_data.GetCard(msg.attacker_uid);
            Card target = game_data.GetCard(msg.target_uid);
            onAttackStart?.Invoke(attacker, target);
        }

        private void OnAttackEnd(SerializedData sdata)
        {
            MsgAttack msg = sdata.Get<MsgAttack>();
            Card attacker = game_data.GetCard(msg.attacker_uid);
            Card target = game_data.GetCard(msg.target_uid);
            onAttackEnd?.Invoke(attacker, target);
        }

        private void OnAttackPlayerStart(SerializedData sdata)
        {
            MsgAttackPlayer msg = sdata.Get<MsgAttackPlayer>();
            Card attacker = game_data.GetCard(msg.attacker_uid);
            Player target = game_data.GetPlayer(msg.target_id);
            onAttackPlayerStart?.Invoke(attacker, target);
        }

        private void OnAttackPlayerEnd(SerializedData sdata)
        {
            MsgAttackPlayer msg = sdata.Get<MsgAttackPlayer>();
            Card attacker = game_data.GetCard(msg.attacker_uid);
            Player target = game_data.GetPlayer(msg.target_id);
            onAttackPlayerEnd?.Invoke(attacker, target);
        }

        private void OnAbilityTrigger(SerializedData sdata)
        {
            MsgCastAbility msg = sdata.Get<MsgCastAbility>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            onAbilityStart?.Invoke(ability, caster);
        }

        private void OnAbilityTargetCard(SerializedData sdata)
        {
            MsgCastAbility msg = sdata.Get<MsgCastAbility>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            Card target = game_data.GetCard(msg.target_uid);
            onAbilityTargetCard?.Invoke(ability, caster, target);
        }

        private void OnAbilityTargetPlayer(SerializedData sdata)
        {
            MsgCastAbilityPlayer msg = sdata.Get<MsgCastAbilityPlayer>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            Player target = game_data.GetPlayer(msg.target_id);
            onAbilityTargetPlayer?.Invoke(ability, caster, target);
        }

        private void OnAbilityTargetSlot(SerializedData sdata)
        {
            MsgCastAbilitySlot msg = sdata.Get<MsgCastAbilitySlot>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            onAbilityTargetSlot?.Invoke(ability, caster, msg.slot);
        }

        private void OnAbilityAfter(SerializedData sdata)
        {
            MsgCastAbility msg = sdata.Get<MsgCastAbility>();
            AbilityData ability = AbilityData.Get(msg.ability_id);
            Card caster = game_data.GetCard(msg.caster_uid);
            onAbilityEnd?.Invoke(ability, caster);
        }

        private void OnSecretTrigger(SerializedData sdata)
        {
            MsgSecret msg = sdata.Get<MsgSecret>();
            Card secret = game_data.GetCard(msg.secret_uid);
            Card triggerer = game_data.GetCard(msg.triggerer_uid);
            onSecretTrigger?.Invoke(secret, triggerer);
        }

        private void OnSecretResolve(SerializedData sdata)
        {
            MsgSecret msg = sdata.Get<MsgSecret>();
            Card secret = game_data.GetCard(msg.secret_uid);
            Card triggerer = game_data.GetCard(msg.triggerer_uid);
            onSecretResolve?.Invoke(secret, triggerer);
        }

        private void OnChat(SerializedData sdata)
        {
            MsgChat msg = sdata.Get<MsgChat>();
            onChatMsg?.Invoke(msg.player_id, msg.msg);
        }

        private void OnServerMsg(SerializedData sdata)
        {
            string msg = sdata.GetString();
            onServerMsg?.Invoke(msg);
        }

        private void OnRefreshAll(SerializedData sdata)
        {
            MsgRefreshAll msg = sdata.Get<MsgRefreshAll>();
            game_data = msg.game_data;
            onRefreshAll?.Invoke();
        }

        //--------------------------

        public virtual bool IsReady()
        {
            return game_data != null && TcgNetwork.Get().IsConnected();
        }

        public Player GetPlayer()
        {
            Game gdata = GetGameData();
            return gdata.GetPlayer(GetPlayerID());
        }

        public Player GetOpponentPlayer()
        {
            Game gdata = GetGameData();
            return gdata.GetPlayer(GetOpponentPlayerID());
        }

        public int GetPlayerID()
        {
            if (observe_mode)
                return observe_player_id;
            return player_id;
        }

        public int GetOpponentPlayerID()
        {
            return GetPlayerID() == 0 ? 1 : 0;
        }

        public virtual bool IsYourTurn()
        {
            int player_id = GetPlayerID();
            Game game_data = GetGameData();

            if (!IsReady())
                return false;
            return player_id == game_data.current_player;
        }

        public bool IsObserveMode()
        {
            return observe_mode;
        }

        public Game GetGameData()
        {
            return game_data;
        }

        public bool HasEnded()
        {
            return game_data.HasEnded();
        }

        private void OnApplicationQuit()
        {
            Resign(); //關閉應用程序之前自動退出。但似乎不起作用，可能因為消息在關閉之前沒有時間發送
        }

        public bool IsHost { get { return TcgNetwork.Get().IsHost; } }
        public ulong ServerID { get { return TcgNetwork.Get().ServerID; } }
        public NetworkMessaging Messaging { get { return TcgNetwork.Get().Messaging; } }

        public static GameClient Get()
        {
            return _instance;
        }

    }

    public class RefreshEvent
    {
        public ushort tag;
        public UnityAction<SerializedData> callback;
    }
}