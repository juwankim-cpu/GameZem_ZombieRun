using System;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using com.cyborgAssets.inspectorButtonPro;
using ZombieRun.Adohi;

/// <summary>
/// 월드 객체(2D/3D) 전용 애니메이션
/// UIAnimation과 달리 Transform과 Renderer를 사용
/// </summary>
public class WorldObjectAnimation : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.OutCubic;
    [Tooltip("true: Start→Visible→Hidden 3단계, false: Visible↔Hidden 2단계")]
    [SerializeField] private bool useThreeStates = false;
    [Tooltip("true: Time.timeScale 무시 (일시정지 중에도 애니메이션), false: Time.timeScale 영향 받음")]
    [SerializeField] private bool ignoreTimeScale = false;

    [Header("스케일")]
    [SerializeField] private bool useScale = true;
    [SerializeField] private Vector3 scaleStart = Vector3.one;     // 시작 스케일 (3단계 모드)
    [SerializeField] private Vector3 scaleVisible = Vector3.one;   // 보임 스케일
    [SerializeField] private Vector3 scaleHidden = Vector3.zero;   // 숨김 스케일

    [Header("포지션 (World/Local)")]
    [SerializeField] private bool usePosition = false;
    [SerializeField] private bool useLocalPosition = true;  // true: localPosition, false: world position
    [SerializeField] private Vector3 positionStart = Vector3.zero;    // 시작 위치 (3단계 모드)
    [SerializeField] private Vector3 positionVisible = Vector3.zero;  // 보임 위치
    [SerializeField] private Vector3 positionHidden = Vector3.zero;   // 숨김 위치

    [Header("회전")]
    [SerializeField] private bool useRotation = false;
    [SerializeField] private bool useLocalRotation = true;  // true: localRotation, false: world rotation
    [SerializeField] private Vector3 rotationStart = Vector3.zero;    // 시작 회전 (3단계 모드)
    [SerializeField] private Vector3 rotationVisible = Vector3.zero;  // 보임 회전
    [SerializeField] private Vector3 rotationHidden = Vector3.zero;   // 숨김 회전

    [Header("페이드")]
    [SerializeField] private bool useFade = false;
    [SerializeField] private FadeTargetType fadeTarget = FadeTargetType.SpriteRenderer;
    [SerializeField] private float fadeStart = 1f;     // 시작 알파 (3단계 모드)
    [SerializeField] private float fadeVisible = 1f;   // 보임 알파
    [SerializeField] private float fadeHidden = 0f;    // 숨김 알파

    public enum FadeTargetType
    {
        SpriteRenderer,
        MeshRenderer,
        Material
    }

    [Header("시작 설정")]
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool showOnStart = false;

    [Header("ShowAndHide 설정")]
    [Tooltip("ShowAndHide 메서드에서 Show 후 대기할 시간 (초)")]
    [SerializeField] private float defaultWaitTime = 1f;

    private Transform targetTransform;
    private SpriteRenderer spriteRenderer;
    private MeshRenderer meshRenderer;
    private Material targetMaterial;
    private Sequence currentSequence;
    private bool isShowing = false;

    // 이벤트
    public event Action OnShowCompleted;
    public event Action OnHideCompleted;

    void Awake()
    {
        InitializeComponents();
    }

    void OnEnable()
    {
        // 씬 재시작 시 컴포넌트가 null일 수 있으므로 재초기화
        if (targetTransform == null)
        {
            InitializeComponents();
        }
    }

    private void InitializeComponents()
    {
        targetTransform = transform;

        if (useFade)
        {
            switch (fadeTarget)
            {
                case FadeTargetType.SpriteRenderer:
                    spriteRenderer = GetComponent<SpriteRenderer>();
                    if (spriteRenderer == null)
                    {
                        Debug.LogWarning($"[WorldObjectAnimation] {gameObject.name}에 SpriteRenderer가 없습니다.");
                    }
                    break;

                case FadeTargetType.MeshRenderer:
                    meshRenderer = GetComponent<MeshRenderer>();
                    if (meshRenderer == null)
                    {
                        Debug.LogWarning($"[WorldObjectAnimation] {gameObject.name}에 MeshRenderer가 없습니다.");
                    }
                    break;

                case FadeTargetType.Material:
                    Renderer renderer = GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        targetMaterial = renderer.material;
                    }
                    else
                    {
                        Debug.LogWarning($"[WorldObjectAnimation] {gameObject.name}에 Renderer가 없습니다.");
                    }
                    break;
            }
        }
    }

    void Start()
    {
        if (hideOnStart)
        {
            // 3단계 모드: Start 상태로 초기화 (아직 안보임)
            if (useThreeStates)
            {
                SetToStartState();
                // Start 상태는 Show() 애니메이션의 시작점
                // 객체는 존재하지만 아직 화면에 나타나지 않은 상태
            }
            else
            {
                // 2단계 모드: Hidden 상태로 초기화
                SetVisualState(false);
            }

            if (!showOnStart)
            {
                // Hide 애니메이션은 발동 안 하고 객체만 숨김
                gameObject.SetActive(false);
                isShowing = false;
            }
        }

        if (showOnStart)
        {
            // 자동으로 Show 애니메이션 실행
            Show().SafeAsync(this).Forget();
        }
    }

    void OnDestroy()
    {
        currentSequence?.Kill();
    }

    /// <summary>
    /// 표시 애니메이션
    /// </summary>
    public async UniTask Show()
    {
        try
        {
            // 객체가 파괴되었는지 확인
            if (this == null || gameObject == null) return;

            // 이미 실행 중인 트윈이 있으면 취소 (중복 실행 방지)
            currentSequence?.Kill();

            gameObject.SetActive(true);

            // 3단계 모드: Start 상태로 먼저 설정
            if (useThreeStates)
            {
                SetToStartState();
            }
            else
            {
                // 2단계 모드: Hidden 상태로 초기화
                SetVisualState(false);
            }

            await PlayAnimation(true);

            isShowing = true;
            OnShowCompleted?.Invoke();
        }
        catch (OperationCanceledException)
        {
            // 취소된 경우 안전하게 처리 (OnDestroy에서 취소됨)
        }
    }

    /// <summary>
    /// 숨김 애니메이션
    /// </summary>
    public async UniTask Hide(bool deactivate = true)
    {
        try
        {
            // 객체가 파괴되었는지 확인
            if (this == null || gameObject == null) return;

            // 이미 실행 중인 트윈이 있으면 취소 (중복 실행 방지)
            currentSequence?.Kill();

            // 3단계 모드: Visible 상태에서 시작
            if (useThreeStates)
            {
                SetToVisibleState();
            }

            await PlayAnimation(false);

            if (deactivate)
            {
                gameObject.SetActive(false);
            }

            isShowing = false;
            OnHideCompleted?.Invoke();
        }
        catch (OperationCanceledException)
        {
            // 취소된 경우 안전하게 처리 (OnDestroy에서 취소됨)
        }
    }

    /// <summary>
    /// Start 상태로 즉시 설정 (모든 속성)
    /// </summary>
    private void SetToStartState()
    {
        if (targetTransform == null) return;

        if (useScale)
            targetTransform.localScale = scaleStart;

        if (usePosition)
        {
            if (useLocalPosition)
                targetTransform.localPosition = positionStart;
            else
                targetTransform.position = positionStart;
        }

        if (useRotation)
        {
            if (useLocalRotation)
                targetTransform.localEulerAngles = rotationStart;
            else
                targetTransform.eulerAngles = rotationStart;
        }

        if (useFade)
        {
            SetAlpha(fadeStart);
        }
    }

    /// <summary>
    /// Visible 상태로 즉시 설정 (모든 속성)
    /// </summary>
    private void SetToVisibleState()
    {
        if (targetTransform == null) return;

        if (useScale)
            targetTransform.localScale = scaleVisible;

        if (usePosition)
        {
            if (useLocalPosition)
                targetTransform.localPosition = positionVisible;
            else
                targetTransform.position = positionVisible;
        }

        if (useRotation)
        {
            if (useLocalRotation)
                targetTransform.localEulerAngles = rotationVisible;
            else
                targetTransform.eulerAngles = rotationVisible;
        }

        if (useFade)
        {
            SetAlpha(fadeVisible);
        }
    }

    /// <summary>
    /// 알파값 설정 헬퍼 메서드
    /// </summary>
    private void SetAlpha(float alpha)
    {
        switch (fadeTarget)
        {
            case FadeTargetType.SpriteRenderer:
                if (spriteRenderer != null)
                {
                    Color spriteColor = spriteRenderer.color;
                    spriteColor.a = alpha;
                    spriteRenderer.color = spriteColor;
                }
                break;

            case FadeTargetType.MeshRenderer:
                if (meshRenderer != null)
                {
                    Color meshColor = meshRenderer.material.color;
                    meshColor.a = alpha;
                    meshRenderer.material.color = meshColor;
                }
                break;

            case FadeTargetType.Material:
                if (targetMaterial != null)
                {
                    Color matColor = targetMaterial.color;
                    matColor.a = alpha;
                    targetMaterial.color = matColor;
                }
                break;
        }
    }

    /// <summary>
    /// 토글
    /// </summary>
    public async UniTask Toggle()
    {
        // 객체가 파괴되었는지 확인
        if (this == null || gameObject == null) return;

        if (isShowing)
            await Hide().SafeAsync(this);
        else
            await Show().SafeAsync(this);
    }

    /// <summary>
    /// Show → 대기 → Hide를 한 번에 실행 (기본 대기 시간 사용)
    /// </summary>
    /// <param name="deactivateOnHide">Hide 후 GameObject 비활성화 여부</param>
    public async UniTask ShowAndHide(bool deactivateOnHide = true)
    {
        await ShowAndHide(defaultWaitTime, deactivateOnHide);
    }

    /// <summary>
    /// Show → 대기 → Hide를 한 번에 실행 (커스텀 대기 시간)
    /// </summary>
    /// <param name="waitTime">Show 후 대기 시간 (초)</param>
    /// <param name="deactivateOnHide">Hide 후 GameObject 비활성화 여부</param>
    public async UniTask ShowAndHide(float waitTime, bool deactivateOnHide = true)
    {
        try
        {
            // 객체가 파괴되었는지 확인
            if (this == null || gameObject == null) return;

            // 이미 실행 중인 트윈이 있으면 취소
            currentSequence?.Kill();

            // Show 실행
            await Show();

            // 대기
            await UniTask.Delay((int)(waitTime * 1000), ignoreTimeScale: ignoreTimeScale, cancellationToken: this.GetCancellationTokenOnDestroy());

            // Hide 실행
            await Hide(deactivateOnHide);
        }
        catch (OperationCanceledException)
        {
            // 취소된 경우 안전하게 처리
        }
    }

    /// <summary>
    /// 애니메이션 재생
    /// </summary>
    private async UniTask PlayAnimation(bool show)
    {
        // 객체가 파괴되었는지 확인
        if (this == null || gameObject == null) return;

        if (targetTransform == null)
        {
            Debug.LogWarning("[WorldObjectAnimation] targetTransform이 null입니다. 애니메이션을 건너뜁니다.");
            return;
        }

        // currentSequence는 Show/Hide에서 이미 Kill되었음
        currentSequence = DOTween.Sequence();

        // 타임스케일 무시 설정
        if (ignoreTimeScale)
        {
            currentSequence = currentSequence.SetUpdate(true);
        }

        // 스케일
        if (useScale)
        {
            Vector3 targetScale = show ? scaleVisible : scaleHidden;
            var tween = targetTransform.DOScale(targetScale, duration).SetEase(easeType);
            if (ignoreTimeScale) tween = tween.SetUpdate(true);
            currentSequence = currentSequence.Join(tween);
        }

        // 포지션
        if (usePosition)
        {
            Vector3 targetPos = show ? positionVisible : positionHidden;
            Tween tween;
            if (useLocalPosition)
            {
                tween = targetTransform.DOLocalMove(targetPos, duration).SetEase(easeType);
            }
            else
            {
                tween = targetTransform.DOMove(targetPos, duration).SetEase(easeType);
            }
            if (ignoreTimeScale) tween = tween.SetUpdate(true);
            currentSequence = currentSequence.Join(tween);
        }

        // 회전
        if (useRotation)
        {
            Vector3 targetRot = show ? rotationVisible : rotationHidden;
            Tween tween;
            if (useLocalRotation)
            {
                tween = targetTransform.DOLocalRotate(targetRot, duration).SetEase(easeType);
            }
            else
            {
                tween = targetTransform.DORotate(targetRot, duration).SetEase(easeType);
            }
            if (ignoreTimeScale) tween = tween.SetUpdate(true);
            currentSequence = currentSequence.Join(tween);
        }

        // 페이드
        if (useFade)
        {
            float targetAlpha = show ? fadeVisible : fadeHidden;

            switch (fadeTarget)
            {
                case FadeTargetType.SpriteRenderer:
                    if (spriteRenderer != null)
                    {
                        var tween = spriteRenderer.DOFade(targetAlpha, duration).SetEase(easeType);
                        if (ignoreTimeScale) tween = tween.SetUpdate(true);
                        currentSequence = currentSequence.Join(tween);
                    }
                    break;

                case FadeTargetType.MeshRenderer:
                    if (meshRenderer != null)
                    {
                        var tween = meshRenderer.material.DOFade(targetAlpha, duration).SetEase(easeType);
                        if (ignoreTimeScale) tween = tween.SetUpdate(true);
                        currentSequence = currentSequence.Join(tween);
                    }
                    break;

                case FadeTargetType.Material:
                    if (targetMaterial != null)
                    {
                        var tween = targetMaterial.DOFade(targetAlpha, duration).SetEase(easeType);
                        if (ignoreTimeScale) tween = tween.SetUpdate(true);
                        currentSequence = currentSequence.Join(tween);
                    }
                    break;
            }
        }

        await currentSequence.SafeAsync(this);
    }

    /// <summary>
    /// 비주얼 상태만 변경 (Active는 변경 안 함)
    /// </summary>
    private void SetVisualState(bool visible)
    {
        // 객체가 파괴되었는지 확인
        if (this == null || gameObject == null) return;

        if (targetTransform == null)
        {
            Debug.LogWarning("[WorldObjectAnimation] targetTransform이 null입니다. 초기화를 건너뜁니다.");
            return;
        }

        if (useScale)
            targetTransform.localScale = visible ? scaleVisible : scaleHidden;

        if (usePosition)
        {
            if (useLocalPosition)
                targetTransform.localPosition = visible ? positionVisible : positionHidden;
            else
                targetTransform.position = visible ? positionVisible : positionHidden;
        }

        if (useRotation)
        {
            if (useLocalRotation)
                targetTransform.localEulerAngles = visible ? rotationVisible : rotationHidden;
            else
                targetTransform.eulerAngles = visible ? rotationVisible : rotationHidden;
        }

        if (useFade)
        {
            float alpha = visible ? fadeVisible : fadeHidden;

            switch (fadeTarget)
            {
                case FadeTargetType.SpriteRenderer:
                    if (spriteRenderer != null)
                    {
                        Color spriteColor = spriteRenderer.color;
                        spriteColor.a = alpha;
                        spriteRenderer.color = spriteColor;
                    }
                    break;

                case FadeTargetType.MeshRenderer:
                    if (meshRenderer != null)
                    {
                        Color meshColor = meshRenderer.material.color;
                        meshColor.a = alpha;
                        meshRenderer.material.color = meshColor;
                    }
                    break;

                case FadeTargetType.Material:
                    if (targetMaterial != null)
                    {
                        Color matColor = targetMaterial.color;
                        matColor.a = alpha;
                        targetMaterial.color = matColor;
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// 즉시 상태 변경 (애니메이션 없이)
    /// </summary>
    public void SetStateImmediate(bool visible)
    {
        // 객체가 파괴되었는지 확인
        if (this == null || gameObject == null) return;

        SetVisualState(visible);
        gameObject.SetActive(visible);
        isShowing = visible;
    }

    /// <summary>
    /// 현재 상태를 Start로 캡처 (3단계 모드)
    /// </summary>
    public void CaptureCurrentAsStart()
    {
        // 객체가 파괴되었는지 확인
        if (this == null || gameObject == null || targetTransform == null) return;

        if (useScale) scaleStart = targetTransform.localScale;

        if (usePosition)
        {
            if (useLocalPosition)
                positionStart = targetTransform.localPosition;
            else
                positionStart = targetTransform.position;
        }

        if (useRotation)
        {
            if (useLocalRotation)
                rotationStart = targetTransform.localEulerAngles;
            else
                rotationStart = targetTransform.eulerAngles;
        }

        if (useFade)
        {
            switch (fadeTarget)
            {
                case FadeTargetType.SpriteRenderer:
                    if (spriteRenderer != null) fadeStart = spriteRenderer.color.a;
                    break;
                case FadeTargetType.MeshRenderer:
                    if (meshRenderer != null) fadeStart = meshRenderer.material.color.a;
                    break;
                case FadeTargetType.Material:
                    if (targetMaterial != null) fadeStart = targetMaterial.color.a;
                    break;
            }
        }

        Debug.Log($"[캡처] Start 상태 저장 - Scale:{scaleStart}, Pos:{positionStart}, Rot:{rotationStart}, Fade:{fadeStart}");
    }

    /// <summary>
    /// 현재 상태를 Hidden으로 캡처
    /// </summary>
    public void CaptureCurrentAsHidden()
    {
        // 객체가 파괴되었는지 확인
        if (this == null || gameObject == null || targetTransform == null) return;

        if (useScale) scaleHidden = targetTransform.localScale;

        if (usePosition)
        {
            if (useLocalPosition)
                positionHidden = targetTransform.localPosition;
            else
                positionHidden = targetTransform.position;
        }

        if (useRotation)
        {
            if (useLocalRotation)
                rotationHidden = targetTransform.localEulerAngles;
            else
                rotationHidden = targetTransform.eulerAngles;
        }

        if (useFade)
        {
            switch (fadeTarget)
            {
                case FadeTargetType.SpriteRenderer:
                    if (spriteRenderer != null) fadeHidden = spriteRenderer.color.a;
                    break;
                case FadeTargetType.MeshRenderer:
                    if (meshRenderer != null) fadeHidden = meshRenderer.material.color.a;
                    break;
                case FadeTargetType.Material:
                    if (targetMaterial != null) fadeHidden = targetMaterial.color.a;
                    break;
            }
        }

        Debug.Log($"[캡처] Hidden 상태 저장 - Scale:{scaleHidden}, Pos:{positionHidden}, Rot:{rotationHidden}, Fade:{fadeHidden}");
    }

    /// <summary>
    /// 현재 상태를 Visible로 캡처
    /// </summary>
    public void CaptureCurrentAsVisible()
    {
        // 객체가 파괴되었는지 확인
        if (this == null || gameObject == null || targetTransform == null) return;

        if (useScale) scaleVisible = targetTransform.localScale;

        if (usePosition)
        {
            if (useLocalPosition)
                positionVisible = targetTransform.localPosition;
            else
                positionVisible = targetTransform.position;
        }

        if (useRotation)
        {
            if (useLocalRotation)
                rotationVisible = targetTransform.localEulerAngles;
            else
                rotationVisible = targetTransform.eulerAngles;
        }

        if (useFade)
        {
            switch (fadeTarget)
            {
                case FadeTargetType.SpriteRenderer:
                    if (spriteRenderer != null) fadeVisible = spriteRenderer.color.a;
                    break;
                case FadeTargetType.MeshRenderer:
                    if (meshRenderer != null) fadeVisible = meshRenderer.material.color.a;
                    break;
                case FadeTargetType.Material:
                    if (targetMaterial != null) fadeVisible = targetMaterial.color.a;
                    break;
            }
        }

        Debug.Log($"[캡처] Visible 상태 저장 - Scale:{scaleVisible}, Pos:{positionVisible}, Rot:{rotationVisible}, Fade:{fadeVisible}");
    }

    // 프로퍼티
    public bool IsShowing => isShowing;
    public bool IgnoreTimeScale
    {
        get => ignoreTimeScale;
        set => ignoreTimeScale = value;
    }

    // ========== 테스트 버튼 ==========

    [ProButton]
    public void TestShow()
    {
        Show().SafeAsync(this).Forget();
    }

    [ProButton]
    public void TestHide()
    {
        Hide().SafeAsync(this).Forget();
    }

    [ProButton]
    public void TestToggle()
    {
        Toggle().SafeAsync(this).Forget();
    }

    [ProButton]
    public void TestShowAndHide()
    {
        ShowAndHide().SafeAsync(this).Forget();
        Debug.Log($"Show → {defaultWaitTime}초 대기 → Hide 실행");
    }

    [ProButton]
    public void TestShowAndHide3Sec()
    {
        ShowAndHide(3f).SafeAsync(this).Forget();
        Debug.Log("Show → 3초 대기 → Hide 실행 (커스텀)");
    }

    [ProButton]
    public void TestShowImmediate()
    {
        SetStateImmediate(true);
        Debug.Log("즉시 표시");
    }

    [ProButton]
    public void TestHideImmediate()
    {
        SetStateImmediate(false);
        Debug.Log("즉시 숨김");
    }

    [ProButton]
    public void TestMoveToStart()
    {
        if (useThreeStates)
        {
            SetToStartState();
            Debug.Log("Start 상태로 이동");
        }
        else
        {
            Debug.LogWarning("3단계 모드가 아닙니다. Use Three States를 활성화하세요.");
        }
    }

    [ProButton]
    public void TestMoveToVisible()
    {
        if (useThreeStates)
        {
            SetToVisibleState();
            Debug.Log("Visible 상태로 이동");
        }
        else
        {
            Debug.LogWarning("3단계 모드가 아닙니다. Use Three States를 활성화하세요.");
        }
    }

    [ProButton]
    public void TestCaptureAsStart()
    {
        CaptureCurrentAsStart();
    }

    [ProButton]
    public void TestCaptureAsHidden()
    {
        CaptureCurrentAsHidden();
    }

    [ProButton]
    public void TestCaptureAsVisible()
    {
        CaptureCurrentAsVisible();
    }

    [ProButton]
    public void TestPrintState()
    {
        Debug.Log($"=== WorldObjectAnimation 상태 ===");
        Debug.Log($"IsShowing: {isShowing}");
        Debug.Log($"Active: {gameObject.activeSelf}");
        Debug.Log($"3단계 모드: {useThreeStates}");
        Debug.Log($"Scale: {targetTransform.localScale}");
        Debug.Log($"Position (Local): {targetTransform.localPosition}");
        Debug.Log($"Position (World): {targetTransform.position}");
        Debug.Log($"Rotation (Local): {targetTransform.localEulerAngles}");
        Debug.Log($"Rotation (World): {targetTransform.eulerAngles}");

        if (useFade)
        {
            switch (fadeTarget)
            {
                case FadeTargetType.SpriteRenderer:
                    if (spriteRenderer != null) Debug.Log($"Alpha (SpriteRenderer): {spriteRenderer.color.a}");
                    break;
                case FadeTargetType.MeshRenderer:
                    if (meshRenderer != null) Debug.Log($"Alpha (MeshRenderer): {meshRenderer.material.color.a}");
                    break;
                case FadeTargetType.Material:
                    if (targetMaterial != null) Debug.Log($"Alpha (Material): {targetMaterial.color.a}");
                    break;
            }
        }

        Debug.Log($"--- 설정 ---");
        if (useThreeStates)
        {
            Debug.Log($"Start   - Scale:{scaleStart}, Pos:{positionStart}, Rot:{rotationStart}, Fade:{fadeStart}");
            Debug.Log($"Visible - Scale:{scaleVisible}, Pos:{positionVisible}, Rot:{rotationVisible}, Fade:{fadeVisible}");
            Debug.Log($"Hidden  - Scale:{scaleHidden}, Pos:{positionHidden}, Rot:{rotationHidden}, Fade:{fadeHidden}");
        }
        else
        {
            Debug.Log($"Visible - Scale:{scaleVisible}, Pos:{positionVisible}, Rot:{rotationVisible}, Fade:{fadeVisible}");
            Debug.Log($"Hidden  - Scale:{scaleHidden}, Pos:{positionHidden}, Rot:{rotationHidden}, Fade:{fadeHidden}");
        }
    }
}

