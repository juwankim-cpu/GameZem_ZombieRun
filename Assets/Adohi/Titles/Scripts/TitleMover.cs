using System;
using com.cyborgAssets.inspectorButtonPro;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ZombieRun.Adohi.Titles
{
    public class TitleMover : MonoBehaviour
    {
        public UIAnimation titleAnimation;
        public UIAnimation spaceKeyAnimation;
        public GameObject spaceKey;

        public float startDelay = 5f;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        async void Start()
        {
            spaceKey.SetActive(false);
            await UniTask.Delay(TimeSpan.FromSeconds(startDelay));
            spaceKey.SetActive(true);
            titleAnimation.Show().Forget();
            spaceKeyAnimation.Show().Forget();
        }

        [ProButton]
        public void End()
        {
            titleAnimation.Hide().Forget();
            spaceKeyAnimation.Hide().Forget();

        }

        public async UniTask EndAsync()
        {
            await UniTask.WhenAll(titleAnimation.Hide(), spaceKeyAnimation.Hide());
            spaceKey.SetActive(false);
        }
    }

}
