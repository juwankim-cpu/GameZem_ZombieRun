using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance;
    public GameObject[] obstaclePrefabs;
    
    

    public MovingDistance movingDistance;
   
    
    public float minSpeed = 3.0f; // 최소 속도
    public float maxSpeed = 7.0f; // 최대 속도
    
    public float obstacleSpawnInterval = 2.0f; 
    
    // ⭐️ Y축 랜덤 범위 변수를 추가합니다.
    public float minYPosition = -2.0f; 
    public float maxYPosition = 2.0f;

    private Transform playerTransform;

    public float spawnXOffset = 10.0f;

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
        GameObject.Find("Obstacle").SetActive(false);
        StartCoroutine(SpawnObstaclesCoroutine());
    }

        // ⭐️ 장애물 생성 로직을 담당하는 코루틴입니다.
    IEnumerator SpawnObstaclesCoroutine()
    {
        // 씬 로드가 완료될 때까지 대기
        yield return null; 

        // 씬이 바뀌어도 계속 실행되도록 무한 루프 설정
        while (true)
        {
            if (GameStatus.sitDown)
            {
                yield return null; // 한 프레임 대기 (CPU 부하 방지)
                continue; // 아래 생성 로직을 건너뛰고 루프 처음으로 돌아가 다시 sitDown 상태를 확인합니다.
            }
            // // ⭐️ 먼저 정해진 시간 간격만큼 대기합니다.
            // yield return new WaitForSeconds(obstacleSpawnInterval); 
            // int currentGoal = 0;

            // // ⭐️ goals[0]를 안전하게 숫자로 변환합니다.
            // // 변환에 성공하면 currentGoal에 값이 저장되고, 실패하면 currentGoal은 0을 유지합니다.
            // if (goals != null && goals.Length > 0 && int.TryParse(goals[0], out currentGoal))
            // {
            //     // 변환 성공
            //     // Debug.Log($"현재 목표 거리: {currentGoal}"); // 디버그용
            // }
            // else
            // {
            //     // 변환 실패 (예: goals[0]이 null, 빈 문자열 또는 "미터"인 경우)
            //     Debug.LogWarning($"목표 거리 문자열 '{goals?[0]}'을(를) 숫자로 변환할 수 없습니다. 현재 스테이지 계산에 기본값 0을 사용합니다.");
            //     currentGoal = 0;
            // }

            // ⭐️ currentGoal 변수를 사용하여 스테이지를 판단합니다.
            if (movingDistance.currentDistance < 150 )
            {
                // Stage1에서 2개의 장애물 생성
                SpawnObstacle(obstaclePrefabs[0]);
                
            }
             if (150 <= movingDistance.currentDistance && movingDistance.currentDistance < 300)
            {
                // Stage2에서 2개의 장애물 생성
                SpawnObstacle(obstaclePrefabs[1]);
                
            }
            if (300 <= movingDistance.currentDistance && movingDistance.currentDistance < 450)
            {
                // Stage3에서 1개의 장애물 생성
                SpawnObstacle(obstaclePrefabs[2]);
            }
            if (movingDistance.currentDistance >= 450)
                SpawnObstacle(obstaclePrefabs[obstaclePrefabs.Length -1]);
        }
    }

    // ⭐️ 장애물을 생성하고 위치, 이동, 파괴를 담당하는 별도의 함수
    void SpawnObstacle(GameObject prefab)
    {
        if (prefab == null) return;

        // 1. Y축 위치와 속도를 랜덤으로 결정합니다.
        float randomY = Random.Range(minYPosition, maxYPosition);
        float randomSpeed = Random.Range(minSpeed, maxSpeed);

        // ⭐️ 변경: 장애물이 생성될 X축 위치를 캐릭터 위치에 기반하여 계산
        float spawnX;
        if (playerTransform != null)
        {
            // 캐릭터의 현재 X 위치 + 오프셋 (캐릭터가 이동해도 X축 위치를 따라감)
            spawnX = playerTransform.position.x + spawnXOffset; 
        }
        else
        {
            // 캐릭터를 찾지 못했으면 이전의 고정된 X 위치를 사용
            spawnX = 8.86f; 
            Debug.LogWarning("플레이어 Transform을 찾을 수 없어 X=8.86f에 생성합니다.");
        }


        // 2. 장애물 생성
        var obstacle = Instantiate(prefab);

        // 3. ⭐️ 초기 위치 설정: (spawnX, randomY, 0)
        obstacle.transform.position = new Vector3(spawnX, randomY, 0f); 

        // 4. 이동 및 파괴를 위한 컴포넌트 추가
        ObstacleMover mover = obstacle.AddComponent<ObstacleMover>();
        
        // 5. 랜덤으로 결정된 속도를 전달합니다.
        mover.moveSpeed = randomSpeed;
    }

    
}
