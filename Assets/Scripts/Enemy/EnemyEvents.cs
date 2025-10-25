using UnityEngine;
using System.Collections;

public class EnemyEvents : MonoBehaviour
{
    [Header("이벤트 설정")]
    public bool useRandomInterval = true; // 랜덤 간격 사용 여부
    public float minInterval = 3f; // 최소 간격 (초)
    public float maxInterval = 7f; // 최대 간격 (초)
    public float fixedInterval = 4f; // 고정 간격 (랜덤 사용 안할 때)
    
    [Header("Sprite 애니메이션 설정")]
    public SpriteRenderer enemySprite; // 적 스프라이트 렌더러
    public float moveDistance = 10f; // 이동할 거리
    public float moveSpeed = 1.3f; // 이동 속도
    public float animationDuration = 4.5f; // 애니메이션 지속 시간
    
    [Header("방향 설정")]
    public bool useRandomDirection = true; // 랜덤 방향 사용
    public float[] spawnAngles = { 0f, 45f, -45f }; // 스폰 각도 (도)
    public Transform playerTransform; // 플레이어 Transform 참조 (방향 계산용)
    public bool autoFindPlayer = true; // 자동으로 플레이어 찾기
    
    [Header("이벤트 효과")]
    public bool enableScreenShake = true; // 화면 흔들림 효과
    public float shakeIntensity = 0.1f; // 흔들림 강도
    public float shakeDuration = 0.5f; // 흔들림 지속 시간
    
    [Header("회전 설정")]
    public bool enableRotation = true; // 회전 효과
    public float rotationSpeed = 90f; // 회전 속도 (도/초)
    
    [Header("디버그 정보")]
    [SerializeField] private float nextEventTime; // 다음 이벤트까지 남은 시간
    
    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private Vector3 originalSpritePosition;
    private bool isAnimating = false;
    private float currentAngle = 0f; // 현재 애니메이션 각도
    
    void Start()
    {
        // 메인 카메라 참조
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
        }
        
        // 스프라이트 렌더러가 설정되지 않았다면 현재 오브젝트에서 찾기
        if (enemySprite == null)
            enemySprite = GetComponent<SpriteRenderer>();
            
        // 플레이어 Transform 찾기 (방향 계산용)
        if (autoFindPlayer && playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                // 태그로 찾지 못했다면 CharactorMove 컴포넌트가 있는 오브젝트 찾기
                CharactorMove playerMove = FindObjectOfType<CharactorMove>();
                if (playerMove != null)
                {
                    playerTransform = playerMove.transform;
                }
            }
        }
        
        // 원래 스프라이트 위치 저장
        if (enemySprite != null)
        {
            originalSpritePosition = enemySprite.transform.position;
        }
        else
        {
            Debug.LogWarning("SpriteRenderer가 설정되지 않았습니다!");
        }
            
        // 이벤트 코루틴 시작
        StartCoroutine(EnemyAnimationEvent());
    }
    
    void Update()
    {
        // 다음 이벤트까지의 시간 업데이트 (디버그용)
        if (useRandomInterval)
        {
            nextEventTime = Random.Range(minInterval, maxInterval);
        }
        else
        {
            nextEventTime = fixedInterval;
        }
    }
    
    private IEnumerator EnemyAnimationEvent()
    {
        while (true)
        {
            // 랜덤 또는 고정 간격으로 대기
            float waitTime = useRandomInterval ? 
                Random.Range(minInterval, maxInterval) : 
                fixedInterval;
                
            yield return new WaitForSeconds(waitTime);
            
            // 적 애니메이션 이벤트 실행
            ExecuteEnemyAnimationEvent();
        }
    }
    
    private void ExecuteEnemyAnimationEvent()
    {
        if (enemySprite != null && !isAnimating)
        {
            // 랜덤 각도 선택
            currentAngle = useRandomDirection ? 
                spawnAngles[Random.Range(0, spawnAngles.Length)] : 
                spawnAngles[0];
            
            // 애니메이션 시작
            StartCoroutine(AnimateEnemySprite());
            
            // 화면 흔들림 효과
            if (enableScreenShake)
            {
                StartCoroutine(ScreenShake());
            }
            
            // 이벤트 로그
            string intervalType = useRandomInterval ? "랜덤" : "고정";
            Debug.Log($"적 애니메이션 이벤트 발생! 시간: {Time.time:F2}초 ({intervalType} 간격, {currentAngle}도 방향)");
        }
    }
    
    private IEnumerator AnimateEnemySprite()
    {
        isAnimating = true;
        
        // 스프라이트를 원래 위치로 리셋
        enemySprite.transform.position = originalSpritePosition;
        
        // 스프라이트 활성화
        enemySprite.gameObject.SetActive(true);
        
        // 각도에 따른 시작 위치와 끝 위치 계산
        Vector3 startPos = originalSpritePosition;
        Vector3 endPos = CalculateEndPosition(startPos, currentAngle);
        
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            // 각도 방향으로 이동
            float progress = elapsed / animationDuration;
            enemySprite.transform.position = Vector3.Lerp(startPos, endPos, progress);
            
            // 회전 효과
            if (enableRotation)
            {
                enemySprite.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 애니메이션 완료 후 스프라이트 비활성화
        enemySprite.gameObject.SetActive(false);
        
        isAnimating = false;
    }
    
    private Vector3 CalculateEndPosition(Vector3 startPos, float angle)
    {
        // 각도를 라디안으로 변환
        float angleInRadians = angle * Mathf.Deg2Rad;
        
        // 각도에 따른 방향 벡터 계산
        Vector3 direction = new Vector3(
            Mathf.Sin(angleInRadians),  // X축 (좌우)
            0,                          // Y축 (사용 안함)
            Mathf.Cos(angleInRadians)   // Z축 (앞뒤)
        );
        
        // 끝 위치 계산
        Vector3 endPos = startPos + direction * moveDistance;
        
        return endPos;
    }
    
    private IEnumerator ScreenShake()
    {
        if (mainCamera == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < shakeDuration)
        {
            // 랜덤한 방향으로 카메라 위치 변경
            Vector3 randomOffset = Random.insideUnitSphere * shakeIntensity;
            mainCamera.transform.position = originalCameraPosition + randomOffset;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // 원래 위치로 복원
        mainCamera.transform.position = originalCameraPosition;
    }
    
    // 에디터에서 애니메이션 경로 시각화
    private void OnDrawGizmosSelected()
    {
        if (enemySprite != null)
        {
            Gizmos.color = Color.red;
            Vector3 startPos = enemySprite.transform.position;
            
            // 각 스폰 각도에 대해 애니메이션 경로 표시
            foreach (float angle in spawnAngles)
            {
                Vector3 endPos = CalculateEndPosition(startPos, angle);
                
                // 시작점과 끝점 표시
                Gizmos.DrawWireSphere(startPos, 0.5f);
                Gizmos.DrawWireSphere(endPos, 0.5f);
                
                // 애니메이션 경로 표시
                Gizmos.DrawLine(startPos, endPos);
                
                // 각도 라벨 표시 (Scene 뷰에서)
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(endPos + Vector3.up * 1f, $"{angle}°");
                #endif
            }
        }
    }
}
