using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using LeTai.Asset.TranslucentImage;
using UnityAtoms.BaseAtoms;
using UnityEngine;
using UnityEngine.Networking;

namespace ZombieRun.Adohi.Ranking
{
    [Serializable]
    public class RankingEntry
    {
        public string playerName;
        public float score;
        public string dateTime;

        public RankingEntry(string playerName, float score)
        {
            this.playerName = playerName;
            this.score = score;
            this.dateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    [Serializable]
    public class RankingData
    {
        public List<RankingEntry> rankings = new List<RankingEntry>();
    }

    public class RankingSystem : MonoBehaviour
    {
        private const string RANKING_KEY = "ZombieRun_Rankings";
        private const int MAX_RANKINGS = 5;

        [Header("Ranking Settings")]
        public FloatReference currentScore;
        [Tooltip("Awake 시 자동으로 현재 점수를 랭킹에 등록할지 여부")]
        [SerializeField] private bool autoRegisterOnAwake = true;
        [Tooltip("랭킹에 등록할 플레이어 이름")]
        [SerializeField] private string defaultPlayerName = "Player";

        [Header("Online Ranking Settings")]
        [Tooltip("온라인 랭킹 사용 여부")]
        [SerializeField] private bool useOnlineRanking = false;
        [Tooltip("구글 시트 Web App URL (Google Apps Script 배포 URL)")]
        [SerializeField] private string googleSheetWebAppUrl = "";
        [Tooltip("온라인 요청 타임아웃 시간 (초)")]
        [SerializeField] private float requestTimeout = 10f;
        [Tooltip("온라인 요청 실패 시 자동으로 오프라인 모드로 전환")]
        [SerializeField] private bool autoFallbackToOffline = true;

        [Header("Reset Settings")]
        [Tooltip("랭킹을 리셋할 키코드")]
        [SerializeField] private KeyCode resetKeyCode = KeyCode.Delete;
        [Tooltip("리셋 키를 활성화할지 여부")]
        [SerializeField] private bool enableResetKey = true;

        private RankingData rankingData;
        private bool isOnlineMode = false; // 현재 온라인 모드 사용 중인지

        public TMPro.TextMeshProUGUI currentScoreText;
        public List<TMPro.TextMeshProUGUI> rankingScoreTexts;

        public UIAnimation rankingUIAnimation;
        public TranslucentImage blurImage;

        [Header("Blur Animation Settings")]
        [SerializeField] private float blurTargetAlpha = 0.8f;
        [SerializeField] private float blurFadeDuration = 0.3f;
        [SerializeField] private Ease blurFadeEase = Ease.OutQuad;

        private Tween blurTween;

        // 중복 등록 방지용 플래그
        private bool hasRegisteredThisSession = false;


        void Awake()
        {
            blurImage.color = new Color(blurImage.color.r, blurImage.color.g, blurImage.color.b, 0f);

            // 온라인 모드 초기화
            isOnlineMode = useOnlineRanking && !string.IsNullOrEmpty(googleSheetWebAppUrl);
            if (isOnlineMode)
            {
                Debug.Log("[RankingSystem] 📡 온라인 랭킹 모드 활성화 (연결 실패 시 자동으로 오프라인 전환)");
            }
            else
            {
                Debug.Log("[RankingSystem] 💾 오프라인 랭킹 모드");
            }
        }

        void Update()
        {
            // 리셋 키가 활성화되어 있고, 해당 키가 눌렸을 때
            if (enableResetKey && Input.GetKeyDown(resetKeyCode))
            {
                Debug.Log($"[RankingSystem] {resetKeyCode} 키가 눌렸습니다. 랭킹을 리셋합니다.");
                ClearAllRankings();
            }
        }

        public void UpdateAndShow()
        {
            UpdateAndShowAsync().Forget();
        }

        private async UniTaskVoid UpdateAndShowAsync()
        {
            var startTime = Time.realtimeSinceStartup;
            Debug.Log($"[RankingSystem] ⏱️ UpdateAndShow 시작 (모드: {(isOnlineMode ? "온라인" : "오프라인")})");

            // 랭킹 데이터가 이미 로드되어 있지 않으면 로드
            if (rankingData == null || rankingData.rankings == null)
            {
                Debug.LogWarning($"[RankingSystem] ❌ 미리 로드된 데이터 없음! 지금 로드 시작... (이러면 느려짐!)");
                if (isOnlineMode)
                {
                    // 온라인 모드: 구글 시트에서 데이터 로드
                    Debug.Log($"[RankingSystem] 📡 온라인 로드 시작...");
                    var loadStart = Time.realtimeSinceStartup;
                    bool success = await LoadRankingsOnline();
                    Debug.Log($"[RankingSystem] 📡 온라인 로드 결과: {(success ? "성공" : "실패")} ({(Time.realtimeSinceStartup - loadStart):F3}초)");

                    if (!success && autoFallbackToOffline)
                    {
                        Debug.Log("[RankingSystem] 📡→💾 온라인 연결 실패 → 오프라인 모드로 전환");
                        isOnlineMode = false;
                        LoadRankings();
                    }
                }
                else
                {
                    // 오프라인 모드: 로컬에서 데이터 로드
                    LoadRankings();
                }
                Debug.Log($"[RankingSystem] ⏱️ 로드 완료 ({(Time.realtimeSinceStartup - startTime):F3}초)");
            }
            else
            {
                Debug.Log($"[RankingSystem] ⚡ 미리 로드된 랭킹 데이터 사용! (항목: {rankingData.rankings.Count}개) ({(Time.realtimeSinceStartup - startTime):F3}초)");
            }

            // 현재 점수를 랭킹에 등록 (중복 등록 방지)
            if (autoRegisterOnAwake && currentScore != null && currentScore.Value > 0 && !hasRegisteredThisSession)
            {
                // 점수 등록 (즉시 로컬에 추가하고 UI에 표시)
                AddScoreToRankingData(defaultPlayerName, currentScore.Value);
                hasRegisteredThisSession = true;

                // 온라인 저장은 백그라운드에서 (기다리지 않음)
                if (isOnlineMode)
                {
                    SaveRankingsOnlineAsync().Forget();
                }
            }

            Debug.Log($"[RankingSystem] ⏱️ 점수 등록 완료 ({(Time.realtimeSinceStartup - startTime):F3}초)");

            // UI 업데이트
            UpdateRankingDisplay();

            Debug.Log($"[RankingSystem] ⏱️ UI 데이터 업데이트 완료 ({(Time.realtimeSinceStartup - startTime):F3}초)");

            // UI 표시
            Debug.Log($"[RankingSystem] 🎬 UI 애니메이션 시작...");
            await rankingUIAnimation.Show();

            Debug.Log($"[RankingSystem] ✅ UI 애니메이션 완료! ({(Time.realtimeSinceStartup - startTime):F3}초)");

            blurTween?.Kill();
            blurTween = blurImage.DOFade(blurTargetAlpha, blurFadeDuration).SetEase(blurFadeEase).SetUpdate(true);

            Debug.Log($"[RankingSystem] ✅ 블러 효과 시작! 총 시간: {(Time.realtimeSinceStartup - startTime):F3}초");
        }

        /// <summary>
        /// 점수를 랭킹 데이터에 추가 (로컬 처리, 즉시 완료)
        /// </summary>
        private void AddScoreToRankingData(string playerName, float score)
        {
            if (rankingData == null)
            {
                rankingData = new RankingData();
            }

            // 새로운 랭킹 엔트리 생성
            RankingEntry newEntry = new RankingEntry(playerName, score);

            // 랭킹 리스트에 추가
            rankingData.rankings.Add(newEntry);

            // 점수 기준 내림차순 정렬
            rankingData.rankings = rankingData.rankings
                .OrderByDescending(entry => entry.score)
                .ToList();

            // 상위 MAX_RANKINGS개만 유지
            if (rankingData.rankings.Count > MAX_RANKINGS)
            {
                rankingData.rankings = rankingData.rankings.Take(MAX_RANKINGS).ToList();
            }

            // 로컬에 즉시 저장
            SaveRankings();

            Debug.Log($"[RankingSystem] 점수 등록 완료: {playerName} - {(int)score}미터");
        }

        /// <summary>
        /// 랭킹 UI 업데이트 (데이터가 없으면 빈 텍스트 또는 기본값 표시)
        /// </summary>
        public void UpdateRankingDisplay()
        {
            // 랭킹 데이터가 null이면 초기화
            if (rankingData == null)
            {
                rankingData = new RankingData();
            }

            // 현재 점수 표시
            if (currentScoreText != null && currentScore != null)
            {
                currentScoreText.text = ((int)(currentScore.Value)).ToString() + "미터";
            }

            // 랭킹 점수 표시
            if (rankingScoreTexts != null)
            {
                for (int i = 0; i < rankingScoreTexts.Count; i++)
                {
                    if (rankingScoreTexts[i] != null)
                    {
                        // 랭킹 데이터가 존재하는 경우
                        if (rankingData.rankings != null && i < rankingData.rankings.Count)
                        {
                            rankingScoreTexts[i].text = ((int)(rankingData.rankings[i].score)).ToString() + "미터";
                        }
                        else
                        {
                            // 랭킹 데이터가 없는 경우 빈 텍스트 표시
                            rankingScoreTexts[i].text = "-";
                        }
                    }
                }
            }
        }

        /// <summary>
        /// PlayerPrefs에서 랭킹 데이터 로드
        /// </summary>
        private void LoadRankings()
        {
            if (PlayerPrefs.HasKey(RANKING_KEY))
            {
                string json = PlayerPrefs.GetString(RANKING_KEY);
                rankingData = JsonUtility.FromJson<RankingData>(json);
            }
            else
            {
                rankingData = new RankingData();
            }
        }

        /// <summary>
        /// PlayerPrefs에 랭킹 데이터 저장
        /// </summary>
        private void SaveRankings()
        {
            // rankingData가 null이면 초기화
            if (rankingData == null)
            {
                rankingData = new RankingData();
            }

            string json = JsonUtility.ToJson(rankingData);
            PlayerPrefs.SetString(RANKING_KEY, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 새로운 점수를 랭킹에 등록
        /// </summary>
        /// <param name="playerName">플레이어 이름</param>
        /// <param name="score">점수</param>
        /// <returns>랭킹에 등록되었는지 여부 (상위 5등 안에 들었는지)</returns>
        public bool RegisterScore(string playerName, float score)
        {
            // 새로운 랭킹 엔트리 생성
            RankingEntry newEntry = new RankingEntry(playerName, score);

            // 랭킹 리스트에 추가
            rankingData.rankings.Add(newEntry);

            // 점수 기준 내림차순 정렬 (높은 점수가 위로)
            rankingData.rankings = rankingData.rankings
                .OrderByDescending(entry => entry.score)
                .ToList();

            // 상위 5개만 유지
            bool isInTopRanking = false;
            if (rankingData.rankings.Count > MAX_RANKINGS)
            {
                // 새로 추가된 항목이 상위 5개 안에 있는지 확인
                isInTopRanking = rankingData.rankings.Take(MAX_RANKINGS).Any(e => e == newEntry);
                rankingData.rankings = rankingData.rankings.Take(MAX_RANKINGS).ToList();
            }
            else
            {
                isInTopRanking = true;
            }

            // 저장
            SaveRankings();

            // UI 업데이트
            UpdateRankingDisplay();

            return isInTopRanking;
        }

        /// <summary>
        /// 현재 점수를 랭킹에 등록 (currentScore 사용)
        /// </summary>
        /// <param name="playerName">플레이어 이름</param>
        /// <returns>랭킹에 등록되었는지 여부</returns>
        public bool RegisterCurrentScore(string playerName)
        {
            if (currentScore != null)
            {
                return RegisterScore(playerName, currentScore.Value);
            }
            return false;
        }

        /// <summary>
        /// 상위 5등까지의 랭킹 가져오기
        /// </summary>
        /// <returns>상위 랭킹 리스트</returns>
        public List<RankingEntry> GetTopRankings()
        {
            return rankingData.rankings.Take(MAX_RANKINGS).ToList();
        }

        /// <summary>
        /// 특정 등수의 랭킹 엔트리 가져오기
        /// </summary>
        /// <param name="rank">등수 (1부터 시작)</param>
        /// <returns>해당 등수의 랭킹 엔트리 (없으면 null)</returns>
        public RankingEntry GetRankingByPosition(int rank)
        {
            if (rank < 1 || rank > rankingData.rankings.Count)
            {
                return null;
            }
            return rankingData.rankings[rank - 1];
        }

        /// <summary>
        /// 특정 점수가 몇 등인지 확인
        /// </summary>
        /// <param name="score">확인할 점수</param>
        /// <returns>예상 등수 (1부터 시작, 랭킹 외면 -1)</returns>
        public int GetRankPosition(float score)
        {
            int position = 1;
            foreach (var entry in rankingData.rankings)
            {
                if (score > entry.score)
                {
                    return position;
                }
                position++;
            }

            // 현재 랭킹보다 낮은 점수인 경우
            if (rankingData.rankings.Count < MAX_RANKINGS)
            {
                return position; // 아직 5개 미만이면 등록 가능
            }
            return -1; // 랭킹 외
        }

        /// <summary>
        /// 모든 랭킹 데이터 삭제 및 UI 업데이트
        /// </summary>
        public void ClearAllRankings()
        {
            rankingData.rankings.Clear();
            SaveRankings();
            UpdateRankingDisplay();
            Debug.Log("[RankingSystem] 랭킹이 리셋되었습니다.");
        }

        /// <summary>
        /// 랭킹 데이터를 콘솔에 출력 (디버그용)
        /// </summary>
        public void PrintRankings()
        {
            Debug.Log("=== Top Rankings ===");
            for (int i = 0; i < rankingData.rankings.Count; i++)
            {
                var entry = rankingData.rankings[i];
                Debug.Log($"{i + 1}등: {entry.playerName} - {entry.score}점 ({entry.dateTime})");
            }
        }

        // ========== 온라인 랭킹 메서드 ==========

        /// <summary>
        /// 랭킹 데이터 미리 로드 (백그라운드에서 실행)
        /// 게임 시작 시 미리 호출하면 나중에 빠르게 표시 가능
        /// </summary>
        public async UniTask PreloadRankingsAsync()
        {
            Debug.Log("[RankingSystem] 🔄 랭킹 데이터 미리 로드 시작...");

            if (isOnlineMode)
            {
                // 온라인 모드: 구글 시트에서 데이터 로드
                bool success = await LoadRankingsOnline();
                if (!success && autoFallbackToOffline)
                {
                    Debug.Log("[RankingSystem] 📡→💾 온라인 연결 실패 → 오프라인 모드로 전환");
                    isOnlineMode = false;
                    LoadRankings();
                }
            }
            else
            {
                // 오프라인 모드: 로컬에서 데이터 로드
                LoadRankings();
            }

            Debug.Log("[RankingSystem] ✓ 랭킹 데이터 미리 로드 완료");
        }

        /// <summary>
        /// 구글 시트에서 랭킹 데이터 로드
        /// </summary>
        private async UniTask<bool> LoadRankingsOnline()
        {
            if (string.IsNullOrEmpty(googleSheetWebAppUrl))
            {
                Debug.LogError("[RankingSystem] Google Sheet Web App URL이 설정되지 않았습니다.");
                return false;
            }

            try
            {
                // GET 요청으로 랭킹 데이터 가져오기
                string url = $"{googleSheetWebAppUrl}?action=get";

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.timeout = (int)requestTimeout;

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string json = request.downloadHandler.text;
                        rankingData = JsonUtility.FromJson<RankingData>(json);

                        if (rankingData == null)
                        {
                            rankingData = new RankingData();
                        }

                        Debug.Log($"[RankingSystem] 온라인 랭킹 로드 성공: {rankingData.rankings.Count}개 항목");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"[RankingSystem] 온라인 연결 실패: {request.error} → 오프라인 모드로 전환");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                // 타임아웃이나 네트워크 오류는 정상적인 상황 (오프라인 환경)
                Debug.LogWarning($"[RankingSystem] 온라인 연결 불가 ({e.Message}) → 오프라인 랭킹 사용");
                return false;
            }
        }

        /// <summary>
        /// 구글 시트에 랭킹 데이터 저장
        /// </summary>
        private async UniTask<bool> SaveRankingsOnline()
        {
            if (string.IsNullOrEmpty(googleSheetWebAppUrl))
            {
                Debug.LogError("[RankingSystem] Google Sheet Web App URL이 설정되지 않았습니다.");
                return false;
            }

            try
            {
                string json = JsonUtility.ToJson(rankingData);

                WWWForm form = new WWWForm();
                form.AddField("action", "save");
                form.AddField("data", json);

                using (UnityWebRequest request = UnityWebRequest.Post(googleSheetWebAppUrl, form))
                {
                    request.timeout = (int)requestTimeout;

                    await request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log("[RankingSystem] 온라인 랭킹 저장 성공 ✓");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"[RankingSystem] 온라인 저장 실패: {request.error} (로컬에는 저장됨)");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                // 타임아웃이나 네트워크 오류는 정상적인 상황 (오프라인 환경)
                Debug.LogWarning($"[RankingSystem] 온라인 저장 불가 ({e.Message}) (로컬에는 저장됨)");
                return false;
            }
        }

        /// <summary>
        /// 온라인으로 점수 등록
        /// </summary>
        private async UniTask<bool> RegisterCurrentScoreOnline(string playerName)
        {
            if (currentScore == null)
                return false;

            // 랭킹 데이터가 없으면 로드 (미리 로드되어 있으면 스킵)
            if (rankingData == null || rankingData.rankings == null)
            {
                Debug.Log("[RankingSystem] 랭킹 데이터가 없어서 로드합니다...");
                bool loadSuccess = await LoadRankingsOnline();
                if (!loadSuccess && autoFallbackToOffline)
                {
                    Debug.Log("[RankingSystem] 📡→💾 온라인 연결 실패 → 오프라인으로 등록");
                    isOnlineMode = false;
                    RegisterCurrentScore(playerName);
                    return false;
                }
            }
            else
            {
                Debug.Log("[RankingSystem] ⚡ 미리 로드된 데이터 사용 - 로드 시간 절약!");
            }

            // 새로운 랭킹 엔트리 생성
            RankingEntry newEntry = new RankingEntry(playerName, currentScore.Value);

            // 랭킹 리스트에 추가
            rankingData.rankings.Add(newEntry);

            // 점수 기준 내림차순 정렬
            rankingData.rankings = rankingData.rankings
                .OrderByDescending(entry => entry.score)
                .ToList();

            // 상위 MAX_RANKINGS개만 유지
            if (rankingData.rankings.Count > MAX_RANKINGS)
            {
                rankingData.rankings = rankingData.rankings.Take(MAX_RANKINGS).ToList();
            }

            // 로컬에 즉시 저장 (백업용)
            SaveRankings();

            // 온라인으로 저장 (백그라운드에서 실행 - 기다리지 않음!)
            SaveRankingsOnlineAsync().Forget();

            return true;
        }

        /// <summary>
        /// 온라인 저장을 백그라운드에서 실행
        /// </summary>
        private async UniTaskVoid SaveRankingsOnlineAsync()
        {
            Debug.Log("[RankingSystem] 백그라운드에서 온라인 저장 시도 중...");
            bool saveSuccess = await SaveRankingsOnline();

            if (!saveSuccess)
            {
                Debug.Log("[RankingSystem] 온라인 저장 실패했지만 로컬에는 저장되어 있습니다.");
            }
        }
    }
}
