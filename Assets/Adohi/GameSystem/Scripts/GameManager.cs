using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using TMPro.Examples;
using UniRx;
using Unity.VisualScripting;
using UnityAtoms.BaseAtoms;
using UnityEngine;
using ZombieRun.Adohi.Ranking;
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
        private float nextStageClearScore; // 다음 스테이지로 넘어갈 목표 점수

        public FloatReference currentHealth;
        public FloatReference currentBoost;

        [Header("Title")]
        public TitleMover titleMover;
        public KeyCode titleStartKey = KeyCode.Space;

        public BackgroundController backgroundController;

        [Header("UI")]
        public GameObject screenUI;

        [Header("Ranking")]
        public RankingSystem rankingSystem;


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

                // 현재 점수가 다음 스테이지 클리어 점수를 넘으면 자동으로 다음 스테이지로 이동
                if (x >= nextStageClearScore && nextStageClearScore < float.MaxValue)
                {
                    MoveNextStage();
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

            // 현재 스테이지의 클리어 점수 설정
            int stageIndex = currentStage.Value - 1;
            if (stageIndex >= 0 && stageIndex < stageClearScoreConfig.Count)
            {
                nextStageClearScore = stageClearScoreConfig[stageIndex].Value;
                Debug.Log($"스테이지 {currentStage.Value} 시작! 목표 점수: {nextStageClearScore}미터");
            }
            else
            {
                // 마지막 스테이지는 무한대
                nextStageClearScore = float.MaxValue;
                Debug.Log($"마지막 스테이지 {currentStage.Value} 시작! (무한)");
            }
        }

        public void MoveNextStage()
        {
            Debug.Log($"스테이지 {currentStage.Value} 클리어! 다음 스테이지로 이동");

            // 배경을 다음 스테이지로 이동
            backgroundController.MoveNextStage(currentStage.Value);

            // 다음 스테이지 시작 (점수 초기화 및 다음 스테이지 목표 점수 설정)
            StageStart();
        }


        public async UniTask StageEndAsync()
        {
            rankingSystem.UpdateAndShow();
        }
    }

}
