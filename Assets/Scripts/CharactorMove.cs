using Unity.VisualScripting;
using UnityEngine;
using ZombieRun.Adohi.GameSystem;
using System.Collections;
using Ami.BroAudio;
using PinePie.SimpleJoystick;
using Cysharp.Threading.Tasks;
using ZombieRun.Adohi;
using Ami.BroAudio.Runtime;

public class CharactorMove : MonoBehaviour
{
    Animator p_Animator;
    Rigidbody2D rb;

    [Header("이동 설정")]
    public float moveSpeed = 0.15f; // 이동 속도

    [Header("이동 경계 제한 설정")]
    public float xMinLimit = -10f;
    public float xMaxLimit = 10f;
    public float yMinLimit = -10f;
    public float yMaxLimit = 0.5f;

    [Header("카메라 경계 설정")]
    public bool enableCameraBounds = true; // 카메라 경계 제한 활성화
    public float yOffset = 0.5f; // y축 오프셋 (플레이어 크기 고려)

    [Header("의자 설정")]
    public Transform sensorOrigin;
    public float chairSensorRange = 1f;
    public GameObject shadow;
    public float chairShadowXScale = 3.4f;
    private Vector3 originalShadowScale;


    [Header("무적 시간 설정")]
    public float invincibilityDuration = 2f; // 무적 지속 시간 (초)
    public bool isInvincible = false; // 현재 무적 상태인지
    public float blinkInterval = 0.1f; // 깜빡임 간격 (초)
    public float blinkAlpha = 0.3f; // 깜빡일 때 투명도 (0~1)

    [Header("피격 상태 설정")]
    public float hittedDuration = 1f; // 피격 상태 지속 시간 (초)

    [Header("피버 모드 설정")]
    public Fever feverUI;
    public float feverDuration = 10f; // 피버 모드 지속 시간 (초)
    public float feverSpeedMultiplier = 3f; // 피버 모드 속도 배율
    public bool isFeverMode = false; // 현재 피버 모드 상태인지
    private float originalMoveSpeed; // 원래 이동 속도 저장

    public GameObject feverModeChunk;
    public SoundID feverBgm;

    private Camera mainCamera;
    private float cameraTopBound;
    private float cameraBottomBound;

    private Vector3 initialScale;

    public SoundID hitSfx;


    private SpriteRenderer spriteRenderer;

    [Header("VFXs")]
    public UISimpleParticle hpVFX;
    public UISimpleParticle hpBarVFX;
    public UISimpleParticle boostVFX;
    public UISimpleParticle boostBarVFX;

    public JoystickController joystickController;

    [Header("Input Settings")]
    [Tooltip("키보드 입력 활성화 여부")]
    public bool enableKeyboardInput = true;



    void Init()
    {
        p_Animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialScale = transform.localScale;
        originalMoveSpeed = moveSpeed; // 원래 속도 저장


        GameStatus.sitDown = false;
        GameStatus.hearted = false;
        GameStatus.study = false;
        GameStatus.hitted = false;

        originalShadowScale = shadow.transform.localScale;

    }
    void Start()
    {
        Init();
        // 메인 카메라 참조
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindFirstObjectByType<Camera>();
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

        // 키보드 입력 처리 (활성화된 경우에만)
        if (enableKeyboardInput)
        {
            HandleKeyboardInput();
        }

        // 피버 모드 활성화 (V 키 또는 스페이스바로)
        if (GameManager.Instance.currentBoost.Value >= 100f && !isFeverMode)
        {
            ActivateFeverMode();
        }
    }

    void FixedUpdate()
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

