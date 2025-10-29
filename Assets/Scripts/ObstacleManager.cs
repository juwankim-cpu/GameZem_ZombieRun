using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityAtoms.BaseAtoms;
using ZombieRun.Adohi.GameSystem;
public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance;
    public GameObject[] obstaclePrefabs;



    public MovingDistance movingDistance;


    public float minSpeed = 3.0f; // 최소 속도
    public float maxSpeed = 7.0f; // 최대 속도

    public float minObstacleSpawnInterval = 2.0f;
    public float maxObstacleSpawnInterval = 3.0f;

    // ⭐️ Y축 랜덤 범위 변수를 추가합니다.
    public float minYPosition = -2.0f;
    public float maxYPosition = 2.0f;

    public float minTitleYOffset;
    public float maxTitleYOffset;

    private Transform playerTransform;

    public float spawnXOffset = 10.0f;

    [Header("초기 스폰 설정")]
    [Tooltip("게임 시작 시 미리 생성할 장애물 개수")]
    public int initialObstacleCount = 5;
    [Tooltip("초기 장애물 생성 X 최소 위치")]
    public float initialSpawnMinX = 0f;
    [Tooltip("초기 장애물 생성 X 최대 위치")]
    public float initialSpawnMaxX = 15f;

    void Awake()
    {
        Instance = this;

        if (Instance != null)
            DontDestroyOnLoad(Instance);
    }
    void Start()
    {
        // if (goalDistance != null)
        // {
        //     // 예: "123 미터" -> ["123", "미터"]
        //     goals = goalDistance.text.Split(' ');
        //     // goals[0]에 "123"이 들어갑니다.

        //     // 디버그 확인 (선택 사항)
        //     if (goals.Length > 0)
        //     {
        //         Debug.Log($"목표 거리 텍스트에서 추출된 숫자: {goals[0]}");
        //     }
        // }
        // else
        // {
        //     Debug.LogError("goalDistance 텍스트 컴포넌트가 할당되지 않았습니다.");
        //     // 오류 방지를 위해 임시 값 설정 (선택 사항)
        //     goals = new string[] { "0", "미터" }; 
        // }



        CharactorMove player = FindObjectOfType<CharactorMove>();
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("CharactorMove 컴포넌트를 가진 플레이어 오브젝트를 찾을 수 없습니다! 장애물 생성 위치가 고정됩니다.");
        }

        // 초기 장애물 생성
        SpawnInitialObstacles();

        //GameObject.Find("Obstacle").SetActive(false);
        StartCoroutine(SpawnObstaclesCoroutine());
    }

    /// <summary>
    /// 게임 시작 시 초기 장애물 생성
    /// </summary>
    void SpawnInitialObstacles()
    {
        if (obstaclePrefabs == null || obstaclePrefabs.Length == 0) return;
        if (initialObstacleCount <= 0) return;

        // X 범위(min ~ max)를 장애물 개수만큼 등분
        float totalRange = initialSpawnMaxX - initialSpawnMinX;
        float sectionWidth = totalRange / initialObstacleCount;

        for (int i = 0; i < initialObstacleCount; i++)
        {
            // 스테이지에 맞는 프리팹 선택
            GameObject prefab = GetPrefabByStage();

            // i번째 구간의 시작과 끝
            float sectionStart = initialSpawnMinX + (i * sectionWidth);
            float sectionEnd = initialSpawnMinX + ((i + 1) * sectionWidth);

            // 해당 구간 내에서 랜덤 X 위치
            float randomX = Random.Range(sectionStart, sectionEnd);

            // Y 위치를 타이틀을 피한 안전 범위에서 생성
            // 위쪽 안전 범위: [maxYPosition - maxTitleYOffset, maxYPosition]
            // 아래쪽 안전 범위: [minYPosition, minYPosition + minTitleYOffset]
            float randomY;
            if (Random.value > 0.5f)
            {
                // 위쪽 안전 범위
                randomY = Random.Range(maxYPosition - maxTitleYOffset, maxYPosition);
            }
            else
            {
                // 아래쪽 안전 범위
                randomY = Random.Range(minYPosition, minYPosition + minTitleYOffset);
            }

            // 커스텀 X 위치로 장애물 생성 (Y는 기존 로직 사용 - 타이틀 회피)
            SpawnObstacleAtPosition(prefab, randomX, randomY);
        }
    }

    /// <summary>
    /// 현재 스테이지에 맞는 프리팹 반환
    /// </summary>
    GameObject GetPrefabByStage()
    {
        int stage = GameManager.Instance.currentStage.Value;

        if (stage == 0 && obstaclePrefabs.Length > 0)
            return obstaclePrefabs[0];
        else if (stage == 1 && obstaclePrefabs.Length > 1)
            return obstaclePrefabs[1];
        else if (stage == 2 && obstaclePrefabs.Length > 2)
            return obstaclePrefabs[2];
        else if (stage == 3)
            return obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];

        // 기본값
        return obstaclePrefabs[0];
    }

    // ⭐️ 장애물 생성 로직을 담당하는 코루틴입니다.
    IEnumerator SpawnObstaclesCoroutine()
    {
        // 씬 로드가 완료될 때까지 대기
        yield return null;

        // 씬이 바뀌어도 계속 실행되도록 무한 루프 설정
        while (true)
        {

            var obstacleSpawnInterval = Random.Range(minObstacleSpawnInterval, maxObstacleSpawnInterval);

            // 스테이지에 맞는 프리팹으로 장애물 생성
            GameObject prefab = GetPrefabByStage();
            SpawnObstacle(prefab);

            yield return new WaitForSeconds(obstacleSpawnInterval);
        }
    }

    // ⭐️ 장애물을 생성하고 위치, 이동, 파괴를 담당하는 별도의 함수 (기본 위치)
    void SpawnObstacle(GameObject prefab)
    {
        // 기본 spawnXOffset 위치 사용
        float spawnX = playerTransform != null ? spawnXOffset : 15f;
        SpawnObstacle(prefab, spawnX);
    }

    // ⭐️ 장애물을 생성하고 위치, 이동, 파괴를 담당하는 별도의 함수 (커스텀 X 위치)
    void SpawnObstacle(GameObject prefab, float customSpawnX)
    {
        if (prefab == null) return;

        // 1. Y축 위치와 속도를 랜덤으로 결정합니다.
        float randomY = Random.Range(minYPosition, maxYPosition);

        // 타이틀 화면일 때 Y 위치 제한 (타이틀과 겹치지 않게)
        if (!GameManager.Instance.IsPlaying)
        {
            if (Random.value > 0.5f)
            {
                // 위쪽 안전 범위
                randomY = Random.Range(maxYPosition - maxTitleYOffset, maxYPosition);
            }
            else
            {
                // 아래쪽 안전 범위
                randomY = Random.Range(minYPosition, minYPosition + minTitleYOffset);
            }

        }

        SpawnObstacleAtPosition(prefab, customSpawnX, randomY);
    }

    // ⭐️ 장애물을 생성하고 위치, 이동, 파괴를 담당하는 별도의 함수 (커스텀 X, Y 위치)
    void SpawnObstacleAtPosition(GameObject prefab, float customSpawnX, float customSpawnY)
    {
        if (prefab == null) return;

        float randomSpeed = Random.Range(minSpeed, maxSpeed);

        // 장애물 생성
        var obstacle = Instantiate(prefab);

        // 초기 위치 설정: (customSpawnX, customSpawnY, 0)
        obstacle.transform.position = new Vector3(customSpawnX, customSpawnY, 0f);

        // 이동 및 파괴를 위한 컴포넌트 추가
        ObstacleMover mover = obstacle.GetComponent<ObstacleMover>();

        // 랜덤으로 결정된 속도를 전달합니다.
        mover.moveSpeed = randomSpeed;
    }


}
