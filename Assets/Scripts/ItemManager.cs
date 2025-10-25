using UnityEngine;
using System.Collections;

public class ItemManager : MonoBehaviour
{
    public HPManager hpManager; 
    
    [Header("아이템 설정")]
    public GameObject hpItemPrefab; // HP 아이템 프리팹
    public GameObject buffItemPrefab; // 버프 아이템 프리팹
    public float itemSpawnInterval = 5f; // 아이템 생성 간격 (초)
    public float spawnAreaRadius = 10f; // 아이템 생성 범위 반경
    
    [Header("아이템 효과")]
    public float hpRecoveryAmount = 5f; // HP 아이템 회복량
    public float buffGainAmount = 5f; // 버프 아이템 획득량

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // HPManager가 연결되어 있는지 확인
        if (hpManager == null)
        {
            Debug.LogError("HPManager가 ItemManager에 연결되지 않았습니다. 인스펙터에서 연결해주세요.");
        }
        
        // 아이템 생성 코루틴 시작
        StartCoroutine(SpawnItemsRoutine());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    // 아이템을 랜덤한 위치에 생성하는 코루틴
    private IEnumerator SpawnItemsRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(itemSpawnInterval);
            
            SpawnRandomItem();
        }
    }
    
    // 랜덤한 위치에 HP 또는 Buff 아이템을 생성하는 메서드
    private void SpawnRandomItem()
    {
        // 랜덤 위치 계산 (현재 ItemManager의 위치를 기준으로 반경 내)
        Vector3 randomPos = GetRandomSpawnPosition();
        
        // HP 아이템 또는 Buff 아이템을 랜덤하게 선택하여 생성
        GameObject itemToSpawn = Random.Range(0, 2) == 0 ? hpItemPrefab : buffItemPrefab;
        
        if (itemToSpawn != null)
        {
            GameObject newItem = Instantiate(itemToSpawn, randomPos, Quaternion.identity);
            
            // 생성된 아이템에 충돌 처리를 위한 ItemScript 추가 및 초기화
            ItemScript itemScript = newItem.GetComponent<ItemScript>();
            if (itemScript == null)
            {
                itemScript = newItem.AddComponent<ItemScript>();
            }
            
            // 아이템 타입 설정
            itemScript.itemType = (itemToSpawn == hpItemPrefab) ? ItemScript.ItemType.HP : ItemScript.ItemType.Buff;
            itemScript.itemManager = this; // ItemManager 참조 전달
            
            // 생성 로그
            Debug.Log($"아이템 생성: {itemScript.itemType} at {randomPos.ToString()}");
        }
        else
        {
            Debug.LogWarning("아이템 프리팹(HP 또는 Buff) 중 하나가 연결되지 않았습니다.");
        }
    }
    
    // 랜덤한 생성 위치를 반환하는 메서드
    private Vector3 GetRandomSpawnPosition()
    {
        // XZ 평면에서 랜덤한 위치를 생성 (2D 게임이라면 XY 평면)
        // ItemManager가 부착된 오브젝트의 위치를 중심으로 합니다.
        Vector2 randomCircle = Random.insideUnitCircle * spawnAreaRadius;
        Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        
        // Y축은 지면(Ground)에 맞게 조정이 필요할 수 있으나, 여기서는 간단히 0 또는 현재 위치의 Y값을 사용합니다.
        // 3D 환경이라면, 플레이어 레벨에 맞춰 Y 값을 설정해야 합니다.
        spawnPosition.y = transform.position.y; 
        
        return spawnPosition;
    }

    // 아이템 충돌 시 호출될 메서드
    public void OnItemCollected(ItemScript.ItemType type, GameObject itemObject)
    {
        if (hpManager == null) return;
        
        // 아이템 제거
        Destroy(itemObject);
        
        // 아이템 효과 적용
        switch (type)
        {
            case ItemScript.ItemType.HP:
                hpManager.HealHP(hpRecoveryAmount); // HP 5 회복
                Debug.Log($"HP 아이템 획득! HP +{hpRecoveryAmount}");
                break;
            case ItemScript.ItemType.Buff:
                // Buff 게이지 증가 로직 (HPManager에 추가 필요)
                
                hpManager.currentBuff = Mathf.Min(hpManager.maxBuff, hpManager.currentBuff + buffGainAmount);
                Debug.Log($"버프 아이템 획득! Buff +{buffGainAmount}, 현재 Buff: {hpManager.currentBuff}");
                break;
        }
    }
}

// 아이템 프리팹에 추가될 컴포넌트 (충돌 감지 및 ItemManager 연결)
public class ItemScript : MonoBehaviour
{
    public enum ItemType { HP, Buff }
    public ItemType itemType;
    [HideInInspector] public ItemManager itemManager;
    
    
    
    // 2D 환경을 위한 충돌 감지 (2D 게임이라면 이 코드를 사용)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (itemManager != null)
            {
                itemManager.OnItemCollected(itemType, gameObject);
            }
        }
    }
}
