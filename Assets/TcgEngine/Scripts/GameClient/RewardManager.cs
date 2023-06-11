using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine.Client
{
    //Grants rewards for adventure solo mode

    public class RewardManager : MonoBehaviour
    {
        private bool reward_gained = false;

        private static RewardManager instance;

        void Awake()
        {
            instance = this;
        }

        private void Start()
        {
            GameClient.Get().onGameEnd += OnGameEnd;
        }

        void OnGameEnd(int winner)
        {
            int player_id = GameClient.Get().GetPlayerID();
            if (GameClient.game_settings.play_mode == PlayMode.Adventure && winner == player_id)
            {
                UserData udata = Authenticator.Get().UserData;
                LevelData level = LevelData.Get(GameClient.game_settings.level);
                if (level != null && !udata.HasReward(level.id) && !reward_gained)
                {
                    if (Authenticator.Get().IsTest())
                        GainRewardTest(level);
                    if (Authenticator.Get().IsApi())
                        GainRewardAPI(level);
                }
            }
        }

        private async void GainRewardTest(LevelData level)
        {
            UserData udata = Authenticator.Get().UserData;
            udata.coins += level.reward_coins;
            udata.xp += level.reward_xp;
            udata.AddReward(level.id);

            foreach (CardData card in level.reward_cards)
            {
                udata.AddCard(card.id, 1);
            }

            foreach (PackData pack in level.reward_packs)
            {
                udata.AddPack(pack.id, 1);
            }

            reward_gained = true;
            await Authenticator.Get().SaveUserData();
        }

        private async void GainRewardAPI(LevelData level)
        {
            RewardGainRequest req = new RewardGainRequest();
            req.reward = level.id;

            string url = ApiClient.ServerURL + "/users/rewards/gain/" + ApiClient.Get().UserID;
            string json = ApiTool.ToJson(req);
            WebResponse res = await ApiClient.Get().SendPostRequest(url, json);
            Debug.Log("Gain Reward: " + level.id + " " + res.success);
            reward_gained = res.success;
        }

        public bool IsRewardGained()
        {
            return reward_gained;
        }

        public static RewardManager Get()
        {
            return instance;
        }
    }
}
