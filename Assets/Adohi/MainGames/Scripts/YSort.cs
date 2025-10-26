using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class YSort : MonoBehaviour
{
    [SerializeField] private int baseOrder = 0;      // 이 그룹의 시작 Order
    [SerializeField] private int orderRange = 1000;  // 이 그룹에서 사용할 범위
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        // Y좌표를 정규화해서 해당 그룹 범위 내로 매핑
        float y = transform.position.y;
        int localOrder = Mathf.RoundToInt((-y * orderRange));
        spriteRenderer.sortingOrder = baseOrder + localOrder;
    }
}