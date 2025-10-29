using UnityEngine;

public class ObstacleMover : MonoBehaviour
{
    public float moveSpeed;

    public float minXPosition = -20f;


    [Header("그림자 설정")]
    [Tooltip("EnemySightSystem에서 사용할 그림자 길이")]
    public float shadowLength = 1f;

    void Update()
    {

        // 1. X축(왼쪽)으로 이동
        // moveSpeed는 ObstacleManager로부터 랜덤 값을 전달받아 사용합니다.
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime, Space.World);

        // 2. 파괴 조건 검사 (X 위치가 -10보다 작아지면 파괴)
        if (transform.position.x < minXPosition)
        {
            Destroy(gameObject);
        }
    }
}
