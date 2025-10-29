using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Canvas에 붙여서 Safe Area 안에 UI를 배치하는 스크립트
/// LetterboxManager가 있어야 작동
/// </summary>
[RequireComponent(typeof(Canvas))]
public class SafeAreaFitter : MonoBehaviour
{
    [Header("설정")]
    [SerializeField] private bool autoSetup = true; // Awake에서 자동 세팅

    [Header("디버그")]
    [SerializeField] private bool showDebugInfo = false;

    private RectTransform canvasRect;
    private RectTransform safeAreaRoot;
    private Canvas canvas;
    private CanvasScaler canvasScaler;
    private bool isActive = false;

    // 자식들의 원래 스케일 저장
    private Dictionary<Transform, Vector3> originalChildScales = new Dictionary<Transform, Vector3>();

    // 화면/스케일 변화 감지용
    private Vector2Int lastScreenSize;
    private float lastScaleFactor;

    void Awake()
    {
        canvasRect = GetComponent<RectTransform>();
        canvas = GetComponent<Canvas>();
        canvasScaler = GetComponent<CanvasScaler>();

        // LetterboxManager가 없으면 작동 안함
        if (LetterboxManager.Instance == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[{gameObject.name}] LetterboxManager가 없어서 SafeAreaFitter가 작동하지 않습니다.");
            }
            enabled = false;
            return;
        }

        isActive = true;

        if (autoSetup)
        {
            SetupSafeArea();
        }

