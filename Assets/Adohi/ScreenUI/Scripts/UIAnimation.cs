using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System;
using com.cyborgAssets.inspectorButtonPro;

public class UIAnimation : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [SerializeField] private AnimationType animationType = AnimationType.All;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.OutCubic;

    [Header("스케일 애니메이션")]
    [SerializeField] private bool useScale = true;
    [SerializeField] private Vector3 scaleStart = Vector3.zero;
    [SerializeField] private Vector3 scaleEnd = Vector3.one;

    [Header("포지션 애니메이션")]
    [SerializeField] private bool usePosition = false;
    [SerializeField] private bool useLocalPosition = true;
    [SerializeField] private Vector3 positionStart = Vector3.zero;
    [SerializeField] private Vector3 positionEnd = Vector3.zero;

    [Header("회전 애니메이션")]
    [SerializeField] private bool useRotation = false;
    [SerializeField] private Vector3 rotationStart = Vector3.zero;
    [SerializeField] private Vector3 rotationEnd = Vector3.zero;

    [Header("페이드 애니메이션")]
    [SerializeField] private bool useFade = false;
    [SerializeField] private float fadeStart = 0f;
    [SerializeField] private float fadeEnd = 1f;

    [Header("시작 설정")]
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private float startDelay = 0f;

    [Header("애니메이션 모드")]
    [SerializeField] private bool sequential = false; // true: 순차 실행, false: 동시 실행
    [SerializeField] private float sequentialDelay = 0.1f;

    [Header("루프 설정")]
    [SerializeField] private bool loop = false;
    [SerializeField] private LoopType loopType = LoopType.Restart;
    [SerializeField] private int loopCount = -1; // -1 = 무한

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Vector3 originalRotation;
    private bool isShowing = false;
    private Sequence currentSequence;

    // 이벤트 콜백
    public event Action OnShowStarted;
    public event Action OnShowCompleted;
    public event Action OnHideStarted;
    public event Action OnHideCompleted;

    public enum AnimationType
    {
        All,
        Scale,
        Position,
        Rotation,
        Fade,
        Custom
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        // CanvasGroup이 없으면 추가
        if (useFade)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // 원본 값 저장
        originalScale = rectTransform.localScale;
        originalPosition = useLocalPosition ? rectTransform.localPosition : rectTransform.position;
        originalRotation = rectTransform.localEulerAngles;
    }

    private async void Start()
    {
        // 시작 시 숨김 처리
        if (hideOnStart)
        {
            SetHideStateImmediate();
        }

        // 시작 지연 후 자동 표시
        if (showOnStart)
        {
            if (startDelay > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            await Show();
        }
    }

    private void OnDestroy()
    {
        // 진행 중인 애니메이션 정리
        currentSequence?.Kill();
    }

    /// <summary>
    /// UI를 표시합니다
    /// </summary>
    public async UniTask Show(Action onComplete = null)
    {
        if (isShowing) return;

        // 먼저 활성화하고 초기 상태로 설정
        gameObject.SetActive(true);

        // 시작 상태를 즉시 설정 (애니메이션이 제대로 동작하도록)
        SetStartStateForShow();

        isShowing = true;

        OnShowStarted?.Invoke();

        // 기존 애니메이션 중단
        currentSequence?.Kill();

        currentSequence = DOTween.Sequence();

        if (sequential)
        {
            // 순차 실행
            PlayAnimationsSequential(true);
        }
        else
        {
            // 동시 실행
            PlayAnimationsParallel(true);
        }

        if (loop)
        {
            currentSequence = currentSequence.SetLoops(loopCount, loopType);
        }

        await currentSequence.AsyncWaitForCompletion().AsUniTask();

        OnShowCompleted?.Invoke();
        onComplete?.Invoke();
    }

    /// <summary>
    /// UI를 숨깁니다
    /// </summary>
    public async UniTask Hide(Action onComplete = null, bool deactivateOnComplete = true)
    {
        if (!isShowing) return;

        isShowing = false;

        OnHideStarted?.Invoke();

        // 기존 애니메이션 중단
        currentSequence?.Kill();

        currentSequence = DOTween.Sequence();

        if (sequential)
        {
            // 순차 실행
            PlayAnimationsSequential(false);
        }
        else
        {
            // 동시 실행
            PlayAnimationsParallel(false);
        }

        await currentSequence.AsyncWaitForCompletion().AsUniTask();

        if (deactivateOnComplete)
        {
            gameObject.SetActive(false);
        }

        OnHideCompleted?.Invoke();
        onComplete?.Invoke();
    }

    /// <summary>
    /// 애니메이션을 동시에 재생
    /// </summary>
    private void PlayAnimationsParallel(bool show)
    {
        if (useScale || (animationType == AnimationType.All || animationType == AnimationType.Scale))
        {
            Vector3 targetScale = show ? scaleEnd : scaleStart;
            currentSequence.Join(rectTransform.DOScale(targetScale, duration).SetEase(easeType));
        }

        if (usePosition || (animationType == AnimationType.All || animationType == AnimationType.Position))
        {
            Vector3 targetPosition = show ? positionEnd : positionStart;
            if (useLocalPosition)
            {
                currentSequence.Join(rectTransform.DOLocalMove(targetPosition, duration).SetEase(easeType));
            }
            else
            {
                currentSequence.Join(rectTransform.DOMove(targetPosition, duration).SetEase(easeType));
            }
        }

        if (useRotation || (animationType == AnimationType.All || animationType == AnimationType.Rotation))
        {
            Vector3 targetRotation = show ? rotationEnd : rotationStart;
            currentSequence.Join(rectTransform.DOLocalRotate(targetRotation, duration, RotateMode.FastBeyond360).SetEase(easeType));
        }

        if (useFade || (animationType == AnimationType.All || animationType == AnimationType.Fade))
        {
            if (canvasGroup != null)
            {
                float targetAlpha = show ? fadeEnd : fadeStart;
                currentSequence.Join(canvasGroup.DOFade(targetAlpha, duration).SetEase(easeType));
            }
        }
    }

    /// <summary>
    /// 애니메이션을 순차적으로 재생
    /// </summary>
    private void PlayAnimationsSequential(bool show)
    {
        if (useScale || (animationType == AnimationType.All || animationType == AnimationType.Scale))
        {
            Vector3 targetScale = show ? scaleEnd : scaleStart;
            currentSequence.Append(rectTransform.DOScale(targetScale, duration).SetEase(easeType));
            if (sequentialDelay > 0)
                currentSequence.AppendInterval(sequentialDelay);
        }

        if (usePosition || (animationType == AnimationType.All || animationType == AnimationType.Position))
        {
            Vector3 targetPosition = show ? positionEnd : positionStart;
            if (useLocalPosition)
            {
                currentSequence.Append(rectTransform.DOLocalMove(targetPosition, duration).SetEase(easeType));
            }
            else
            {
                currentSequence.Append(rectTransform.DOMove(targetPosition, duration).SetEase(easeType));
            }
            if (sequentialDelay > 0)
                currentSequence.AppendInterval(sequentialDelay);
        }

        if (useRotation || (animationType == AnimationType.All || animationType == AnimationType.Rotation))
        {
            Vector3 targetRotation = show ? rotationEnd : rotationStart;
            currentSequence.Append(rectTransform.DOLocalRotate(targetRotation, duration, RotateMode.FastBeyond360).SetEase(easeType));
            if (sequentialDelay > 0)
                currentSequence.AppendInterval(sequentialDelay);
        }

        if (useFade || (animationType == AnimationType.All || animationType == AnimationType.Fade))
        {
            if (canvasGroup != null)
            {
                float targetAlpha = show ? fadeEnd : fadeStart;
                currentSequence.Append(canvasGroup.DOFade(targetAlpha, duration).SetEase(easeType));
            }
        }
    }

    /// <summary>
    /// Show 애니메이션 시작 전 초기 상태 설정
    /// </summary>
    private void SetStartStateForShow()
    {
        if (useScale) rectTransform.localScale = scaleStart;
        if (usePosition)
        {
            if (useLocalPosition)
                rectTransform.localPosition = positionStart;
            else
                rectTransform.position = positionStart;
        }
        if (useRotation) rectTransform.localEulerAngles = rotationStart;
        if (useFade && canvasGroup != null) canvasGroup.alpha = fadeStart;
    }

    /// <summary>
    /// 즉시 숨김 상태로 설정
    /// </summary>
    private void SetHideStateImmediate()
    {
        SetStartStateForShow();

        if (hideOnStart && !showOnStart)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 즉시 표시 상태로 설정
    /// </summary>
    public void SetShowStateImmediate()
    {
        if (useScale) rectTransform.localScale = scaleEnd;
        if (usePosition)
        {
            if (useLocalPosition)
                rectTransform.localPosition = positionEnd;
            else
                rectTransform.position = positionEnd;
        }
        if (useRotation) rectTransform.localEulerAngles = rotationEnd;
        if (useFade && canvasGroup != null) canvasGroup.alpha = fadeEnd;

        gameObject.SetActive(true);
        isShowing = true;
    }

    /// <summary>
    /// 토글 (Show/Hide 전환)
    /// </summary>
    public async UniTask Toggle()
    {
        if (isShowing)
        {
            await Hide();
        }
        else
        {
            await Show();
        }
    }

    /// <summary>
    /// 현재 애니메이션 중단
    /// </summary>
    public void Stop()
    {
        currentSequence?.Kill();
    }

    /// <summary>
    /// 원본 값으로 리셋
    /// </summary>
    public void ResetToOriginal()
    {
        rectTransform.localScale = originalScale;
        if (useLocalPosition)
            rectTransform.localPosition = originalPosition;
        else
            rectTransform.position = originalPosition;
        rectTransform.localEulerAngles = originalRotation;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    // 프로퍼티
    public bool IsShowing => isShowing;
    public float Duration => duration;

    // ========== 테스트 버튼들 (Inspector에서 사용) ==========

    [ProButton]
    public async void TestShow()
    {
        await Show();
    }

    [ProButton]
    public async void TestHide()
    {
        await Hide();
    }

    [ProButton]
    public async void TestToggle()
    {
        await Toggle();
    }

    [ProButton]
    public void TestShowImmediate()
    {
        SetShowStateImmediate();
    }

    [ProButton]
    public void TestHideImmediate()
    {
        SetHideStateImmediate();
    }

    [ProButton]
    public void TestResetToOriginal()
    {
        ResetToOriginal();
    }

    [ProButton]
    public void TestStop()
    {
        Stop();
        Debug.Log("애니메이션 중단됨");
    }

    [ProButton]
    public void TestDeactivate()
    {
        gameObject.SetActive(false);
        isShowing = false;
        Debug.Log("오브젝트 비활성화됨 (이제 TestShow로 다시 활성화 테스트 가능)");
    }

    [ProButton]
    public async void TestShowFromInactive()
    {
        gameObject.SetActive(false);
        isShowing = false;
        await UniTask.Delay(100); // 잠시 대기
        Debug.Log("비활성화 상태에서 Show 호출 테스트 시작");
        await Show();
    }
}