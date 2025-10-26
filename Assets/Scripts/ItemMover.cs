using UnityEngine;

public class ItemMover : MonoBehaviour
{
    public float moveSpeed;
    public float minXPosition = -20f;

    void Update()
    {
        if (GameStatus.sitDown)
            return;

        // X축(왼쪽)으로 이동
        transform.Translate(Vector3.left * moveSpeed * Time.deltaTime, Space.World);

        // 파괴 조건 검사 (X 위치가 minXPosition보다 작아지면 파괴)
        if (transform.position.x < minXPosition)
        {
            Destroy(gameObject);
        }
    }
}

