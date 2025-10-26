using Unity.VisualScripting;
using UnityEngine;
using ZombieRun.Adohi.GameSystem;
using System.Collections;

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

    [Header("무적 시간 설정")]
    public float invincibilityDuration = 2f; // 무적 지속 시간 (초)
    public bool isInvincible = false; // 현재 무적 상태인지
    public float blinkInterval = 0.1f; // 깜빡임 간격 (초)
    public float blinkAlpha = 0.3f; // 깜빡일 때 투명도 (0~1)

    [Header("피격 상태 설정")]
    public float hittedDuration = 1f; // 피격 상태 지속 시간 (초)

    private Camera mainCamera;
    private float cameraTopBound;
    private float cameraBottomBound;

    private Vector3 initialScale;

    public float chiarSensorRange = 1f;
    HPManager hpManager;

    private SpriteRenderer spriteRenderer;
    void Init()
    {
        p_Animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
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

        if (!GameManager.Instance.IsPlaying)
        {
            return;
        }

        // 이동 입력 처리

        if (GameStatus.IsIdle())
        {
            Move();
            CheckChair();
        }


        if (Input.GetKey(KeyCode.Z)) // 키를 누르는 순간
        {
            if (GameStatus.IsIdle())
            {
                GameStatus.sitDown = true;
                //앉기 애니메이션 재생
                p_Animator.SetBool("Sit", true);
            }

        }
        else
        {
            GameStatus.sitDown = false;
            p_Animator.SetBool("Sit", false);
        }

        if (Input.GetKey(KeyCode.X))
        {
            if (GameStatus.IsIdle())
            {
                GameStatus.hearted = true;
                p_Animator.SetBool("Heart", true);
            }

        }
        else
        {
            GameStatus.hearted = false;
            p_Animator.SetBool("Heart", false);
        }

        if (GameStatus.study)
        {

        }
        if (Input.GetKeyUp(KeyCode.C))
        {
            GameStatus.study = false;
            p_Animator.SetBool("Study", false);
        }
    }
    // 앉기 기능


    private void Move()
    {
        Vector3 movement = Vector3.zero;

        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");

        movement = new Vector3(horizontal, vertical);

        if (p_Animator != null) // p_Animator가 null이 아닌지 확인 (Init()이 호출되지 않았다면 null일 수 있음)
        {
            p_Animator.SetBool("Walk", movement != Vector3.zero);
        }

        // 이동 적용 (카메라 경계 제한 포함)
        if (movement != Vector3.zero)
        {
            var moveDirection = movement.normalized;
            Vector3 newPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;

            if (moveDirection.x > 0)
            {
                transform.localScale = new Vector3(initialScale.x, initialScale.y, initialScale.z);
            }
            else if (moveDirection.x < 0)
            {
                transform.localScale = new Vector3(-initialScale.x, initialScale.y, initialScale.z);
            }

            // Y축 이동 제한 적용 (최소 -0.5, 최대 0.5)
            newPosition.y = Mathf.Clamp(newPosition.y, yMinLimit, yMaxLimit);

            // // Z축 카메라 경계 제한 적용
            // if (enableCameraBounds)
            // {
            //     newPosition.z = Mathf.Clamp(newPosition.z, cameraBottomBound, cameraTopBound);
            // }

            transform.position = newPosition;
        }

    }

    private void CheckChair()
    {

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, chiarSensorRange);
        foreach (Collider2D collider in colliders)
        {
            if (collider.gameObject.CompareTag("Chair"))
            {

                if (Input.GetKeyDown(KeyCode.C))
                {
                    //공부 Status값 갱신
                    GameStatus.study = !GameStatus.study;
                    //공부 애니메이션 재생
                    p_Animator.SetBool("Study", true);

                    Destroy(collider.gameObject);

                    return;
                }
            }
        }
    }



    public void GetHit()
    {
        if (isInvincible) return;

        if (!GameStatus.IsIdle())
        {
            GameStatus.sitDown = false;
            GameStatus.hearted = false;
            GameStatus.study = false;
            p_Animator.SetBool("Sit", false);
            p_Animator.SetBool("Heart", false);
            p_Animator.SetBool("Study", false);
        }
        p_Animator.SetBool("Hit", true);
        GameStatus.hitted = true;

        StartCoroutine(HittedCoroutine());
        StartCoroutine(InvincibilityCoroutine());
    }

    IEnumerator HittedCoroutine()
    {
        yield return new WaitForSeconds(hittedDuration);

        GameStatus.hitted = false;
        p_Animator.SetBool("Hit", false);
    }

    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;
        float elapsed = 0f;

        while (elapsed < invincibilityDuration)
        {
            // 깜빡임
            float alpha = (Mathf.Sin(elapsed / blinkInterval * Mathf.PI) + 1f) * 0.5f;
            alpha = Mathf.Lerp(blinkAlpha, 1f, alpha);

            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 무적 해제
        isInvincible = false;
        Color finalColor = spriteRenderer.color;
        finalColor.a = 1f;
        spriteRenderer.color = finalColor;
    }

    /*

        void OnCollisionStay2D(Collision2D other)
        {
            Debug.Log("CollisioinStay");
            Debug.Log(other.gameObject.name);
            Debug.Log(other.gameObject.tag);
            if (other.gameObject.CompareTag("Chair"))
            {
                Debug.Log(other.gameObject.name);
                if (Input.GetKeyDown(KeyCode.C))
                {
                    //공부 Status값 갱신
                    GameStatus.study = !GameStatus.study;
                    //공부 애니메이션 재생
                    p_Animator.SetBool("Study", true);

                    Destroy(other.gameObject);
                }
            }
        }
        */

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