        // LetterboxManager의 SafeArea 변경 이벤트 구독
        LetterboxManager.Instance.OnSafeAreaChanged += ApplySafeArea;
    }

    void Start()
    {
        if (!isActive) return;

        // 초기 SafeArea 적용 (이벤트와 별개로 한 번 실행)
        ApplySafeArea();

        // 변화 감지 초기화
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastScaleFactor = GetCanvasScaleFactor();
    }

    void Update()
    {
        if (!isActive) return;

        // 화면 해상도 또는 Canvas 스케일 변화 감지 시 재적용
        bool screenChanged = Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y;
        float currentScaleFactor = GetCanvasScaleFactor();
        bool scaleChanged = !Mathf.Approximately(currentScaleFactor, lastScaleFactor);

        if (screenChanged || scaleChanged)
        {
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            lastScaleFactor = currentScaleFactor;
            ApplySafeArea();

            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] SafeAreaFitter 리프레시: screenChanged={screenChanged}, scaleChanged={scaleChanged}");
            }
        }
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제
        if (LetterboxManager.Instance != null)
        {
            LetterboxManager.Instance.OnSafeAreaChanged -= ApplySafeArea;
        }
    }

    /// <summary>
    /// Safe Area Root 생성 및 기존 자식들 이동
    /// </summary>
    public void SetupSafeArea()
    {
        // SafeArea Root가 이미 있는지 체크
        Transform existingSafeArea = transform.Find("SafeAreaRoot");
        if (existingSafeArea != null)
        {
            safeAreaRoot = existingSafeArea.GetComponent<RectTransform>();

            if (showDebugInfo)
            {
                Debug.Log($"[{gameObject.name}] SafeAreaRoot 이미 존재");
            }
            return;
        }

        // SafeArea Root 생성
        GameObject safeAreaObj = new GameObject("SafeAreaRoot");
        safeAreaObj.transform.SetParent(transform, false);
        safeAreaRoot = safeAreaObj.AddComponent<RectTransform>();

        // 앵커를 화면 전체로 설정
        safeAreaRoot.anchorMin = Vector2.zero;
        safeAreaRoot.anchorMax = Vector2.one;
        safeAreaRoot.sizeDelta = Vector2.zero;
        safeAreaRoot.anchoredPosition = Vector2.zero;

        // 기존 자식들을 SafeArea로 이동
        List<Transform> children = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child != safeAreaRoot.transform)
            {
                children.Add(child);
            }
        }

        foreach (Transform child in children)
        {
            child.SetParent(safeAreaRoot, true);
        }

        // 자식들의 원래 스케일 저장
        SaveOriginalChildScales();

        if (showDebugInfo)
        {
            Debug.Log($"[{gameObject.name}] SafeAreaRoot 생성 완료, {children.Count}개 자식 이동");
        }
    }

    /// <summary>
    /// SafeAreaRoot 하위 모든 고정 앵커 UI의 원래 스케일 재귀적으로 저장
    /// </summary>
    void SaveOriginalChildScales()
    {
        if (safeAreaRoot == null) return;

        originalChildScales.Clear();

        foreach (Transform child in safeAreaRoot)
        {
            SaveScalesRecursive(child);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[{gameObject.name}] {originalChildScales.Count}개 UI의 원래 스케일 저장 (재귀)");
        }
    }

    /// <summary>
    /// 재귀적으로 스케일 저장
    /// - Stretch 앵커: 자신은 저장 안 하고, 자식들 재귀 처리
    /// - 고정 앵커: 자신만 저장 (자식들은 부모 스케일 상속)
    /// </summary>
    void SaveScalesRecursive(Transform target)
    {
        RectTransform targetRect = target.GetComponent<RectTransform>();

        if (targetRect != null && IsStretchAnchor(targetRect))
        {
            // Stretch 앵커: 자신은 저장 안 하고, 자식들 재귀
            foreach (Transform child in target)
            {
                SaveScalesRecursive(child);
            }
        }
        else
        {
            // 고정 앵커: 자신의 스케일 저장
            originalChildScales[target] = target.localScale;
        }
    }

    /// <summary>
    /// Safe Area 적용
    /// </summary>
    void ApplySafeArea()
    {
        if (safeAreaRoot == null)
        {
            Debug.LogWarning($"[{gameObject.name}] SafeAreaRoot가 없습니다. SetupSafeArea()를 먼저 호출하세요.");
            return;
        }

        if (LetterboxManager.Instance == null)
        {
            Debug.LogWarning($"[{gameObject.name}] LetterboxManager가 없어서 SafeArea를 적용할 수 없습니다.");
            return;
        }

        // 픽셀 offset 값
        float topOffsetPixels = LetterboxManager.Instance.TopOffset;
        float bottomOffsetPixels = LetterboxManager.Instance.BottomOffset;
        float leftOffsetPixels = LetterboxManager.Instance.LeftOffset;
        float rightOffsetPixels = LetterboxManager.Instance.RightOffset;

        // Canvas Scaler를 고려한 offset 계산
        float scaleFactor = GetCanvasScaleFactor();

        // 픽셀 → Canvas 좌표계로 변환
        float topOffset = topOffsetPixels / scaleFactor;
        float bottomOffset = bottomOffsetPixels / scaleFactor;
        float leftOffset = leftOffsetPixels / scaleFactor;
        float rightOffset = rightOffsetPixels / scaleFactor;

        // SafeArea 조정
        safeAreaRoot.anchorMin = Vector2.zero;
        safeAreaRoot.anchorMax = Vector2.one;
        safeAreaRoot.offsetMin = new Vector2(leftOffset, bottomOffset);  // left, bottom
        safeAreaRoot.offsetMax = new Vector2(-rightOffset, -topOffset); // right, top

        // SafeAreaRoot의 직계 자식들 스케일 조정
        float scaleRatio = CalculateSafeAreaScale();
        ApplyScaleToChildren(scaleRatio);

        if (showDebugInfo)
        {
            Debug.Log($"[{gameObject.name}] 픽셀 Offset: Top={topOffsetPixels}, Bottom={bottomOffsetPixels}, Left={leftOffsetPixels}, Right={rightOffsetPixels}");
            Debug.Log($"[{gameObject.name}] Canvas Scale Factor: {scaleFactor}");
            Debug.Log($"[{gameObject.name}] 최종 Offset: Top={topOffset}, Bottom={bottomOffset}, Left={leftOffset}, Right={rightOffset}");
            Debug.Log($"[{gameObject.name}] UI Scale Ratio: {scaleRatio}");
        }
    }

    /// <summary>
    /// SafeAreaRoot의 직계 자식들에게 스케일 적용 (재귀 시작점)
    /// </summary>
    void ApplyScaleToChildren(float scale)
    {
        if (safeAreaRoot == null) return;

        foreach (Transform child in safeAreaRoot)
        {
            ApplyScaleRecursive(child, scale);
        }

        if (showDebugInfo)
        {
            Debug.Log($"[{gameObject.name}] 재귀적 스케일 적용 완료: scale={scale}");
        }
    }

    /// <summary>
    /// 재귀적으로 스케일 적용
    /// - Stretch 앵커: 자신은 스케일 안 받고, 자식들 재귀 처리
    /// - 고정 앵커: 자신은 스케일 받고, 자식들은 처리 안 함
    /// </summary>
    void ApplyScaleRecursive(Transform target, float scale)
    {
        RectTransform targetRect = target.GetComponent<RectTransform>();

        if (targetRect != null && IsStretchAnchor(targetRect))
        {
            // Stretch 앵커: 자신은 스케일 조정 안 하고, 자식들 재귀 처리
            foreach (Transform child in target)
            {
                ApplyScaleRecursive(child, scale);
            }
        }
        else
        {
            // 고정 앵커: 자신에게 스케일 적용, 자식들은 부모 스케일 상속받으니 처리 안 함
            if (originalChildScales.TryGetValue(target, out Vector3 originalScale))
            {
                target.localScale = originalScale * scale;
            }
            else
            {
                originalChildScales[target] = target.localScale;
                target.localScale = target.localScale * scale;
            }
        }
    }

    /// <summary>
    /// RectTransform이 Stretch 앵커인지 확인
    /// </summary>
    bool IsStretchAnchor(RectTransform rectTransform)
    {
        // 가로 또는 세로 방향이 stretch면 true
        bool isHorizontalStretch = rectTransform.anchorMin.x != rectTransform.anchorMax.x;
        bool isVerticalStretch = rectTransform.anchorMin.y != rectTransform.anchorMax.y;

        return isHorizontalStretch || isVerticalStretch;
    }

    /// <summary>
    /// Safe Area 기준 UI 스케일 비율 계산
    /// </summary>
    float CalculateSafeAreaScale()
    {
        if (canvasScaler == null || canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            return 1f;
        }

        Vector2 referenceResolution = canvasScaler.referenceResolution;
        float match = canvasScaler.matchWidthOrHeight;

        // Safe Area의 실제 픽셀 크기
        Rect safeAreaRect = LetterboxManager.Instance.SafeAreaRect;
        float safeAreaWidth = safeAreaRect.width;
        float safeAreaHeight = safeAreaRect.height;

        // Safe Area를 기준으로 한 스케일 계산
        float safeWidthScale = safeAreaWidth / referenceResolution.x;
        float safeHeightScale = safeAreaHeight / referenceResolution.y;
        float safeScaleFactor = Mathf.Lerp(safeWidthScale, safeHeightScale, match);

        // 전체 화면 기준 스케일
        float fullScaleFactor = GetCanvasScaleFactor();

        // 비율 계산: Safe Area 스케일 / 전체 스케일
        float scaleRatio = safeScaleFactor / fullScaleFactor;

        return scaleRatio;
    }

    /// <summary>
    /// Canvas의 실제 스케일 팩터 계산
    /// </summary>
    float GetCanvasScaleFactor()
    {
        if (canvasScaler == null || canvasScaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
        {
            // CanvasScaler가 없거나 다른 모드면 1.0 반환
            return 1f;
        }

        Vector2 referenceResolution = canvasScaler.referenceResolution;
        float match = canvasScaler.matchWidthOrHeight;

        // Width 기준 스케일
        float widthScale = Screen.width / referenceResolution.x;

        // Height 기준 스케일
        float heightScale = Screen.height / referenceResolution.y;

        // Match 값에 따라 보간
        float scaleFactor = Mathf.Lerp(widthScale, heightScale, match);

        return scaleFactor;
    }

    /// <summary>
    /// 수동으로 SafeArea 갱신 (필요 시 호출)
    /// </summary>
    public void RefreshSafeArea()
    {
        ApplySafeArea();
    }
}

