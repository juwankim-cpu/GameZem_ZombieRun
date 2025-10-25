using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;
using com.cyborgAssets.inspectorButtonPro;

public class BackgroundObjectMover : MonoBehaviour
{
    [Header("오브젝트 설정")]
    [SerializeField] private bool useSprites = true; // true: Sprite 사용, false: Prefab 사용
    [SerializeField] private Sprite[] sprites; // 스프라이트 배열 (구름 등)
    [SerializeField] private GameObject[] objectPrefabs; // 프리팹 배열 (useSprites=false일 때 사용)
    [SerializeField] private float moveSpeed = 2f; // 이동 속도
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 0;

    [Header("스폰 위치 설정")]
    [SerializeField] private float spawnOffsetX = 15f; // 카메라 오른쪽 밖에서 스폰
    [SerializeField] private float heightMin = 0f; // 최소 높이
    [SerializeField] private float heightMax = 5f; // 최대 높이

    [Header("스폰 간격 설정")]
    [SerializeField] private float baseInterval = 3f; // 기본 간격 (초)
    [SerializeField] private float intervalRandomRange = 2f; // 간격 랜덤 범위 (±)
    [SerializeField] private float minInterval = 1f; // 최소 간격

    [Header("초기 설정")]
    [SerializeField] private bool autoStart = true; // 자동 시작
    [SerializeField] private float startDelay = 0f; // 시작 지연
    [SerializeField] private int initialSpawnCount = 3; // 초기 스폰 개수

    [Header("풀링 설정")]
    [SerializeField] private bool usePooling = true;
    [SerializeField] private int poolSize = 10;

    [Header("제거 설정")]
    [SerializeField] private float despawnOffsetX = -15f; // 카메라 왼쪽 밖에서 제거

    [Header("디버그")]
    [SerializeField] private bool showDebugInfo = false;

    private List<GameObject> objectPool = new List<GameObject>();
    private List<BackgroundObject> activeObjects = new List<BackgroundObject>();
    private bool isSpawning = false;
    private float nextSpawnTime = 0f;

    private class BackgroundObject
    {
        public GameObject gameObject;
        public int prefabIndex;
    }

    void Start()
    {
        // 검증
        if (useSprites)
        {
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogError("BackgroundObjectMover: sprites가 설정되지 않았습니다!");
                return;
            }
        }
        else
        {
            if (objectPrefabs == null || objectPrefabs.Length == 0)
            {
                Debug.LogError("BackgroundObjectMover: objectPrefabs가 설정되지 않았습니다!");
                return;
            }
        }

        if (usePooling)
        {
            InitializePool();
        }

