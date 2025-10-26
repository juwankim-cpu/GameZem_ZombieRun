using UnityEngine;
using System.Collections;
using ZombieRun.Adohi.GameSystem;

public class ItemManager : MonoBehaviour
{

    [Header("아이템 설정")]
    public GameObject hpItemPrefab; // HP 아이템 프리팹
    public GameObject buffItemPrefab; // 버프 아이템 프리팹
    public float itemSpawnInterval = 5f; // 아이템 생성 간격 (초)

    [Header("스폰 위치 설정")]
    public float spawnXPosition = 10f; // 아이템 생성 X 위치 (화면 오른쪽)
    public float minYPosition = -2.0f; // 최소 Y 위치
    public float maxYPosition = 2.0f; // 최대 Y 위치

    [Header("이동 설정")]
    public float minMoveSpeed = 3.0f; // 최소 이동 속도
    public float maxMoveSpeed = 7.0f; // 최대 이동 속도
    public float minXPosition = -20f; // 아이템 파괴 X 위치

    [Header("아이템 효과")]
    public float hpRecoveryAmount = 5f; // HP 아이템 회복량
    public float buffGainAmount = 5f; // 버프 아이템 획득량

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

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
            if (!GameManager.Instance.IsPlaying)
            {
                SpawnRandomItem();
            }
        }
    }

    // 랜덤한 위치에 HP 또는 Buff 아이템을 생성하는 메서드
    private void SpawnRandomItem()
    {
        // HP 아이템 또는 Buff 아이템을 랜덤하게 선택하여 생성
        GameObject itemToSpawn = Random.Range(0, 2) == 0 ? hpItemPrefab : buffItemPrefab;

        if (itemToSpawn != null)
        {
            // 랜덤 Y 위치 계산
            float randomY = Random.Range(minYPosition, maxYPosition);

            // 스폰 위치 설정 (오른쪽 화면)
            Vector3 spawnPosition = new Vector3(spawnXPosition, randomY, 0f);

            // 아이템 생성
            GameObject newItem = Instantiate(itemToSpawn, spawnPosition, Quaternion.identity);

            // 생성된 아이템에 충돌 처리를 위한 ItemScript 추가 및 초기화
            ItemScript itemScript = newItem.GetComponent<ItemScript>();
            if (itemScript == null)
            {
                itemScript = newItem.AddComponent<ItemScript>();
            }

            // 아이템 타입 설정
            itemScript.itemType = (itemToSpawn == hpItemPrefab) ? ItemScript.ItemType.HP : ItemScript.ItemType.Buff;
            itemScript.itemManager = this; // ItemManager 참조 전달

            // 이동 컴포넌트 추가
            ItemMover mover = newItem.GetComponent<ItemMover>();
            if (mover == null)
            {
                mover = newItem.AddComponent<ItemMover>();
            }

            // 랜덤 속도 설정
            mover.moveSpeed = Random.Range(minMoveSpeed, maxMoveSpeed);
            mover.minXPosition = minXPosition;

            // 생성 로그
            Debug.Log($"아이템 생성: {itemScript.itemType} at {spawnPosition.ToString()}");
        }
        else
        {
            Debug.LogWarning("아이템 프리팹(HP 또는 Buff) 중 하나가 연결되지 않았습니다.");
        }
    }


}

