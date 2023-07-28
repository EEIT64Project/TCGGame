using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TcgEngine
{
    //默認效果和音頻，有些可以在每個單獨的卡上覆蓋

    [CreateAssetMenu(fileName = "AssetData", menuName = "TcgEngine/AssetData", order = 0)]
    public class AssetData : ScriptableObject
    {
        [Header("FX")]
        public GameObject card_spawn_fx;
        public GameObject card_destroy_fx;
        public GameObject card_attack_fx;
        public GameObject card_damage_fx;
        public GameObject card_exhausted_fx;
        public GameObject player_damage_fx;
        public GameObject damage_fx;
        public GameObject play_card_fx;
        public GameObject play_card_other_fx;
        public GameObject play_secret_fx;
        public GameObject play_secret_other_fx;
        public GameObject dice_roll_fx;
        public GameObject hover_text_box;
        public GameObject new_turn_fx;
        public GameObject win_fx;
        public GameObject lose_fx;
        public GameObject tied_fx;

        [Header("Audio")]
        public AudioClip card_spawn_audio;
        public AudioClip card_destroy_audio;
        public AudioClip card_attack_audio;
        public AudioClip card_move_audio;
        public AudioClip card_damage_audio;
        public AudioClip player_damage_audio;
        public AudioClip hand_card_click_audio;
        public AudioClip new_turn_audio;
        public AudioClip win_audio;
        public AudioClip defeat_audio;
        public AudioClip win_music;
        public AudioClip defeat_music;

        public static AssetData Get()
        {
            return DataLoader.Get().assets;
        }
    }
}
