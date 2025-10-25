
using UnityEngine;
using UnityEngine.UI;

public class MovingDistance : MonoBehaviour
{
    [Header("UI 설정")]
    public Text distanceText; // 거리를 표시할 텍스트 컴포넌트
    public string textFormat = "{0} 미터"; // 텍스트 형식
    
    [Header("플레이어 설정")]
    public Transform playerTransform; // 플레이어 Transform 참조
    public bool autoFindPlayer = true; // 자동으로 플레이어 찾기
    
    [Header("거리 설정")]
    public float distanceMultiplier = 1f; // 거리 배율 (1 = 1유닛당 1미터)
    public int decimalPlaces = 0; // 소수점 자릿수
    
    private float startXPosition; // 시작 x 위치
    public float currentDistance; // 현재 이동 거리
    
    void Start()
    {
        // 플레이어 Transform 찾기
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
        
        // 텍스트 컴포넌트가 설정되지 않았다면 현재 오브젝트에서 찾기
        if (distanceText == null)
        {
            distanceText = GetComponent<Text>();
        }
        
        // 시작 위치 저장
        if (playerTransform != null)
        {
            startXPosition = playerTransform.position.x;
        }
        else
        {
            Debug.LogWarning("플레이어 Transform을 찾을 수 없습니다!");
        }
        
        // 초기 텍스트 설정
        UpdateDistanceText();
    }

    void Update()
    {
        if (playerTransform != null)
        {
            // 현재 x 위치에서 시작 x 위치를 뺀 절댓값이 이동 거리
            float currentXPosition = playerTransform.position.x;
            currentDistance = Mathf.Abs(currentXPosition - startXPosition) * distanceMultiplier;
            
            // UI 텍스트 업데이트
            UpdateDistanceText();
        }
    }
    
    private void UpdateDistanceText()
    {
        if (distanceText != null)
        {
            // 소수점 자릿수에 따라 포맷팅
            string formatString = "F" + decimalPlaces;
            string distanceString = currentDistance.ToString(formatString);
            
            // 텍스트 업데이트
            distanceText.text = string.Format(textFormat, distanceString);
        }
    }
    
    // 거리 리셋 (필요시 호출)
    public void ResetDistance()
    {
        if (playerTransform != null)
        {
            startXPosition = playerTransform.position.x;
            currentDistance = 0f;
            UpdateDistanceText();
        }
    }
    
    // 현재 거리 반환
    public float GetCurrentDistance()
    {
        return currentDistance;
    }
}
