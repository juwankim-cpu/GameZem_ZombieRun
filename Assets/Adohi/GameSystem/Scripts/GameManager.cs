using System.Collections.Generic;
using com.cyborgAssets.inspectorButtonPro;
using Cysharp.Threading.Tasks;
using Pixelplacement;
using TMPro;
using TMPro.Examples;
using UniRx;
using UnityAtoms.BaseAtoms;
using UnityEngine;
using ZombieRun.Adohi.Enemy;
using ZombieRun.Adohi.Ranking;
using ZombieRun.Adohi.SceneManagement;
using ZombieRun.Adohi.Titles;

namespace ZombieRun.Adohi.GameSystem
{
    public class GameManager : Singleton<GameManager>
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
        private bool isSceneTransition = false;
        public SceneManagerWithTransition sceneManagerWithTransition;

        [Header("Charactter")]
        public CharactorMove character;

        [Header("Enemy")]
        public EnemyManager enemyManager;

        [Header("Difficulty")]
        public float scorePerSecond = 1f;
        public float speedMultiply = 1f;

        [Header("Camera Zoom")]
        public int zoomStartPPU = 100;
        public int zoomEndPPU = 50;
        public float zoomDuration = 2f;

        private UnityEngine.U2D.PixelPerfectCamera pixelPerfectCamera;

        public bool IsTitleShowing;

        public bool IsPlaying => !IsTitleShowing;

        public float difficulty = 1f;
        public float healthDecreasePerSecond = 1f;

        private bool isEnd;

        void Awake()
        {
            if (screenUI != null) screenUI.SetActive(false);

            // 메인 카메라에서 PixelPerfectCamera 컴포넌트 가져오기
            if (Camera.main != null)
            {
                pixelPerfectCamera = Camera.main.GetComponent<UnityEngine.U2D.PixelPerfectCamera>();
                if (pixelPerfectCamera == null)
                {
                    Debug.LogWarning("메인 카메라에 PixelPerfectCamera 컴포넌트가 없습니다!");
                }
            }

            if (isFirstStage)
            {
                currentHealth.Value = 100f;
                currentBoost.Value = 0f;
                IsTitleShowing = true;
                character.GetComponent<Animator>().SetTrigger("IsTitleStart");


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

            if (IsPlaying && !GameStatus.sitDown)
            {
                currentScore.Value += scorePerSecond * Time.deltaTime * speedMultiply;
            }

            if (IsPlaying)
            {

                currentHealth.Value -= healthDecreasePerSecond * difficulty * Time.deltaTime;
            }


        }

        public async UniTask PlayAsync()
        {
            if (isFirstStage)
            {
                await TitleStartAsync();
                character.GetComponent<Animator>().SetTrigger("IsTitleEnd");
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
                await UniTask.Delay(2000);
                await UniTask.WaitUntil(() => Input.GetKeyDown(titleStartKey));
                await titleMover.EndAsync();
                IsTitleShowing = false;
            }


        }

        public void StageStart()
        {
            if (screenUI != null) screenUI.SetActive(true);

            if (isFirstStage)
            {

                timeFromStart = 0f;
                currentScore.Value = 0f;
                currentHealth.Value = 100f;
                currentBoost.Value = 0f;

            }


            enemyManager.StartSpawn();

            // 게임 시작 시 줌아웃 효과
            ZoomCamera(zoomStartPPU, zoomEndPPU, zoomDuration).Forget();
        }

        /// <summary>
        /// 카메라 PPU를 a에서 b까지 일정 시간 동안 변화
        /// </summary>
        public async UniTask ZoomCamera(int startPPU, int endPPU, float duration)
        {
            if (pixelPerfectCamera == null)
            {
                Debug.LogWarning("PixelPerfectCamera가 설정되지 않았습니다!");
                return;
            }

            float elapsed = 0f;
            pixelPerfectCamera.assetsPPU = startPPU;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Ease Out Quad 효과
                t = 1f - (1f - t) * (1f - t);

                pixelPerfectCamera.assetsPPU = (int)Mathf.Lerp(startPPU, endPPU, t);

                await UniTask.Yield();
            }

            pixelPerfectCamera.assetsPPU = endPPU;
        }

        [ProButton]
        public void MoveNextStage()
        {
            if (isSceneTransition) return;
            isSceneTransition = true;

            Debug.Log($"스테이지 {currentStage.Value} 클리어! 다음 스테이지로 이동");
            //씬 트랜지션
            //씬 이동
            sceneManagerWithTransition.LoadNextScene();


        }

        public void GetHit(float damage)
        {
            currentHealth.Value -= damage;
            if (currentHealth.Value <= 0f && !isEnd)
            {
                isEnd = true;

                GameEnd();
            }
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
