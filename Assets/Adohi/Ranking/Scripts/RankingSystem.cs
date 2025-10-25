using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityAtoms.BaseAtoms;
using UnityEngine;

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

        [Header("Reset Settings")]
        [Tooltip("랭킹을 리셋할 키코드")]
        [SerializeField] private KeyCode resetKeyCode = KeyCode.Delete;
        [Tooltip("리셋 키를 활성화할지 여부")]
        [SerializeField] private bool enableResetKey = true;

        private RankingData rankingData;

        public TMPro.TextMeshProUGUI currentScoreText;
        public List<TMPro.TextMeshProUGUI> rankingScoreTexts;

        public UIAnimation rankingUIAnimation;

        void Awake()
        {
            // 1. 기존 랭킹 데이터 로드
            LoadRankings();

            // 2. 현재 점수를 랭킹에 등록 (RegisterScore 내부에서 자동으로 SaveRankings 호출됨)
            if (autoRegisterOnAwake && currentScore != null && currentScore.Value > 0)
            {
                RegisterCurrentScore(defaultPlayerName);
            }

            // 3. UI 업데이트
            UpdateRankingDisplay();

            rankingUIAnimation.Show().SafeAsync(this).Forget();
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
            LoadRankings();

            // 2. 현재 점수를 랭킹에 등록 (RegisterScore 내부에서 자동으로 SaveRankings 호출됨)
            if (autoRegisterOnAwake && currentScore != null && currentScore.Value > 0)
            {
                RegisterCurrentScore(defaultPlayerName);
            }

            // 3. UI 업데이트
            UpdateRankingDisplay();

            rankingUIAnimation.Show().SafeAsync(this).Forget();
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
    }
}
