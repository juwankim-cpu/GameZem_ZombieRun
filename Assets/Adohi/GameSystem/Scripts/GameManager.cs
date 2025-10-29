using System.Collections.Generic;
using Ami.BroAudio;
using com.cyborgAssets.inspectorButtonPro;
using Cysharp.Threading.Tasks;
using Pixelplacement;
using TMPro;
using TMPro.Examples;
using UniRx;
using Unity.VisualScripting;
using UnityAtoms.BaseAtoms;
using UnityEngine;
using ZombieRun.Adohi.Enemy;
using ZombieRun.Adohi.Ranking;
using ZombieRun.Adohi.SceneManagement;
using ZombieRun.Adohi.Titles;

namespace ZombieRun.Adohi.GameSystem
{
    public class GameManager : Pixelplacement.Singleton<GameManager>
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
        public HeartBeat titleHeartBeat;
        public KeyCode titleStartKey = KeyCode.Space;
        public float doubleTapTime = 0.3f; // 더블탭으로 인정할 시간 간격

        private float lastTapTime = 0f;
        private bool isDoubleTapDetected = false;

        [Header("Game Control")]
        public KeyCode quitKey = KeyCode.Escape;


        [Header("UI")]
        public GameObject screenUI;
        public Canvas stepRollCanvas;


        [Header("Ranking")]
        public RankingSystem rankingSystem;

        public bool isFirstStage = false;

        public bool initValues;


        public float timeFromStart;

        [Header("Scene Transition")]
        private bool isSceneTransition = false;
        public SceneManagerWithTransition sceneManagerWithTransition;

        [Header("Character")]
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

        [Header("Aspect Ratio (16:9 고정)")]
        [SerializeField] private bool maintainAspectRatio = true;
        [SerializeField] private float targetAspect = 16f / 9f; // 16:9
        [SerializeField] private float baseOrthographicSize = 5f; // 16:9일 때 기준 ortho size
        private Camera mainCamera;

        [Header("Mobile UI")]
        public Canvas joystickCanvas;
        public Canvas buttonCanvas;

        [Header("Tutorials")]
        public Tutorials tutorials;

        public KeyCode tutorialsKey = KeyCode.Q;


        private UnityEngine.U2D.PixelPerfectCamera pixelPerfectCamera;

        [HideInInspector] public bool IsTitleShowing = false;

        public bool IsPlaying => !IsTitleShowing && !isEnd;

        public float difficulty = 1f;
        public float difficultyRatio = 90f;
        public float healthDecreasePerSecond = 1f;

        private bool isEnd;

        // UniRx Disposables
        private System.IDisposable scoreSubscription;
        private System.IDisposable healthSubscription;

        public SoundID titleBGM;
        public SoundID stageBGM;

        [Header("Sfx")]
        public SoundID dieSfx;
        public SoundID hitSfx;
        public SoundID uiSfx;

        [Header("Mobile Mode UIs")]
        public List<GameObject> mobileModeUIs;
        public List<GameObject> pcModeUIs;


        void Awake()
        {
            Time.timeScale = 1f;

            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            // 에디터가 아닐 때 마우스 커서 숨기기
#if !UNITY_EDITOR
            Cursor.visible = false;
#endif

            // 빌드 타입에 따라 UI 자동 켜기/끄기 (에디터는 제외)
#if !UNITY_EDITOR
#if UNITY_ANDROID || UNITY_IOS
            // 모바일 빌드: PC 모드 UI 끄기
            foreach (var ui in pcModeUIs)
            {
                if (ui != null) ui.SetActive(false);
            }
#else
            // PC 빌드: 모바일 모드 UI 끄기
            foreach (var ui in mobileModeUIs)
            {
                if (ui != null) ui.SetActive(false);
            }
#endif
#endif

            if (joystickCanvas != null) joystickCanvas.gameObject.SetActive(false);
            if (buttonCanvas != null) buttonCanvas.gameObject.SetActive(false);

            if (screenUI != null) screenUI.SetActive(false);

            // 메인 카메라 설정
            mainCamera = Camera.main;
            if (mainCamera != null)
            {
                pixelPerfectCamera = mainCamera.GetComponent<UnityEngine.U2D.PixelPerfectCamera>();
                if (pixelPerfectCamera == null)
                {
                    Debug.LogWarning("메인 카메라에 PixelPerfectCamera 컴포넌트가 없습니다!");
                }

                // 초기 Aspect Ratio 설정
                if (maintainAspectRatio)
                {
                    AdjustOrthographicSize();
                }
            }

            if (isFirstStage)
            {
                IsTitleShowing = true;
                character.GetComponent<Animator>().SetTrigger("IsTitleStart");


            }

            else
            {
                IsTitleShowing = true;


            }

            if (initValues)
            {
                currentHealth.Value = 100f;
                currentBoost.Value = 0f;
                currentScore.Value = 0f;
            }

            BroAudio.Stop(BroAudioType.All);
            BroAudio.SetVolume(BroAudioType.All, 1f);


        }

        void Start()
        {
            timeFromStart = 0f;

            // 기존 구독이 있으면 먼저 해제
            scoreSubscription?.Dispose();
            healthSubscription?.Dispose();

            scoreSubscription = currentScore.ObserveEveryValueChanged(x => x.Value).Subscribe(x =>
            {
                if (scoreText != null) scoreText.text = ((int)x).ToString() + "미터";

                // 현재 점수가 다음 스테이지 클리어 점수를 넘으면 자동으로 다음 스테이지로 이동
                if (x >= stageClearScoreConfig.Value && stageClearScoreConfig.Value < float.MaxValue)
                {
                    MoveNextStage();
                }
            });

            healthSubscription = currentHealth.ObserveEveryValueChanged(x => x.Value).Subscribe(x =>
            {
                if (x <= 0f)
                {
                    GameEnd();
                }
            });


            if (initValues)
            {
                BroAudio.Play(titleBGM, 1f).AsBGM();
            }

            currentBoost.Value += 0.01f;

            PlayAsync().SafeAsync(this).Forget();


            // 백그라운드에서 랭킹 데이터 미리 로드 (게임 종료 시 빠른 표시를 위해)
            if (rankingSystem != null)
            {
                rankingSystem.PreloadRankingsAsync().Forget();
            }


        }