        // 밀려나거나 외부에서 위치가 변경되어도 경계를 벗어나지 못하게 강제
        ClampPosition();
    }

    /// <summary>
    /// 키보드 입력 처리 (Update에서 호출)
    /// </summary>
    private void HandleKeyboardInput()
    {
        // Z키: 앉기
        if (Input.GetKey(KeyCode.Z))
        {
            StartSit();
        }
        else if (Input.GetKeyUp(KeyCode.Z))
        {
            StopSit();
        }

        // X키: 하트
        if (Input.GetKey(KeyCode.X))
        {
            StartHeart();
        }
        else if (Input.GetKeyUp(KeyCode.X))
        {
            StopHeart();
        }

        // C키: 공부 (눌렀을 때만)
        if (Input.GetKeyDown(KeyCode.C))
        {
            StartStudy();
        }
        else if (Input.GetKeyUp(KeyCode.C))
        {
            StopStudy();
        }
    }

    // 피버 모드 활성화
    private void ActivateFeverMode()
    {
        if (isFeverMode) return; // 이미 피버 모드 중이면 무시

        // 부스트 소모
        GameManager.Instance.currentBoost.Value = 0f;

        // 피버 모드 시작 (UniTask)
        FeverModeAsync().SafeAsync(this).Forget();
    }

    // 피버 모드 (UniTask로 변환)
    private async UniTask FeverModeAsync()
    {

        BroAudio.SetVolume(BroAudioType.Music, 0f, 1f);
        BroAudio.Play(feverBgm, 1f);
        if (feverUI != null)
        {
            await feverUI.ShowFever();
        }
        isFeverMode = true;
        isInvincible = true; // 무적 활성화
        p_Animator.SetBool("IsFever", true);

        // 이동 속도 증가
        moveSpeed = originalMoveSpeed * feverSpeedMultiplier;

        // 시각적 효과 (빛나는 효과)
        Color originalColor = spriteRenderer.color;
        float elapsed = 0f;

        Debug.Log($"피버 모드 활성화! {feverDuration}초 동안 무적 + 속도 {feverSpeedMultiplier}배!");
        feverModeChunk.SetActive(true);


        while (elapsed < feverDuration)
        {
            // 빛나는 효과 (빠르게 깜빡이며 밝게)
            float glow = (Mathf.Sin(elapsed * 10f) + 1f) * 0.5f;
            Color glowColor = Color.Lerp(originalColor, Color.yellow, glow * 0.3f);
            spriteRenderer.color = glowColor;

            elapsed += Time.deltaTime;
            await UniTask.Yield();  // yield return null 대신 UniTask.Yield()
        }

        // 피버 모드 종료
        isFeverMode = false;
        isInvincible = false; // 무적 해제
        moveSpeed = originalMoveSpeed; // 원래 속도로 복구
        spriteRenderer.color = originalColor; // 원래 색상으로 복구
        p_Animator.SetBool("IsFever", false);
        feverModeChunk.SetActive(false);
        BroAudio.SetVolume(BroAudioType.Music, 1f, 1f);
        BroAudio.Stop(feverBgm, 1f);
        Debug.Log("피버 모드 종료!");
    }

    private void Move()
    {
        Vector3 movement = Vector3.zero;

        // 키보드 입력
        var horizontal = Input.GetAxisRaw("Horizontal");
        var vertical = Input.GetAxisRaw("Vertical");
        movement = new Vector3(horizontal, vertical);

        // 조이스틱 입력 추가 (있으면)
        if (joystickController != null)
        {
            Vector2 joystickInput = joystickController.InputDirection;
            movement += new Vector3(joystickInput.x, joystickInput.y, 0f);
        }

        // 합친 입력이 1을 초과하지 않도록 클램프
        movement = Vector3.ClampMagnitude(movement, 1f);

        if (p_Animator != null) // p_Animator가 null이 아닌지 확인 (Init()이 호출되지 않았다면 null일 수 있음)
        {
            p_Animator.SetBool("Walk", movement != Vector3.zero);
        }

        // 이동 적용 (카메라 경계 제한 포함)
        if (movement != Vector3.zero)
        {
            var moveDirection = movement.normalized;
            Vector2 newPosition = (Vector2)transform.position + (Vector2)moveDirection * moveSpeed * Time.deltaTime;

            if (moveDirection.x > 0)
            {
                transform.localScale = new Vector3(initialScale.x, initialScale.y, initialScale.z);
            }
            else if (moveDirection.x < 0)
            {
                transform.localScale = new Vector3(-initialScale.x, initialScale.y, initialScale.z);
            }

            // X, Y축 이동 제한 적용
            newPosition.x = Mathf.Clamp(newPosition.x, xMinLimit, xMaxLimit);
            newPosition.y = Mathf.Clamp(newPosition.y, yMinLimit, yMaxLimit);

            // Rigidbody2D가 있으면 MovePosition 사용, 없으면 transform 직접 수정
            if (rb != null)
            {
                rb.MovePosition(newPosition);
            }
            else
            {
                transform.position = newPosition;
            }
        }

    }

    private void CheckChair()
    {
        // CheckChair는 이제 필요없지만 호환성을 위해 유지
        // 실제 C키 입력은 HandleKeyboardInput()에서 처리
    }



    public void GetHit(float damage)
    {
        if (isInvincible) return;

        GameManager.Instance.currentHealth.Value -= damage;
        GameManager.Instance.currentHealth.Value = Mathf.Max(GameManager.Instance.currentHealth.Value, 0f);

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
        BroAudio.Play(hitSfx);

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

    /// <summary>
    /// 캐릭터 위치를 경계 내로 강제 제한 (밀려남 방지)
    /// </summary>
    private void ClampPosition()
    {
        Vector2 pos = transform.position;
        Vector2 clampedPos = new Vector2(
            Mathf.Clamp(pos.x, xMinLimit, xMaxLimit),
            Mathf.Clamp(pos.y, yMinLimit, yMaxLimit)
        );

        // 위치가 변경되었을 때만 적용
        if (pos != clampedPos)
        {
            // Rigidbody2D가 있으면 rb.position 직접 설정 (더 강제적)
            if (rb != null)
            {
                rb.position = clampedPos;
            }
            else
            {
                transform.position = clampedPos;
            }
        }
    }

    // ===== 액션 함수들 (키보드 또는 버튼에서 호출 가능) =====
    // 주의: 이 함수들은 키보드와 버튼 모두에서 사용됩니다.
    // - 키보드: HandleKeyboardInput()에서 호출 (enableKeyboardInput = true일 때만)
    // - 버튼: UI 버튼의 PointerDown/PointerUp 이벤트에서 직접 호출

    /// <summary>
    /// 앉기 시작 (키보드 Z키 또는 버튼 누를 때 호출)
    /// </summary>
    public void StartSit()
    {
        if (isFeverMode) return; // 피버 모드 중에는 앉기 불가

        if (GameStatus.IsIdle())
        {
            GameStatus.sitDown = true;
            p_Animator.SetBool("Sit", true);
        }
    }

    /// <summary>
    /// 앉기 중지 (키보드 Z키 떼거나 버튼 뗄 때 호출)
    /// </summary>
    public void StopSit()
    {
        GameStatus.sitDown = false;
        p_Animator.SetBool("Sit", false);
    }

    /// <summary>
    /// 하트 시작 (키보드 X키 또는 버튼 누를 때 호출)
    /// </summary>
    public void StartHeart()
    {
        if (isFeverMode) return; // 피버 모드 중에는 하트 불가

        if (GameStatus.IsIdle())
        {
            GameStatus.hearted = true;
            p_Animator.SetBool("Heart", true);
        }
    }

    /// <summary>
    /// 하트 중지 (키보드 X키 떼거나 버튼 뗄 때 호출)
    /// </summary>
    public void StopHeart()
    {
        GameStatus.hearted = false;
        p_Animator.SetBool("Heart", false);
    }

    /// <summary>
    /// 공부 시작 (키보드 C키 또는 버튼 눌렀을 때 호출)
    /// 주변에 의자가 있으면 파괴하고 공부 시작
    /// </summary>
    public void StartStudy()
    {
        if (isFeverMode) return; // 피버 모드 중에는 공부 불가
        if (!GameStatus.IsIdle()) return;

        // 주변에 의자가 있는지 확인
        Collider2D[] colliders = Physics2D.OverlapCircleAll(sensorOrigin.position, chairSensorRange);
        foreach (Collider2D collider in colliders)
        {
            if (collider.gameObject.CompareTag("Chair"))
            {
                transform.position = collider.transform.position;
                shadow.transform.localScale = new Vector3(chairShadowXScale, originalShadowScale.y, originalShadowScale.z);
                transform.localScale = new Vector3(initialScale.x, initialScale.y, initialScale.z);
                GameStatus.study = true;
                p_Animator.SetBool("Study", true);
                // 의자 파괴
                Destroy(collider.gameObject);
                break;
            }
        }

        // 공부 시작

    }

    /// <summary>
    /// 공부 중지 (키보드 C키 떼거나 버튼 뗄 때 호출)
    /// </summary>
    public void StopStudy()
    {
        GameStatus.study = false;
        shadow.transform.localScale = originalShadowScale;
        p_Animator.SetBool("Study", false);
    }


}