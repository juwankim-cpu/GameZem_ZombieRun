using UnityEngine;

public class CharactorMove : MonoBehaviour
{
    Animator p_Animator;
    [Header("이동 설정")]
    public float moveSpeed = 0.15f; // 이동 속도

    [Header("Y축 제한 설정")]
    // Y축 최소 및 최대 제한 값 추가
    public float yMinLimit = -10f; 
    public float yMaxLimit = 0.5f;
    
    [Header("카메라 경계 설정")]
    public bool enableCameraBounds = true; // 카메라 경계 제한 활성화
    public float yOffset = 0.5f; // y축 오프셋 (플레이어 크기 고려)
    
    private Camera mainCamera;
    private float cameraTopBound;
    private float cameraBottomBound;
    
    void Init()
    {
        p_Animator = GetComponent<Animator>();
    }
    void Start()
    {
        Init();
        // 메인 카메라 참조
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
        
        // 카메라 경계 계산
        CalculateCameraBounds();
    }

    void Update()
    {
        
        // 이동 입력 처리
        Vector3 movement = Vector3.zero;
        
        if(Input.GetKey(KeyCode.UpArrow))
        {
            movement += new Vector3(0, 1, 0);
            
        }
            
        if(Input.GetKey(KeyCode.DownArrow))
        {
            movement += new Vector3(0, -1, 0);
          
        }
    
        if(Input.GetKey(KeyCode.LeftArrow))
        {
            movement += new Vector3(-1, 0, 0);
            
        }
        
        if(Input.GetKey(KeyCode.RightArrow))
        {
            movement += new Vector3(1, 0, 0);
            
        }

        if (p_Animator != null) // p_Animator가 null이 아닌지 확인 (Init()이 호출되지 않았다면 null일 수 있음)
        {
            p_Animator.SetBool("Walk", movement != Vector3.zero);
        }
        
        // 이동 적용 (카메라 경계 제한 포함)
        if (movement != Vector3.zero)
        {
            Vector3 newPosition = transform.position + movement * moveSpeed;

            // Y축 이동 제한 적용 (최소 -0.5, 최대 0.5)
            newPosition.y = Mathf.Clamp(newPosition.y, yMinLimit, yMaxLimit);
            
            // // Z축 카메라 경계 제한 적용
            // if (enableCameraBounds)
            // {
            //     newPosition.z = Mathf.Clamp(newPosition.z, cameraBottomBound, cameraTopBound);
            // }
            
            transform.position = newPosition;
        }

        // 앉기 기능
        if (Input.GetKeyDown(KeyCode.Z)) // 키를 누르는 순간
        {    GameStatus.sitDown = !GameStatus.sitDown;
             //앉기 애니메이션 재생
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            //애교 Status값 갱신
            //애교 애니메이션 재생
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            //공부 Status값 갱신
            //공부 애니메이션 재생
        }

    
        
       
    }
    
    private void CalculateCameraBounds()
    {
        if (mainCamera != null)
        {
            // 카메라의 화면 경계를 월드 좌표로 변환
            float cameraHeight = mainCamera.orthographicSize * 2f;
            float cameraWidth = cameraHeight * mainCamera.aspect;
            
            // y축 경계 계산 (플레이어 오프셋 고려)
            cameraTopBound = mainCamera.transform.position.y + (cameraHeight * 0.5f) - yOffset;
            cameraBottomBound = mainCamera.transform.position.y - (cameraHeight * 0.5f) + yOffset;
            
            Debug.Log($"카메라 y축 경계: {cameraBottomBound:F2} ~ {cameraTopBound:F2}");
        }
        else
        {
            Debug.LogWarning("카메라를 찾을 수 없습니다!");
        }
    }
    
    // 카메라가 이동했을 때 경계 재계산 (필요시 호출)
    public void UpdateCameraBounds()
    {
        CalculateCameraBounds();
    }
    

}