        public void Update()
        {
            timeFromStart += Time.deltaTime;

            // Aspect Ratio 유지 (화면 크기 변경 감지)
            if (maintainAspectRatio && mainCamera != null)
            {
                AdjustOrthographicSize();
            }

            if (IsTitleShowing)
            {
                if (Input.GetKeyDown(tutorialsKey) || Input.GetKeyDown(KeyCode.Escape))
                {
                    tutorials.Toggle();
                }

                // 입력 감지 (튜토리얼이 꺼져있을 때만)
                if (tutorials == null || !tutorials.gameObject.activeSelf)
                {
                    // 키보드: 한 번만 눌러도 시작
                    if (Input.GetKeyDown(titleStartKey))
                    {
                        isDoubleTapDetected = true;
                    }

                    // 모바일: 더블탭 필요
                    if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                    {
                        float currentTime = Time.realtimeSinceStartup;

                        if (currentTime - lastTapTime <= doubleTapTime)
                        {
                            // 더블탭 감지됨
                            isDoubleTapDetected = true;
                        }

                        lastTapTime = currentTime;
                    }
                }
            }

            if (IsPlaying)
            {
                currentScore.Value += scorePerSecond * Time.deltaTime * speedMultiply;
            }

            if (IsPlaying)
            {

                currentHealth.Value -= healthDecreasePerSecond * difficulty * Time.deltaTime;
                currentBoost.Value += 0.0001f * Time.deltaTime;
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                Restart();
            }

            if (Input.GetKeyDown(quitKey))
            {
                QuitGame();
            }

            difficulty = currentScore.Value / difficultyRatio + 1f;
        }

        public async UniTask PlayAsync()
        {
            if (initValues)
            {
                await TitleStartAsync();
            }
            character.GetComponent<Animator>().SetTrigger("IsTitleEnd");

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

                stepRollCanvas.gameObject.SetActive(true);
                await UniTask.Delay(3000);
                titleHeartBeat.StartBeating();


                // 더블탭 대기
                isDoubleTapDetected = false;
                lastTapTime = 0f;
                await UniTask.WaitUntil(() => isDoubleTapDetected);

                BroAudio.Stop(BroAudioType.Music, 1f);
                BroAudio.Play(uiSfx);
                titleHeartBeat.StopBeating();
                stepRollCanvas.gameObject.SetActive(false);
                await titleMover.EndAsync();
                IsTitleShowing = false;
            }


        }

        public void StageStart()
        {
            if (screenUI != null) screenUI.SetActive(true);

            if (initValues)
            {

                timeFromStart = 0f;
                currentScore.Value = 0f;
                currentHealth.Value = 100f;
                currentBoost.Value = 0f;

            }
            BroAudio.SetVolume(BroAudioType.Music, 1f);
            BroAudio.Play(stageBGM, 2f).AsBGM();
            enemyManager.StartSpawn();
            IsTitleShowing = false;
            // 게임 시작 시 줌아웃 효과
            ZoomCamera(zoomStartPPU, zoomEndPPU, zoomDuration).Forget();


#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
            if (joystickCanvas != null) joystickCanvas.gameObject.SetActive(true);
            if (buttonCanvas != null) buttonCanvas.gameObject.SetActive(true);
#endif
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
            BroAudio.Stop(BroAudioType.Music, 1f);
            //씬 트랜지션
            //씬 이동
            sceneManagerWithTransition.LoadNextScene();


        }

        void OnDestroy()
        {
            // UniRx 구독 해제
            scoreSubscription?.Dispose();
            healthSubscription?.Dispose();
        }


        public void GameEnd()
        {
            if (isEnd) return;
            isEnd = true;
            BroAudio.Stop(BroAudioType.All);
            BroAudio.Play(dieSfx);

            Time.timeScale = 0f;
            StageEndAsync().SafeAsync(this).Forget();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            sceneManagerWithTransition.LoadSceneByName("Stage_1");
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
            Debug.Log("게임 종료");
        }

        /// <summary>
        /// 16:9 Aspect Ratio를 유지하도록 Orthographic Size 조정
        /// - 가로가 더 길면: Letterbox로 처리 (LetterboxManager 사용)
        /// - 세로가 더 길면: Orthographic Size를 늘림
        /// </summary>
        private void AdjustOrthographicSize()
        {
            if (mainCamera == null || !mainCamera.orthographic) return;

            float currentAspect = (float)Screen.width / Screen.height;

            // 현재 화면이 목표 aspect보다 세로가 길면 (currentAspect < targetAspect)
            // Orthographic Size를 늘려서 세로 영역을 더 보여줌
            if (currentAspect < targetAspect)
            {
                // 세로가 더 긴 경우
                float aspectRatio = targetAspect / currentAspect;
                mainCamera.orthographicSize = baseOrthographicSize * aspectRatio;
            }
            else
            {
                // 가로가 더 길거나 16:9인 경우 - 기본 ortho size 사용
                // Letterbox는 LetterboxManager에서 처리
                mainCamera.orthographicSize = baseOrthographicSize;
            }
        }

        public UniTask StageEndAsync()
        {
            rankingSystem.UpdateAndShow();
            return UniTask.CompletedTask;
        }
    }

}
