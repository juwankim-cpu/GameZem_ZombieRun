using UnityEngine;

public class MapMove : MonoBehaviour
{
    [Header("플레이어 설정")]
    public Transform player; // 인스펙터 창에서 플레이어 오브젝트를 여기에 연결합니다.
    public bool autoFindPlayer = true; // 자동으로 플레이어 찾기
    
    [Header("따라가기 설정")]
    public float smoothSpeed = 0.125f; // 카메라가 플레이어를 따라가는 속도
    public bool followX = true; // X축 따라가기
    public bool followY = false; // Y축 따라가기
    public bool followZ = true; // Z축 따라가기
    
    [Header("무한 맵 설정")]
    public bool enableInfiniteMap = true; // 무한 맵 사용
    public InfiniteMapManager mapManager; // 무한 맵 매니저 참조
    
    [Header("디버그 설정")]
    public bool showDebugInfo = true; // 디버그 정보 표시
    public bool drawPlayerPosition = true; // 플레이어 위치 그리기
    
    [Header("오프셋 설정")]
    public Vector3 offset = Vector3.zero; // 플레이어로부터의 오프셋
    public bool useOffset = false; // 오프셋 사용 여부
    
    [Header("경계 설정")]
    public bool enableBounds = false; // 경계 제한 사용 여부
    public float minX = -40f; // 최소 X 위치 
    public float maxX = 40f; // 최대 X 위치
    
    
    private CharactorMove playerMoveScript; // 플레이어의 이동 스크립트 참조
    private Vector3 initialPosition; // 초기 위치

    void Start()
    {
        // 초기 위치 저장
        initialPosition = transform.position;
        
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
                // 태그로 찾지 못했다면 CharactorMove 컴포넌트가 있는 오브젝트 찾기
                CharactorMove playerMove = FindObjectOfType<CharactorMove>();
                if (playerMove != null)
                {
                    player = playerMove.transform;
                }
            }
        }
        
        // 플레이어의 CharactorMove 스크립트 참조 가져오기
        if (player != null)
        {
            playerMoveScript = player.GetComponent<CharactorMove>();
        }
        else
        {
            Debug.LogWarning("플레이어를 찾을 수 없습니다!");
        }
        
        // 무한 맵 매니저 자동 찾기
        if (enableInfiniteMap && mapManager == null)
        {
            mapManager = FindObjectOfType<InfiniteMapManager>();
        }
    }

    void LateUpdate()
    {
        if (player != null)
        {
            // 이전 카메라 위치 저장
            Vector3 previousPosition = transform.position;
            
            // 목표 위치 계산
            Vector3 desiredPosition = CalculateDesiredPosition();
            
            // 부드럽게 이동하도록 Lerp 함수를 사용합니다.
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            
            // 경계 제한 적용
            if (enableBounds)
            {
                smoothedPosition = ApplyBounds(smoothedPosition);
            }
            
            transform.position = smoothedPosition;
            
            // 디버그 정보 표시
            if (showDebugInfo)
            {
                Debug.Log($"카메라 위치: {transform.position}, 플레이어 위치: {player.position}, 목표 위치: {desiredPosition}");
            }
            
            // 카메라가 움직였고 플레이어 스크립트가 있다면 경계 업데이트
            if (Vector3.Distance(previousPosition, transform.position) > 0.01f && playerMoveScript != null)
            {
                playerMoveScript.UpdateCameraBounds();
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("플레이어가 null입니다!");
            }
        }
    }
    
    private Vector3 CalculateDesiredPosition()
    {
        Vector3 desiredPosition = transform.position;
        
        // X축 따라가기
        if (followX)
        {
            desiredPosition.x = player.position.x;
        }
        
        // Y축 따라가기
        if (followY)
        {
            desiredPosition.y = player.position.y;
        }
        
        
        
        // 오프셋 적용
        if (useOffset)
        {
            desiredPosition += offset;
        }
        
        return desiredPosition;
    }
    
    private Vector3 ApplyBounds(Vector3 position)
    {
        // X축 경계 제한
        if (followX)
        {
            position.x = Mathf.Clamp(position.x, minX, maxX);
        }
        
        
        
        return position;
    }
    
    // 외부에서 호출할 수 있는 메서드들
    public void SetFollowAxis(bool x, bool y)
    {
        followX = x;
        followY = y;
        
    }
    
    public void SetOffset(Vector3 newOffset)
    {
        offset = newOffset;
        useOffset = true;
    }
    
    public void ResetToInitialPosition()
    {
        transform.position = initialPosition;
    }
    
    public void SetBounds(float minX, float maxX)
    {
        this.minX = minX;
        this.maxX = maxX;
       
        enableBounds = true;
    }
    
    // 에디터에서 경계 시각화
    private void OnDrawGizmosSelected()
    {
        // 플레이어 위치 표시
        if (drawPlayerPosition && player != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(player.position, 1f);
            
            // 플레이어에서 카메라로의 선 그리기
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(player.position, transform.position);
        }
        
        // 카메라 위치 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // 경계 표시
        if (enableBounds)
        {
            Gizmos.color = Color.yellow;
            
            // 경계 박스 그리기
            Vector3 center = new Vector3(
                (minX + maxX) * 0.5f,
                transform.position.y,
                0
            );
            
            Vector3 size = new Vector3(
                maxX - minX,
                1f,
                0
            );
            
            Gizmos.DrawWireCube(center, size);
        }
    }
    
   
}
