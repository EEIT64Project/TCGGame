using System.Collections;
using System.Collections.Generic;
using TcgEngine.UI;
using UnityEngine;

namespace TcgEngine.Client
{
    /// <summary>
    /// 添加到場景中的組件，以向競技場添加一些通用的 sfx/音樂
    /// </summary>

    public class SceneSettings : MonoBehaviour
    {
        public AudioClip music;
        public AudioClip start_audio;
        public AudioClip[] game_music;
        public AudioClip[] game_ambience;

        private static SceneSettings instance;

        private void Awake()
        {
            instance = this;
        }

        void Start()
        {
            AudioTool.Get().PlayMusic("music", music);
            AudioTool.Get().PlaySFX("game_sfx", start_audio);
            if (game_music.Length > 0)
                AudioTool.Get().PlayMusic("music", game_music[Random.Range(0, game_music.Length)]);
            if (game_ambience.Length > 0)
                AudioTool.Get().PlaySFX("ambience", game_ambience[Random.Range(0, game_ambience.Length)], 0.5f, true);
        }

        void Update()
        {

        }

        public void FadeToScene(string scene)
        {
            StartCoroutine(FadeToRun(scene));
        }

        private IEnumerator FadeToRun(string scene)
        {
            BlackPanel.Get().Show();
            AudioTool.Get().FadeOutMusic("music");
            yield return new WaitForSeconds(1f);
            SceneNav.GoTo(scene);
        }

        public static SceneSettings Get()
        {
            return instance;
        }
    }
}
