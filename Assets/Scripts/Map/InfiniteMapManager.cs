using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class InfiniteMapManager : MonoBehaviour
{
    public static InfiniteMapManager Instance;
    void Awake()
    {
        Instance = this;

        if (Instance != null)
            DontDestroyOnLoad(Instance);
    }

    
    [Header("맵 타일 설정")]
    public GameObject[] mapTilePrefabs; // 맵 타일 프리팹들
    public Vector2 tileSize = new Vector2(10f, 10f); // 타일 크기
    public int renderDistance = 1; // 렌더링 거리 (플레이어 주변 몇 개 타일까지)
    
    [Header("Layer 설정")]
    public string mapLayerName = "Map"; // 맵 레이어 이름
    public bool setLayerOnChildren = true; // 자식 오브젝트들도 레이어 설정
    
    [Header("플레이어 설정")]
    public Transform player; // 플레이어 Transform
    public bool autoFindPlayer = true; // 자동으로 플레이어 찾기
    
    [Header("성능 설정")]
    public bool enablePooling = true; // 오브젝트 풀링 사용
    public int maxPoolSize = 50; // 최대 풀 크기
    public float updateInterval = 0.1f; // 업데이트 간격 (초)
    
    private Dictionary<Vector2Int, GameObject> activeTiles = new Dictionary<Vector2Int, GameObject>();
    private Queue<GameObject> tilePool = new Queue<GameObject>();
    private Vector2Int lastPlayerTilePos = new Vector2Int(int.MaxValue, int.MaxValue);
    private float lastUpdateTime = 0f;
    
    void Start()
    {
        // 플레이어 자동 찾기
        if (autoFindPlayer && player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                CharactorMove playerMove = FindObjectOfType<CharactorMove>();
                if (playerMove != null)
                {
                    player = playerMove.transform;
                }
            }
        }
        
        if (player == null)
        {
            Debug.LogWarning("플레이어를 찾을 수 없습니다!");
            return;
        }
        
        // 오브젝트 풀 초기화
        if (enablePooling)
        {
            InitializePool();
        }
        
        // 초기 맵 생성
        UpdateMapTiles();
    }
    
    void Update()
    {
        if (player == null) return;
        
        // 업데이트 간격 체크
        if (Time.time - lastUpdateTime < updateInterval) return;
        
        lastUpdateTime = Time.time;
        UpdateMapTiles();
    }
    
    private void InitializePool()
    {
        // 오브젝트 풀에 타일들 미리 생성
        for (int i = 0; i < maxPoolSize; i++)
        {
            if (mapTilePrefabs.Length > 0)
            {
                GameObject tile = Instantiate(mapTilePrefabs[Random.Range(0, mapTilePrefabs.Length)]);
                tile.SetActive(false);
                tilePool.Enqueue(tile);
            }
        }
    }
    
    private void UpdateMapTiles()
    {
        // 플레이어의 현재 타일 위치 계산
        Vector2Int currentPlayerTilePos = GetPlayerTilePosition();
        
        // 플레이어가 다른 타일에 있으면 맵 업데이트
        if (currentPlayerTilePos != lastPlayerTilePos)
        {
            lastPlayerTilePos = currentPlayerTilePos;
            GenerateMapAroundPlayer(currentPlayerTilePos);
        }
    }
    
    private Vector2Int GetPlayerTilePosition()
    {
        if (player == null) return Vector2Int.zero;
        
        int tileX = Mathf.FloorToInt(player.position.x / tileSize.x);
        int tileZ = Mathf.FloorToInt(player.position.z / tileSize.y);
        
        return new Vector2Int(tileX, tileZ);
    }
    
    private void GenerateMapAroundPlayer(Vector2Int playerTilePos)
    {
        // 기존 타일들 중에서 렌더링 거리 밖에 있는 것들 제거
        List<Vector2Int> tilesToRemove = new List<Vector2Int>();
        
        foreach (var kvp in activeTiles)
        {
            Vector2Int tilePos = kvp.Key;
            float distance = Vector2Int.Distance(tilePos, playerTilePos);
            
            if (distance > renderDistance)
            {
                tilesToRemove.Add(tilePos);
            }
        }
        
        // 거리 밖 타일들 제거
        foreach (Vector2Int tilePos in tilesToRemove)
        {
            RemoveTile(tilePos);
        }
        
        // 플레이어 주변에 새로운 타일들 생성
        for (int x = playerTilePos.x - renderDistance; x <= playerTilePos.x + renderDistance; x++)
        {
            for (int z = playerTilePos.y - renderDistance; z <= playerTilePos.y + renderDistance; z++)
            {
                Vector2Int tilePos = new Vector2Int(x, z);
                
                if (!activeTiles.ContainsKey(tilePos))
                {
                    CreateTile(tilePos);
                }
            }
        }
    }
    
    private void CreateTile(Vector2Int tilePos)
    {
        if (mapTilePrefabs.Length == 0) return;
        
        GameObject tilePrefab = mapTilePrefabs[Random.Range(0, mapTilePrefabs.Length)];
        GameObject tile;
        
        // 오브젝트 풀에서 가져오기 또는 새로 생성
        if (enablePooling && tilePool.Count > 0)
        {
            tile = tilePool.Dequeue();
            tile.SetActive(true);
        }
        else
        {
            tile = Instantiate(tilePrefab);
        }
        
        // 타일 위치 설정
        Vector3 worldPos = new Vector3(
            tilePos.x * tileSize.x,
            -.5f,
            tilePos.y * tileSize.y
        );
        
        tile.transform.position = worldPos;
        tile.transform.parent = transform;
        
        // 타일 이름 설정 (디버깅용)
        tile.name = $"MapTile_{tilePos.x}_{tilePos.y}";
        
        // 레이어 설정
        SetTileLayer(tile);
        
        // 활성 타일 목록에 추가
        activeTiles[tilePos] = tile;
        
        Debug.Log($"타일 생성: {tilePos} at {worldPos}, Layer: {mapLayerName}");
    }
    
    private void SetTileLayer(GameObject tile)
    {
        // 맵 레이어 인덱스 가져오기
        int mapLayer = LayerMask.NameToLayer(mapLayerName);
        
        if (mapLayer == -1)
        {
            Debug.LogWarning($"레이어 '{mapLayerName}'을 찾을 수 없습니다. 기본 레이어(0)를 사용합니다.");
            mapLayer = 0;
        }
        
        // 타일 자체의 레이어 설정
        tile.layer = mapLayer;
        
        // 자식 오브젝트들의 레이어도 설정 (옵션)
        if (setLayerOnChildren)
        {
            SetLayerRecursively(tile.transform, mapLayer);
        }
    }
    
    private void SetLayerRecursively(Transform parent, int layer)
    {
        // 모든 자식 오브젝트의 레이어 설정
        foreach (Transform child in parent)
        {
            child.gameObject.layer = layer;
            SetLayerRecursively(child, layer);
        }
    }
    
    private void RemoveTile(Vector2Int tilePos)
    {
        if (activeTiles.ContainsKey(tilePos))
        {
            GameObject tile = activeTiles[tilePos];
            
            if (enablePooling && tilePool.Count < maxPoolSize)
            {
                // 오브젝트 풀에 반환
                tile.SetActive(false);
                tilePool.Enqueue(tile);
            }
            else
            {
                // 완전히 제거
                Destroy(tile);
            }
            
            activeTiles.Remove(tilePos);
            Debug.Log($"타일 제거: {tilePos}");
        }
    }
    
    // 외부에서 호출할 수 있는 메서드들
    public void SetRenderDistance(int distance)
    {
        renderDistance = distance;
        UpdateMapTiles();
    }
    
    public void SetTileSize(Vector2 size)
    {
        tileSize = size;
        UpdateMapTiles();
    }
    
    public void ClearAllTiles()
    {
        foreach (var kvp in activeTiles)
        {
            if (kvp.Value != null)
            {
                if (enablePooling)
                {
                    kvp.Value.SetActive(false);
                    tilePool.Enqueue(kvp.Value);
                }
                else
                {
                    Destroy(kvp.Value);
                }
            }
        }
        
        activeTiles.Clear();
    }
    
    public int GetActiveTileCount()
    {
        return activeTiles.Count;
    }
    
    public int GetPoolSize()
    {
        return tilePool.Count;
    }
    
    // 에디터에서 렌더링 거리 시각화
    private void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.green;
            
            // 플레이어 위치
            Gizmos.DrawWireSphere(player.position, 1f);
            
            // 렌더링 거리 표시
            Gizmos.color = Color.yellow;
            Vector2Int playerTilePos = GetPlayerTilePosition();
            
            for (int x = playerTilePos.x - renderDistance; x <= playerTilePos.x + renderDistance; x++)
            {
                for (int z = playerTilePos.y - renderDistance; z <= playerTilePos.y + renderDistance; z++)
                {
                    Vector3 tilePos = new Vector3(
                        x * tileSize.x,
                        0f,
                        z * tileSize.y
                    );
                    
                    Gizmos.DrawWireCube(tilePos + Vector3.up * 0.5f, new Vector3(tileSize.x, 1f, tileSize.y));
                }
            }
        }
    }

    
}
