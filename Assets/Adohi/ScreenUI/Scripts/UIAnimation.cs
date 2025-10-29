using System;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using com.cyborgAssets.inspectorButtonPro;
using ZombieRun.Adohi;

public class UIAnimation : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.OutCubic;
    [Tooltip("true: Time.timeScale 무시 (일시정지 중에도 애니메이션), false: Time.timeScale 영향 받음")]
    [SerializeField] private bool ignoreTimeScale = true;  // UI는 기본값 true (기존 동작 유지)

    [Header("스케일")]
    [SerializeField] private bool useScale = true;
    [SerializeField] private Vector3 scaleHidden = Vector3.zero;
    [SerializeField] private Vector3 scaleVisible = Vector3.one;

    [Header("포지션 (UI용 Anchored Position)")]
    [SerializeField] private bool usePosition = false;
    [SerializeField] private Vector2 positionHidden = Vector2.zero;
    [SerializeField] private Vector2 positionVisible = Vector2.zero;

    [Header("회전")]
    [SerializeField] private bool useRotation = false;
    [SerializeField] private Vector3 rotationHidden = Vector3.zero;
    [SerializeField] private Vector3 rotationVisible = Vector3.zero;

    [Header("페이드")]
    [SerializeField] private bool useFade = false;
    [SerializeField] private float fadeHidden = 0f;
    [SerializeField] private float fadeVisible = 1f;

    [Header("시작 설정")]
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool showOnStart = false;

    [Header("ShowAndHide 설정")]
    [Tooltip("ShowAndHide 메서드에서 Show 후 대기할 시간 (초)")]
    [SerializeField] private float defaultWaitTime = 1f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
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
        if (rectTransform == null)
        {
            InitializeComponents();
        }
    }

    private void InitializeComponents()
    {
        rectTransform = GetComponent<RectTransform>();

        if (useFade)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    void Start()
    {
        if (hideOnStart)
        {
            // 비주얼 상태를 숨김으로 설정
            SetVisualState(false);

            if (!showOnStart)
            {
                // showOnStart가 false면 비활성화
                gameObject.SetActive(false);
                isShowing = false;
            }
        }

        if (showOnStart)
        {
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

            // 숨긴 상태로 설정 (active는 그대로 유지)
            SetVisualState(false);

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

        if (rectTransform == null)
        {
            Debug.LogWarning("[UIAnimation] rectTransform이 null입니다. 애니메이션을 건너뜁니다.");
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
            var tween = rectTransform.DOScale(targetScale, duration).SetEase(easeType);
            if (ignoreTimeScale) tween = tween.SetUpdate(true);
            currentSequence = currentSequence.Join(tween);
        }

        // 포지션
        if (usePosition)
        {
            Vector2 targetPos = show ? positionVisible : positionHidden;
            Debug.Log($"targetPos: {targetPos}");
            var tween = rectTransform.DOAnchorPos(targetPos, duration).SetEase(easeType);
            if (ignoreTimeScale) tween = tween.SetUpdate(true);
            currentSequence = currentSequence.Join(tween);
        }

        // 회전
        if (useRotation)
        {
            Vector3 targetRot = show ? rotationVisible : rotationHidden;
            var tween = rectTransform.DORotate(targetRot, duration).SetEase(easeType);
            if (ignoreTimeScale) tween = tween.SetUpdate(true);
            currentSequence = currentSequence.Join(tween);
        }

        // 페이드
        if (useFade && canvasGroup != null)
        {
            float targetAlpha = show ? fadeVisible : fadeHidden;
            var tween = canvasGroup.DOFade(targetAlpha, duration).SetEase(easeType);
            if (ignoreTimeScale) tween = tween.SetUpdate(true);
            currentSequence = currentSequence.Join(tween);
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

        if (rectTransform == null)
        {
            Debug.LogWarning("[UIAnimation] rectTransform이 null입니다. 초기화를 건너뜁니다.");
            return;
        }

        if (useScale)
            rectTransform.localScale = visible ? scaleVisible : scaleHidden;

        if (usePosition)
            rectTransform.anchoredPosition = visible ? positionVisible : positionHidden;

        if (useRotation)
            rectTransform.localEulerAngles = visible ? rotationVisible : rotationHidden;

        if (useFade && canvasGroup != null)
            canvasGroup.alpha = visible ? fadeVisible : fadeHidden;
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
    /// 현재 상태를 Hidden으로 캡처
    /// </summary>
    public void CaptureCurrentAsHidden()
    {
        // 객체가 파괴되었는지 확인
        if (this == null || gameObject == null || rectTransform == null) return;

        if (useScale) scaleHidden = rectTransform.localScale;
        if (usePosition) positionHidden = rectTransform.anchoredPosition;
        if (useRotation) rotationHidden = rectTransform.localEulerAngles;
        if (useFade && canvasGroup != null) fadeHidden = canvasGroup.alpha;

        Debug.Log($"[캡처] Hidden 상태 저장 - Scale:{scaleHidden}, Pos:{positionHidden}");
    }

    /// <summary>
    /// 현재 상태를 Visible로 캡처
    /// </summary>
    public void CaptureCurrentAsVisible()
    {
        // 객체가 파괴되었는지 확인
        if (this == null || gameObject == null || rectTransform == null) return;

        if (useScale) scaleVisible = rectTransform.localScale;
        if (usePosition) positionVisible = rectTransform.anchoredPosition;
        if (useRotation) rotationVisible = rectTransform.localEulerAngles;
        if (useFade && canvasGroup != null) fadeVisible = canvasGroup.alpha;

        Debug.Log($"[캡처] Visible 상태 저장 - Scale:{scaleVisible}, Pos:{positionVisible}");
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
        Debug.Log($"=== UIAnimation 상태 ===");
        Debug.Log($"IsShowing: {isShowing}");
        Debug.Log($"Active: {gameObject.activeSelf}");
        Debug.Log($"Scale: {rectTransform.localScale}");
        Debug.Log($"Anchored Pos: {rectTransform.anchoredPosition}");
        Debug.Log($"Rotation: {rectTransform.localEulerAngles}");
        if (canvasGroup != null)
            Debug.Log($"Alpha: {canvasGroup.alpha}");
        Debug.Log($"--- 설정 ---");
        Debug.Log($"Hidden - Scale:{scaleHidden}, Pos:{positionHidden}");
        Debug.Log($"Visible - Scale:{scaleVisible}, Pos:{positionVisible}");
    }
}
