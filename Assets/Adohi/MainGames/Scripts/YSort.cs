using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class YSort : MonoBehaviour
{
    [SerializeField] private int baseOrder = 0;      // 이 그룹의 시작 Order
    [SerializeField] private int orderRange = 1000;  // 이 그룹에서 사용할 범위
    [Tooltip("정렬 기준으로 사용할 Transform (비어있으면 자기 자신)")]
    [SerializeField] private Transform targetTransform;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // targetTransform이 비어있으면 자기 자신 사용
        if (targetTransform == null)
        {
            targetTransform = transform;
        }
    }

    void LateUpdate()
    {
        // targetTransform의 Y좌표를 정규화해서 해당 그룹 범위 내로 매핑
        float y = targetTransform.position.y;
        int localOrder = Mathf.RoundToInt((-y * orderRange));
        spriteRenderer.sortingOrder = baseOrder + localOrder;
    }
}