        if (autoStart)
        {
            StartSpawning().Forget();
        }
    }

    void Update()
    {
        // 모든 활성 오브젝트 이동
        MoveObjects();

        // 화면 밖으로 나간 오브젝트 제거
        CheckAndDespawnObjects();

        // 자동 스폰
        if (isSpawning && Time.time >= nextSpawnTime)
        {
            SpawnObject();
            ScheduleNextSpawn();
        }
    }

    /// <summary>
    /// 풀 초기화
    /// </summary>
    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj;

            if (useSprites)
            {
                // 스프라이트로 오브젝트 생성
                int spriteIndex = UnityEngine.Random.Range(0, sprites.Length);
                obj = CreateSpriteObject(spriteIndex);
            }
            else
            {
                // 프리팹 인스턴스화
                int prefabIndex = UnityEngine.Random.Range(0, objectPrefabs.Length);
                obj = Instantiate(objectPrefabs[prefabIndex], transform);
            }

            obj.SetActive(false);
            objectPool.Add(obj);
        }

        if (showDebugInfo)
        {
            Debug.Log($"BackgroundObjectMover: 풀 초기화 완료 ({poolSize}개)");
        }
    }

    /// <summary>
    /// 스프라이트로 GameObject 생성
    /// </summary>
    GameObject CreateSpriteObject(int spriteIndex)
    {
        GameObject obj = new GameObject($"CloudSprite_{spriteIndex}");
        obj.transform.SetParent(transform);

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = sprites[spriteIndex];
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder;

        return obj;
    }

    /// <summary>
    /// 풀에서 오브젝트 가져오기
    /// </summary>
    GameObject GetObjectFromPool(int index)
    {
        GameObject obj;

        if (usePooling && objectPool.Count > 0)
        {
            obj = objectPool[0];
            objectPool.RemoveAt(0);
            obj.SetActive(true);

            // 스프라이트 모드일 때 Sorting Order 업데이트
            if (useSprites)
            {
                SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sortingLayerName = sortingLayerName;
                    sr.sortingOrder = sortingOrder;
                    // 스프라이트도 업데이트 (인덱스에 맞게)
                    if (index >= 0 && index < sprites.Length)
                    {
                        sr.sprite = sprites[index];
                    }
                }
            }
        }
        else
        {
            // 풀이 비었으면 새로 생성
            if (useSprites)
            {
                obj = CreateSpriteObject(index);
            }
            else
            {
                obj = Instantiate(objectPrefabs[index], transform);
            }
        }

        return obj;
    }

    /// <summary>
    /// 풀에 오브젝트 반환
    /// </summary>
    void ReturnObjectToPool(GameObject obj)
    {
        if (obj == null) return;

        if (usePooling)
        {
            obj.SetActive(false);
            objectPool.Add(obj);
        }
        else
        {
            Destroy(obj);
        }
    }

    /// <summary>
    /// 스폰 시작
    /// </summary>
    public async UniTask StartSpawning()
    {
        if (isSpawning)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("BackgroundObjectMover: 이미 스폰 중입니다.");
            }
            return;
        }

        if (startDelay > 0)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        isSpawning = true;

        // 초기 오브젝트 스폰 (화면 채우기)
        SpawnInitialObjects();

        // 다음 스폰 예약
        ScheduleNextSpawn();

        if (showDebugInfo)
        {
            Debug.Log("BackgroundObjectMover: 스폰 시작");
        }
    }

    /// <summary>
    /// 스폰 중지
    /// </summary>
    public void StopSpawning()
    {
        isSpawning = false;

        if (showDebugInfo)
        {
            Debug.Log("BackgroundObjectMover: 스폰 중지");
        }
    }

    /// <summary>
    /// 초기 오브젝트 스폰
    /// </summary>
    void SpawnInitialObjects()
    {
        float currentX = GetSpawnPositionX();
        float intervalUsed = baseInterval;

        for (int i = 0; i < initialSpawnCount; i++)
        {
            // 랜덤 높이
            float height = UnityEngine.Random.Range(heightMin, heightMax);

            // 스폰
            SpawnObjectAt(currentX, height);

            // 다음 위치 계산
            intervalUsed = CalculateNextInterval();
            currentX -= intervalUsed * moveSpeed; // 왼쪽으로 간격만큼
        }
    }

    /// <summary>
    /// 오브젝트 스폰
    /// </summary>
    void SpawnObject()
    {
        float spawnX = GetSpawnPositionX();
        float spawnY = UnityEngine.Random.Range(heightMin, heightMax);

        SpawnObjectAt(spawnX, spawnY);
    }

    /// <summary>
    /// 특정 위치에 오브젝트 스폰
    /// </summary>
    void SpawnObjectAt(float x, float y)
    {
        // 랜덤 인덱스 선택
        int index = useSprites
            ? UnityEngine.Random.Range(0, sprites.Length)
            : UnityEngine.Random.Range(0, objectPrefabs.Length);

        GameObject obj = GetObjectFromPool(index);
        obj.transform.position = new Vector3(x, y, 0f);

        BackgroundObject bgObj = new BackgroundObject
        {
            gameObject = obj,
            prefabIndex = index
        };

        activeObjects.Add(bgObj);

        if (showDebugInfo)
        {
            Debug.Log($"BackgroundObjectMover: 스폰 완료 - 위치: ({x:F2}, {y:F2}), Index: {index}");
        }
    }

    /// <summary>
    /// 스폰 위치 X 좌표 계산
    /// </summary>
    float GetSpawnPositionX()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            return cam.transform.position.x + cam.orthographicSize * cam.aspect + spawnOffsetX;
        }
        return transform.position.x + spawnOffsetX;
    }

    /// <summary>
    /// 제거 위치 X 좌표 계산
    /// </summary>
    float GetDespawnPositionX()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            return cam.transform.position.x - cam.orthographicSize * cam.aspect + despawnOffsetX;
        }
        return transform.position.x + despawnOffsetX;
    }

    /// <summary>
    /// 다음 간격 계산
    /// </summary>
    float CalculateNextInterval()
    {
        float randomOffset = UnityEngine.Random.Range(-intervalRandomRange, intervalRandomRange);
        float interval = baseInterval + randomOffset;
        return Mathf.Max(interval, minInterval);
    }

    /// <summary>
    /// 다음 스폰 예약
    /// </summary>
    void ScheduleNextSpawn()
    {
        float interval = CalculateNextInterval();
        nextSpawnTime = Time.time + interval;

        if (showDebugInfo)
        {
            Debug.Log($"BackgroundObjectMover: 다음 스폰까지 {interval:F2}초");
        }
    }

    /// <summary>
    /// 모든 오브젝트 이동
    /// </summary>
    void MoveObjects()
    {
        foreach (var bgObj in activeObjects)
        {
            if (bgObj.gameObject != null)
            {
                bgObj.gameObject.transform.position += Vector3.left * moveSpeed * Time.deltaTime;
            }
        }
    }

    /// <summary>
    /// 화면 밖 오브젝트 제거
    /// </summary>
    void CheckAndDespawnObjects()
    {
        float despawnX = GetDespawnPositionX();

        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            var bgObj = activeObjects[i];

            if (bgObj.gameObject != null && bgObj.gameObject.transform.position.x < despawnX)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"BackgroundObjectMover: 오브젝트 제거 - 위치: {bgObj.gameObject.transform.position.x:F2}");
                }

                ReturnObjectToPool(bgObj.gameObject);
                activeObjects.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 속도 설정
    /// </summary>
    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
        if (showDebugInfo)
        {
            Debug.Log($"BackgroundObjectMover: 속도 변경 - {speed}");
        }
    }

    /// <summary>
    /// 간격 설정
    /// </summary>
    public void SetSpawnInterval(float interval, float randomRange)
    {
        baseInterval = interval;
        intervalRandomRange = randomRange;

        if (showDebugInfo)
        {
            Debug.Log($"BackgroundObjectMover: 간격 변경 - {interval} ± {randomRange}");
        }
    }

    /// <summary>
    /// 높이 범위 설정
    /// </summary>
    public void SetHeightRange(float min, float max)
    {
        heightMin = min;
        heightMax = max;

        if (showDebugInfo)
        {
            Debug.Log($"BackgroundObjectMover: 높이 범위 변경 - {min} ~ {max}");
        }
    }

    /// <summary>
    /// 모든 오브젝트 제거
    /// </summary>
    public void ClearAllObjects()
    {
        foreach (var bgObj in activeObjects)
        {
            if (bgObj.gameObject != null)
            {
                ReturnObjectToPool(bgObj.gameObject);
            }
        }
        activeObjects.Clear();

        if (showDebugInfo)
        {
            Debug.Log("BackgroundObjectMover: 모든 오브젝트 제거");
        }
    }

    /// <summary>
    /// Sorting Layer 설정 (Sprite 모드에서만 적용)
    /// </summary>
    public void SetSortingLayer(string layerName, int order)
    {
        sortingLayerName = layerName;
        sortingOrder = order;

        // 스프라이트 모드일 때만 업데이트
        if (useSprites)
        {
            // 활성 오브젝트의 Sorting Layer 업데이트
            foreach (var bgObj in activeObjects)
            {
                if (bgObj.gameObject != null)
                {
                    SpriteRenderer sr = bgObj.gameObject.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingLayerName = layerName;
                        sr.sortingOrder = order;
                    }
                }
            }

            // 풀에 있는 오브젝트도 업데이트
            foreach (var obj in objectPool)
            {
                if (obj != null)
                {
                    SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingLayerName = layerName;
                        sr.sortingOrder = order;
                    }
                }
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"BackgroundObjectMover: Sorting Layer 변경 - {layerName}, Order: {order}");
        }
    }

    /// <summary>
    /// 스프라이트 배열 설정
    /// </summary>
    public void SetSprites(Sprite[] newSprites)
    {
        if (newSprites == null || newSprites.Length == 0)
        {
            Debug.LogError("BackgroundObjectMover: 유효하지 않은 스프라이트 배열입니다!");
            return;
        }

        sprites = newSprites;
        useSprites = true;

        if (showDebugInfo)
        {
            Debug.Log($"BackgroundObjectMover: 스프라이트 변경 - {newSprites.Length}개");
        }
    }

    /// <summary>
    /// 프리팹 배열 설정
    /// </summary>
    public void SetPrefabs(GameObject[] newPrefabs)
    {
        if (newPrefabs == null || newPrefabs.Length == 0)
        {
            Debug.LogError("BackgroundObjectMover: 유효하지 않은 프리팹 배열입니다!");
            return;
        }

        objectPrefabs = newPrefabs;
        useSprites = false;

        if (showDebugInfo)
        {
            Debug.Log($"BackgroundObjectMover: 프리팹 변경 - {newPrefabs.Length}개");
        }
    }

    void OnDestroy()
    {
        isSpawning = false;

        // 활성 오브젝트 정리
        foreach (var bgObj in activeObjects)
        {
            if (bgObj.gameObject != null)
            {
                Destroy(bgObj.gameObject);
            }
        }
        activeObjects.Clear();

        // 풀 정리
        foreach (var obj in objectPool)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        objectPool.Clear();
    }

    // ========== 테스트 버튼들 ==========

    [ProButton]
    public async void TestStartSpawning()
    {
        await StartSpawning();
    }

    [ProButton]
    public void TestStopSpawning()
    {
        StopSpawning();
    }

    [ProButton]
    public void TestSpawnOne()
    {
        SpawnObject();
    }

    [ProButton]
    public void TestClearAll()
    {
        ClearAllObjects();
    }

    [ProButton]
    public void TestSpeedUp()
    {
        SetMoveSpeed(moveSpeed * 2f);
    }

    [ProButton]
    public void TestSpeedDown()
    {
        SetMoveSpeed(moveSpeed * 0.5f);
    }

    [ProButton]
    public void TestSetIntervalFast()
    {
        SetSpawnInterval(1f, 0.5f);
    }

    [ProButton]
    public void TestSetIntervalSlow()
    {
        SetSpawnInterval(5f, 2f);
    }

    [ProButton]
    public void TestSetHeightLow()
    {
        SetHeightRange(-2f, 2f);
    }

    [ProButton]
    public void TestSetHeightHigh()
    {
        SetHeightRange(3f, 7f);
    }

    [ProButton]
    public void TestToggleDebug()
    {
        showDebugInfo = !showDebugInfo;
        Debug.Log($"디버그 정보: {(showDebugInfo ? "ON" : "OFF")}");
    }

    [ProButton]
    public void TestToggleUseSprites()
    {
        useSprites = !useSprites;
        Debug.Log($"스프라이트 모드: {(useSprites ? "ON (Sprites)" : "OFF (Prefabs)")}");
        Debug.Log("풀을 다시 초기화하려면 재시작하세요.");
    }

    [ProButton]
    public void TestSetSortingOrder0()
    {
        SetSortingLayer(sortingLayerName, 0);
    }

    [ProButton]
    public void TestSetSortingOrder5()
    {
        SetSortingLayer(sortingLayerName, 5);
    }

    [ProButton]
    public void TestSetSortingOrder10()
    {
        SetSortingLayer(sortingLayerName, 10);
    }

    [ProButton]
    public void TestSetSortingOrderMinus5()
    {
        SetSortingLayer(sortingLayerName, -5);
    }

    [ProButton]
    public void TestPrintStatus()
    {
        Debug.Log($"=== BackgroundObjectMover 상태 ===");
        Debug.Log($"모드: {(useSprites ? "Sprite" : "Prefab")}");
        Debug.Log($"스폰 중: {isSpawning}");
        Debug.Log($"활성 오브젝트: {activeObjects.Count}");
        Debug.Log($"풀 오브젝트: {objectPool.Count}");
        Debug.Log($"이동 속도: {moveSpeed}");
        Debug.Log($"스폰 간격: {baseInterval} ± {intervalRandomRange} (최소: {minInterval})");
        Debug.Log($"높이 범위: {heightMin} ~ {heightMax}");
        Debug.Log($"다음 스폰까지: {(nextSpawnTime - Time.time):F2}초");

        if (useSprites)
        {
            Debug.Log($"스프라이트 개수: {(sprites != null ? sprites.Length : 0)}");
            Debug.Log($"Sorting Layer: {sortingLayerName}, Order: {sortingOrder}");
        }
        else
        {
            Debug.Log($"프리팹 개수: {(objectPrefabs != null ? objectPrefabs.Length : 0)}");
        }
    }
}
