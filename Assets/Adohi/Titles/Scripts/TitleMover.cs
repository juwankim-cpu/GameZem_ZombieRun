using System;
using com.cyborgAssets.inspectorButtonPro;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ZombieRun.Adohi.Titles
{
    public class TitleMover : MonoBehaviour
    {
        public UIAnimation uiAnimation;
        public GameObject spaceKey;

        public float startDelay = 5f;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        async void Start()
        {
            spaceKey.SetActive(false);
            await UniTask.Delay(TimeSpan.FromSeconds(startDelay));
            spaceKey.SetActive(true);
            uiAnimation.Show().Forget();
        }

        [ProButton]
        public void End()
        {
            uiAnimation.Hide().Forget();
            spaceKey.SetActive(false);
        }
    }

}
