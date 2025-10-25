using System.Collections.Generic;
using com.cyborgAssets.inspectorButtonPro;
using Cysharp.Threading.Tasks;
using TMPro;
using TMPro.Examples;
using UniRx;
using Unity.VisualScripting;
using UnityAtoms.BaseAtoms;
using UnityEngine;
using ZombieRun.Adohi.Ranking;
using ZombieRun.Adohi.SceneManagement;
using ZombieRun.Adohi.Titles;

namespace ZombieRun.Adohi.GameSystem
{
    public class GameManager : MonoBehaviour
    {

        public FloatReference stageClearScoreConfig;
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

        [Header("Ranking")]
        public RankingSystem rankingSystem;

        public bool isFirstStage = false;


        public float timeFromStart;

        [Header("Scene Transition")]
        public SceneManagerWithTransition sceneManagerWithTransition;

        void Awake()
        {
            if (screenUI != null) screenUI.SetActive(false);

            if (isFirstStage)
            {
                currentHealth.Value = 100f;
                currentBoost.Value = 0f;
            }
        }

        void Start()
        {
            timeFromStart = 0f;

            currentScore.ObserveEveryValueChanged(x => x.Value).Subscribe(x =>
            {
                if (scoreText != null) scoreText.text = ((int)x).ToString() + "미터";

                // 현재 점수가 다음 스테이지 클리어 점수를 넘으면 자동으로 다음 스테이지로 이동
                if (x >= stageClearScoreConfig.Value && stageClearScoreConfig.Value < float.MaxValue)
                {
                    MoveNextStage();
                }
            });

            currentHealth.ObserveEveryValueChanged(x => x.Value).Subscribe(x =>
            {
                if (x <= 0f)
                {
                    GameEnd();
                }
            });

            PlayAsync().SafeAsync(this).Forget();
        }

        public void Update()
        {
            timeFromStart += Time.deltaTime;
        }

        public async UniTask PlayAsync()
        {
            if (isFirstStage)
            {
                await TitleStartAsync();
            }

            StageStart();
            //show stage ui
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
            if (isFirstStage)
            {

                timeFromStart = 0f;
                currentScore.Value = 0f;
            }

            if (screenUI != null) screenUI.SetActive(true);

        }

        [ProButton]
        public void MoveNextStage()
        {
            Debug.Log($"스테이지 {currentStage.Value} 클리어! 다음 스테이지로 이동");
            //씬 트랜지션
            //씬 이동
            sceneManagerWithTransition.LoadNextScene();


        }

        public void GameEnd()
        {
            Time.timeScale = 0f;
            StageEndAsync();
        }

        public async UniTask StageEndAsync()
        {
            rankingSystem.UpdateAndShow();
        }
    }

}
