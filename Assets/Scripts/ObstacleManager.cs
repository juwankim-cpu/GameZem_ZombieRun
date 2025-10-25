using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance;
    public GameObject[] obstaclePrefabs;
    
    public string stageName;

    public float minSpeed = 3.0f; // 최소 속도
    public float maxSpeed = 7.0f; // 최대 속도
    
    public float obstacleSpawnInterval = 2.0f; 
    
    // ⭐️ Y축 랜덤 범위 변수를 추가합니다.
    public float minYPosition = -2.0f; 
    public float maxYPosition = 2.0f;

    void Awake()
    {
        Instance = this;

        if (Instance != null)
            DontDestroyOnLoad(Instance);
    }
    void Start()
    {
        stageName = SceneManager.GetActiveScene().name;
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
            // ⭐️ 먼저 정해진 시간 간격만큼 대기합니다.
            yield return new WaitForSeconds(obstacleSpawnInterval); 

            // ⭐️ 현재 스테이지에 따라 장애물을 생성합니다.
            if (stageName == "Stage1")
            {
                // Stage1에서 2개의 장애물 생성
                SpawnObstacle(obstaclePrefabs[0]);
                
            }
            else if (stageName == "Stage2")
            {
                // Stage2에서 2개의 장애물 생성
                SpawnObstacle(obstaclePrefabs[2]);
                SpawnObstacle(obstaclePrefabs[3]);
            }
            else if (stageName == "Stage3")
            {
                // Stage3에서 1개의 장애물 생성
                SpawnObstacle(obstaclePrefabs[obstaclePrefabs.Length - 1]);
            }
            // 스테이지가 추가되면 여기에 else if 블록을 추가합니다.
        }
    }

    // ⭐️ 장애물을 생성하고 위치, 이동, 파괴를 담당하는 별도의 함수
    void SpawnObstacle(GameObject prefab)
    {
        if (prefab == null) return;

        // 1. ⭐️ Y축 위치와 속도를 랜덤으로 결정합니다.
        float randomY = Random.Range(minYPosition, maxYPosition);
        float randomSpeed = Random.Range(minSpeed, maxSpeed);

        // 2. 장애물 생성
        var obstacle = Instantiate(prefab);

        // 3. ⭐️ 초기 위치 설정: (8.86, randomY, 0)
        obstacle.transform.position = new Vector3(8.86f, randomY, 0f); 

        // 4. 이동 및 파괴를 위한 컴포넌트 추가
        ObstacleMover mover = obstacle.AddComponent<ObstacleMover>();
        
        // 5. ⭐️ 랜덤으로 결정된 속도를 전달합니다.
        mover.moveSpeed = randomSpeed;
    }

    
}
