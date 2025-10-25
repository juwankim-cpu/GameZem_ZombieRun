using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using TMPro.Examples;
using UniRx;
using Unity.VisualScripting;
using UnityAtoms.BaseAtoms;
using UnityEngine;
using ZombieRun.Adohi.Titles;

namespace ZombieRun.Adohi.GameSystem
{
    public class GameManager : MonoBehaviour
    {

        public List<FloatReference> stageClearScoreConfig;

        public IntReference currentStage;

        [Header("Score")]
        public FloatReference currentScore;
        public TextMeshProUGUI scoreText;

        public FloatReference currentHealth;
        public FloatReference currentBoost;

        [Header("Title")]
        public TitleMover titleMover;
        public KeyCode titleStartKey = KeyCode.Space;

        [Header("UI")]
        public GameObject screenUI;


        public float timeFromStart;

        void Awake()
        {
            if (screenUI != null) screenUI.SetActive(false);
        }

        void Start()
        {
            timeFromStart = 0f;

            currentScore.ObserveEveryValueChanged(x => x.Value).Subscribe(x =>
            {
                if (scoreText != null) scoreText.text = ((int)x).ToString() + "미터";
            });

            PlayAsync().SafeAsync(this).Forget();
        }

        public void Update()
        {
            timeFromStart += Time.deltaTime;
        }

        public async UniTask PlayAsync()
        {
            await TitleStartAsync();
            StageStart();
            //show stage ui
            if (screenUI != null) screenUI.SetActive(true);
            //hide stage ui
            //time scale 1
            //wait for stage end;


        }
        public async UniTask TitleStartAsync()
        {
            if (titleMover != null)
            {
                await UniTask.WaitUntil(() => Input.GetKeyDown(titleStartKey));
                await titleMover.EndAsync();
            }


        }

        public void StageStart()
        {
            currentStage.Value += 1;
            timeFromStart = 0f;

            currentScore.Value = 0f;
            currentHealth.Value = 100f;
            currentBoost.Value = 0f;
        }




        public async UniTask StageEndAsync()
        {

        }
    }

}
