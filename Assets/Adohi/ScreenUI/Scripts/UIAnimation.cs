using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System;
using com.cyborgAssets.inspectorButtonPro;

public class UIAnimation : MonoBehaviour
{
    [Header("애니메이션 설정")]
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.OutCubic;

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

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Sequence currentSequence;
    private bool isShowing = false;

    // 이벤트
    public event Action OnShowCompleted;
    public event Action OnHideCompleted;

    void Awake()
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
            ShowAsync().Forget();
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
        gameObject.SetActive(true);

        // 숨긴 상태로 설정 (active는 그대로 유지)
        SetVisualState(false);

        await PlayAnimation(true);

        isShowing = true;
        OnShowCompleted?.Invoke();
    }

    /// <summary>
    /// 숨김 애니메이션
    /// </summary>
    public async UniTask Hide(bool deactivate = true)
    {
        await PlayAnimation(false);

        if (deactivate)
        {
            gameObject.SetActive(false);
        }

        isShowing = false;
        OnHideCompleted?.Invoke();
    }

    /// <summary>
    /// 토글
    /// </summary>
    public async UniTask Toggle()
    {
        if (isShowing)
            await Hide();
        else
            await Show();
    }

    /// <summary>
    /// 애니메이션 재생
    /// </summary>
    private async UniTask PlayAnimation(bool show)
    {
        currentSequence?.Kill();
        currentSequence = DOTween.Sequence();

        // 스케일
        if (useScale)
        {
            Vector3 targetScale = show ? scaleVisible : scaleHidden;
            currentSequence = currentSequence.Join(rectTransform.DOScale(targetScale, duration).SetEase(easeType));
        }

        // 포지션
        if (usePosition)
        {
            Vector2 targetPos = show ? positionVisible : positionHidden;
            Debug.Log($"targetPos: {targetPos}");
            currentSequence = currentSequence.Join(rectTransform.DOAnchorPos(targetPos, duration).SetEase(easeType));
        }

        // 회전
        if (useRotation)
        {
            Vector3 targetRot = show ? rotationVisible : rotationHidden;
            currentSequence = currentSequence.Join(rectTransform.DORotate(targetRot, duration).SetEase(easeType));
        }

        // 페이드
        if (useFade && canvasGroup != null)
        {
            float targetAlpha = show ? fadeVisible : fadeHidden;
            currentSequence = currentSequence.Join(canvasGroup.DOFade(targetAlpha, duration).SetEase(easeType));
        }

        await currentSequence.AsyncWaitForCompletion().AsUniTask();
    }

    /// <summary>
    /// 비주얼 상태만 변경 (Active는 변경 안 함)
    /// </summary>
    private void SetVisualState(bool visible)
    {
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
        SetVisualState(visible);
        gameObject.SetActive(visible);
        isShowing = visible;
    }

    /// <summary>
    /// 현재 상태를 Hidden으로 캡처
    /// </summary>
    public void CaptureCurrentAsHidden()
    {
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
        if (useScale) scaleVisible = rectTransform.localScale;
        if (usePosition) positionVisible = rectTransform.anchoredPosition;
        if (useRotation) rotationVisible = rectTransform.localEulerAngles;
        if (useFade && canvasGroup != null) fadeVisible = canvasGroup.alpha;

        Debug.Log($"[캡처] Visible 상태 저장 - Scale:{scaleVisible}, Pos:{positionVisible}");
    }

    // Async 래퍼
    private async UniTaskVoid ShowAsync() => await Show();
    private async UniTaskVoid HideAsync() => await Hide();

    // 프로퍼티
    public bool IsShowing => isShowing;

    // ========== 테스트 버튼 ==========

    [ProButton]
    public async void TestShow()
    {
        await Show();
        Debug.Log("Show 완료");
    }

    [ProButton]
    public async void TestHide()
    {
        await Hide();
        Debug.Log("Hide 완료");
    }

    [ProButton]
    public async void TestToggle()
    {
        await Toggle();
        Debug.Log($"Toggle 완료 - IsShowing: {isShowing}");
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
