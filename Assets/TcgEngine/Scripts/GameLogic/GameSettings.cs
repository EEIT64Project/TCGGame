using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace TcgEngine
{

    [System.Serializable]
    public enum GameType
    {
        Solo = 0,
        Adventure = 10,
        Multiplayer = 20,
        HostP2P = 30,
        Observer = 40,
    }

    [System.Serializable]
    public enum GameMode
    {
        Casual = 0,
        Ranked = 10,
    }

    /// <summary>
    /// 保存所有客戶端的遊戲設置，如游戲模式、遊戲 uid 和要加載的場景
    /// 比賽開始時將發送到服務器
    /// </summary>

    [System.Serializable]
    public class GameSettings : INetworkSerializable
    {
        public string server_url;   //要連接的服務器
        public string game_uid;     //該服務器上的遊戲 uid
        public string scene;        //要加載哪個場景
        public int nb_players;      //多少玩家，包括AI（UI僅支持2）

        public GameType game_type = GameType.Solo;      //多人遊戲？獨玩？觀察員？
        public GameMode game_mode = GameMode.Casual;    //有排名還是沒排名？還有其他特殊的遊戲模式嗎？
        public string level;                            //冒險關卡ID

        public virtual bool IsHost()
        {
            return game_type == GameType.Solo || game_type == GameType.Adventure || game_type == GameType.HostP2P;
        }

        public virtual bool IsOffline()
        {
            return game_type == GameType.Solo || game_type == GameType.Adventure;
        }

        public virtual bool IsOnline()
        {
            return game_type == GameType.HostP2P || game_type == GameType.Multiplayer || game_type == GameType.Observer;
        }

        public virtual bool IsOnlinePlayer()
        {
            return game_type == GameType.HostP2P || game_type == GameType.Multiplayer;
        }

        public virtual bool IsRanked()
        {
            return game_mode == GameMode.Ranked;
        }

        public virtual string GetUrl()
        {
            if (!string.IsNullOrEmpty(server_url))
                return server_url;
            return NetworkData.Get().url;
        }

        public virtual string GetScene()
        {
            if (!string.IsNullOrEmpty(scene))
                return scene;
            return GameplayData.Get().GetRandomArena();
        }

        public virtual string GetGameModeId()
        {
            if (game_mode == GameMode.Ranked)
                return "ranked";
            if (game_mode == GameMode.Casual)
                return "casual";
            return "";
        }

        public virtual void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref server_url);
            serializer.SerializeValue(ref game_uid);
            serializer.SerializeValue(ref scene);
            serializer.SerializeValue(ref game_type);
            serializer.SerializeValue(ref game_mode);
            serializer.SerializeValue(ref nb_players);
            serializer.SerializeValue(ref level);
        }

        public static string GetRankModeString(GameMode rank_mode)
        {
            if (rank_mode == GameMode.Ranked)
                return "ranked";
            if (rank_mode == GameMode.Casual)
                return "casual";
            return "";
        }

        public static GameMode GetRankMode(string rank_id)
        {
            if (rank_id == "ranked")
                return GameMode.Ranked;
            if (rank_id == "casual")
                return GameMode.Casual;
            return GameMode.Casual;
        }

        public static GameSettings Default
        {
            get
            {
                GameSettings settings = new GameSettings();
                settings.server_url = "";
                settings.game_uid = "test";
                settings.game_type = GameType.Solo;
                settings.game_mode = GameMode.Casual;
                settings.nb_players = 2;
                settings.scene = "Game";
                settings.level = "";
                return settings;
            }
        }

    }

    /// <summary>
    /// 保存所有客戶端的玩家設置，例如頭像、卡背和正在使用的牌組
    /// 比賽開始時將發送到服務器
    /// </summary>

    [System.Serializable]
    public class PlayerSettings : INetworkSerializable
    {
        public string username;
        public string avatar;
        public string cardback;
        public int ai_level;
        public PlayerDeckSettings deck = new PlayerDeckSettings();

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref username);
            serializer.SerializeValue(ref avatar);
            serializer.SerializeValue(ref cardback);
            serializer.SerializeValue(ref ai_level);
            serializer.SerializeValue(ref deck);
        }

        public static PlayerSettings Default
        {
            get
            {
                PlayerSettings settings = new PlayerSettings();
                settings.username = "Player";
                settings.avatar = "";
                settings.cardback = "";
                settings.deck = PlayerDeckSettings.Default;
                settings.ai_level = 1;
                return settings;
            }
        }

        public static PlayerSettings DefaultAI
        {
            get
            {
                PlayerSettings settings = new PlayerSettings();
                settings.username = "AI";
                settings.avatar = "";
                settings.cardback = "";
                settings.deck = PlayerDeckSettings.Default;
                settings.ai_level = 10;
                return settings;
            }
        }

    }

    [System.Serializable]
    public class PlayerDeckSettings : INetworkSerializable
    {
        public string id;
        public string hero;
        public string[] cards;

        public PlayerDeckSettings() { cards = new string[0]; }
        public PlayerDeckSettings(UserDeckData deck) { id = deck.tid; hero = deck.hero; cards = deck.cards; FixData(); }

        public PlayerDeckSettings(DeckData deck) { 
            id = deck.id;
            hero = deck.hero != null ? deck.hero.id : "";
            cards = new string[deck.cards.Length];
            for (int i = 0; i < deck.cards.Length; i++)
                cards[i] = deck.cards[i].id;
            FixData();
        }

        //確保數據沒有損壞
        public void FixData()
        {
            if (id == null) id = "";
            if (hero == null) hero = "";
            if (cards == null) cards = new string[0];
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref id);
            serializer.SerializeValue(ref hero);
            NetworkTool.NetSerializeArray(serializer, ref cards);
        }

        public static PlayerDeckSettings Default
        {
            get
            {
                PlayerDeckSettings deck = new PlayerDeckSettings();
                deck.id = "";
                deck.hero = "";
                deck.cards = new string[0];
                return deck;
            }
        }
    }
}