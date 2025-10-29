using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using Pixelplacement;
using System;

/// <summary>
/// 레터박스를 관리하는 싱글톤 스크립트
/// Scene에 하나만 존재하며, 설정한 해상도 비율 바깥 영역을 검은색으로 처리
/// </summary>
public class LetterboxManager : Singleton<LetterboxManager>
{
    [Header("화면 비율 설정")]
    [SerializeField] private Vector2 targetResolution = new Vector2(16, 9);

    [Header("Pixel Perfect Camera 지원")]
    [SerializeField] private bool usePixelPerfectCamera = false;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private PixelPerfectCamera pixelPerfectCamera;

    [Header("레터박스 색상")]
    [SerializeField] private Color letterboxColor = Color.black;

    [Header("레터박스 렌더링")]
    [SerializeField] private int canvasSortingOrder = 9999;
    [Tooltip("레터박스 영역 클릭 차단 여부")]
    [SerializeField] private bool raycastTarget = true;

    [Header("디버그")]
    [SerializeField] private bool showDebugInfo = false;

    // 레터박스 패널들
    private Canvas letterboxCanvas;
    private GameObject topLetterbox;
    private GameObject bottomLetterbox;
    private GameObject leftLetterbox;
    private GameObject rightLetterbox;

    private Vector2Int lastScreenSize;

    // Safe Area 정보 (다른 스크립트에서 참조용)
    public Rect SafeAreaRect { get; private set; }
    public float TopOffset { get; private set; }
    public float BottomOffset { get; private set; }
    public float LeftOffset { get; private set; }
    public float RightOffset { get; private set; }

    // 타겟 비율 프로퍼티
    public float TargetAspectRatio => targetResolution.x / targetResolution.y;

    // SafeArea 변경 이벤트
    public event Action OnSafeAreaChanged;

    protected override void OnRegistration()
    {
        base.OnRegistration();

        // Camera 자동 할당
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        // PixelPerfectCamera 자동 할당
        if (usePixelPerfectCamera && pixelPerfectCamera == null && targetCamera != null)
        {
            pixelPerfectCamera = targetCamera.GetComponent<PixelPerfectCamera>();
            if (pixelPerfectCamera == null)
            {
                Debug.LogWarning("[LetterboxManager] usePixelPerfectCamera가 활성화되었지만 PixelPerfectCamera 컴포넌트를 찾을 수 없습니다.");
            }
        }

        CreateLetterboxCanvas();
        CreateLetterboxes();
        ApplyLetterbox();
    }

    void Start()
    {
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    void Update()
    {
        // 화면 크기가 변경되었는지 체크
        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            lastScreenSize = new Vector2Int(Screen.width, Screen.height);
            ApplyLetterbox();

            if (showDebugInfo)
            {
                Debug.Log($"화면 크기 변경: {Screen.width}x{Screen.height}");
            }
        }
    }

#if UNITY_WEBGL
    void OnApplicationFocus(bool hasFocus)
    {
        // WebGL에서 전체화면 토글/포커스 복귀 시 레이아웃이 어긋나는 경우 강제 리프레시
        if (hasFocus)
        {
            ApplyLetterbox();
            OnSafeAreaChanged?.Invoke();
            if (showDebugInfo)
            {
                Debug.Log("[LetterboxManager] Focus 복귀로 인한 강제 리프레시");
            }
        }
    }
#endif

    /// <summary>
    /// 레터박스 전용 Canvas 생성
    /// </summary>
    void CreateLetterboxCanvas()
    {
        GameObject canvasObj = new GameObject("LetterboxCanvas");
        canvasObj.transform.SetParent(transform);

        letterboxCanvas = canvasObj.AddComponent<Canvas>();
        letterboxCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        letterboxCanvas.sortingOrder = canvasSortingOrder;

        // CanvasScaler를 ConstantPixelSize로 설정 (레터박스는 정확한 픽셀 크기로)
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        canvasObj.AddComponent<GraphicRaycaster>();
    }

    /// <summary>
    /// 레터박스 패널들 생성
    /// </summary>
    void CreateLetterboxes()
    {
        // 상단 레터박스
        topLetterbox = CreateLetterboxPanel("TopLetterbox");
        RectTransform topRect = topLetterbox.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0, 1);
        topRect.anchorMax = new Vector2(1, 1);
        topRect.pivot = new Vector2(0.5f, 1);

        // 하단 레터박스
        bottomLetterbox = CreateLetterboxPanel("BottomLetterbox");
        RectTransform bottomRect = bottomLetterbox.GetComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0, 0);
        bottomRect.anchorMax = new Vector2(1, 0);
        bottomRect.pivot = new Vector2(0.5f, 0);

        // 좌측 레터박스
        leftLetterbox = CreateLetterboxPanel("LeftLetterbox");
        RectTransform leftRect = leftLetterbox.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0, 1);
        leftRect.pivot = new Vector2(0, 0.5f);

        // 우측 레터박스
        rightLetterbox = CreateLetterboxPanel("RightLetterbox");
        RectTransform rightRect = rightLetterbox.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.pivot = new Vector2(1, 0.5f);
    }

    /// <summary>
    /// 레터박스 패널 생성
    /// </summary>
    GameObject CreateLetterboxPanel(string name)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(letterboxCanvas.transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        Image image = panel.AddComponent<Image>();
        image.color = letterboxColor;
        image.raycastTarget = raycastTarget;

        return panel;
    }

    /// <summary>
    /// 레터박스 적용
    /// </summary>
    void ApplyLetterbox()
    {
        // === PIXEL PERFECT CAMERA 지원 시작 ===
        if (usePixelPerfectCamera && targetCamera != null)
        {
            ApplyLetterboxWithPixelPerfect();
            return;
        }
        // === PIXEL PERFECT CAMERA 지원 끝 ===

        // 기존 로직
        float currentAspect = (float)Screen.width / Screen.height;
        float targetAspect = TargetAspectRatio;

        if (currentAspect < targetAspect)
        {
            // 세로가 더 긴 경우 (16:10, 4:3 등) - 위아래 레터박스
            ApplyVerticalLetterbox(currentAspect);
        }
        else if (currentAspect > targetAspect)
        {
            // 가로가 더 긴 경우 (21:9 등) - 좌우 레터박스
            ApplyHorizontalLetterbox(currentAspect);
        }
        else
        {
            // 정확히 16:9 - 레터박스 없음
            RemoveLetterboxes();
        }
    }

    /// <summary>
    /// Pixel Perfect Camera를 고려한 레터박스 적용
    /// Crop Frame의 검은 영역을 정확히 계산하여 덮음
    /// </summary>
    void ApplyLetterboxWithPixelPerfect()
    {
        if (targetCamera == null)
        {
            Debug.LogWarning("[LetterboxManager] targetCamera가 null입니다.");
            RemoveLetterboxes();
            return;
        }

        // Camera의 실제 렌더링 영역 (픽셀 단위, 화면 좌하단 기준)
        Rect pixelRect = targetCamera.pixelRect;

        float renderX = pixelRect.x;
        float renderY = pixelRect.y;
        float renderWidth = pixelRect.width;
        float renderHeight = pixelRect.height;

        // 화면 전체 크기
        float totalWidth = Screen.width;
        float totalHeight = Screen.height;

        // Pixel Perfect Camera의 Crop Frame 검은 영역 계산
        // pixelRect가 실제 렌더링 영역이므로, 나머지가 검은 영역
        float leftPixelPerfect = renderX;
        float rightPixelPerfect = totalWidth - (renderX + renderWidth);
        float bottomPixelPerfect = renderY;
        float topPixelPerfect = totalHeight - (renderY + renderHeight);

        if (showDebugInfo)
        {
            Debug.Log($"[PixelPerfect] Screen: {totalWidth}x{totalHeight}");
            Debug.Log($"[PixelPerfect] Camera pixelRect: x={renderX}, y={renderY}, w={renderWidth}, h={renderHeight}");
            Debug.Log($"[PixelPerfect] Crop Frame 검은 영역 - L:{leftPixelPerfect}, R:{rightPixelPerfect}, T:{topPixelPerfect}, B:{bottomPixelPerfect}");
        }

        // 실제 렌더링 영역의 비율
        float renderAspect = renderWidth / renderHeight;
        float targetAspect = TargetAspectRatio;

        // 추가 레터박스 계산 (targetAspect에 맞추기)
        float additionalLeft = 0;
        float additionalRight = 0;
        float additionalTop = 0;
        float additionalBottom = 0;

        if (renderAspect > targetAspect)
        {
            // 렌더 영역이 더 가로로 넓음 → 좌우에 추가 레터박스
            float validWidth = renderHeight * targetAspect;
            float extraWidth = (renderWidth - validWidth) / 2f;
            additionalLeft = extraWidth;
            additionalRight = extraWidth;
        }
        else if (renderAspect < targetAspect)
        {
            // 렌더 영역이 더 세로로 길음 → 상하에 추가 레터박스
            float validHeight = renderWidth / targetAspect;
            float extraHeight = (renderHeight - validHeight) / 2f;
            additionalTop = extraHeight;
            additionalBottom = extraHeight;
        }

        // 최종 offset = Crop Frame 검은 영역 + 추가 레터박스
        LeftOffset = leftPixelPerfect + additionalLeft;
        RightOffset = rightPixelPerfect + additionalRight;
        TopOffset = topPixelPerfect + additionalTop;
        BottomOffset = bottomPixelPerfect + additionalBottom;

        // Safe Area 계산
        float safeX = LeftOffset;
        float safeY = BottomOffset;
        float safeWidth = totalWidth - LeftOffset - RightOffset;
        float safeHeight = totalHeight - TopOffset - BottomOffset;
        SafeAreaRect = new Rect(safeX, safeY, safeWidth, safeHeight);

        // 레터박스 UI 업데이트
        UpdateLetterboxes();

        if (showDebugInfo)
        {
            Debug.Log($"[PixelPerfect] Render Aspect: {renderAspect:F3}, Target Aspect: {targetAspect:F3}");
            Debug.Log($"[PixelPerfect] 추가 레터박스 - L:{additionalLeft}, R:{additionalRight}, T:{additionalTop}, B:{additionalBottom}");
            Debug.Log($"[PixelPerfect] 최종 Offset - L:{LeftOffset}, R:{RightOffset}, T:{TopOffset}, B:{BottomOffset}");
            Debug.Log($"[PixelPerfect] Safe Area: {SafeAreaRect}");
        }

        // 이벤트 발생
        OnSafeAreaChanged?.Invoke();
    }

    /// <summary>
    /// 레터박스 UI 업데이트 (4개 모두)
    /// 화면 전체를 기준으로 레터박스 크기 설정 (Pixel Perfect Camera 검은 영역도 덮음)
    /// </summary>
    void UpdateLetterboxes()
    {
        bool hasTop = TopOffset > 0.5f;
        bool hasBottom = BottomOffset > 0.5f;
        bool hasLeft = LeftOffset > 0.5f;
        bool hasRight = RightOffset > 0.5f;

        // 상단 - 화면 전체 너비, offset 높이
        if (topLetterbox != null)
        {
            topLetterbox.SetActive(hasTop);
            if (hasTop)
            {
                RectTransform topRect = topLetterbox.GetComponent<RectTransform>();
                topRect.anchorMin = new Vector2(0, 1);
                topRect.anchorMax = new Vector2(1, 1);
                topRect.pivot = new Vector2(0.5f, 1);
                topRect.sizeDelta = new Vector2(0, TopOffset);
                topRect.anchoredPosition = Vector2.zero;
            }
        }

        // 하단 - 화면 전체 너비, offset 높이
        if (bottomLetterbox != null)
        {
            bottomLetterbox.SetActive(hasBottom);
            if (hasBottom)
            {
                RectTransform bottomRect = bottomLetterbox.GetComponent<RectTransform>();
                bottomRect.anchorMin = new Vector2(0, 0);
                bottomRect.anchorMax = new Vector2(1, 0);
                bottomRect.pivot = new Vector2(0.5f, 0);
                bottomRect.sizeDelta = new Vector2(0, BottomOffset);
                bottomRect.anchoredPosition = Vector2.zero;
            }
        }

        // 좌측 - offset 너비, Safe Area 높이만 (상하 레터박스 제외)
        if (leftLetterbox != null)
        {
            leftLetterbox.SetActive(hasLeft);
            if (hasLeft)
            {
                RectTransform leftRect = leftLetterbox.GetComponent<RectTransform>();
                leftRect.anchorMin = new Vector2(0, 0);
                leftRect.anchorMax = new Vector2(0, 1);
                leftRect.pivot = new Vector2(0, 0.5f);
                leftRect.sizeDelta = new Vector2(LeftOffset, 0);
                leftRect.anchoredPosition = Vector2.zero;

                // 상하 레터박스가 있으면 높이 조정 (겹치지 않게)
                leftRect.offsetMin = new Vector2(0, BottomOffset);
                leftRect.offsetMax = new Vector2(LeftOffset, -TopOffset);
            }
        }

        // 우측 - offset 너비, Safe Area 높이만 (상하 레터박스 제외)
        if (rightLetterbox != null)
        {
            rightLetterbox.SetActive(hasRight);
            if (hasRight)
            {
                RectTransform rightRect = rightLetterbox.GetComponent<RectTransform>();
                rightRect.anchorMin = new Vector2(1, 0);
                rightRect.anchorMax = new Vector2(1, 1);
                rightRect.pivot = new Vector2(1, 0.5f);
                rightRect.sizeDelta = new Vector2(RightOffset, 0);
                rightRect.anchoredPosition = Vector2.zero;

                // 상하 레터박스가 있으면 높이 조정 (겹치지 않게)
                rightRect.offsetMin = new Vector2(-RightOffset, BottomOffset);
                rightRect.offsetMax = new Vector2(0, -TopOffset);
            }
        }
    }

    /// <summary>
    /// 세로 레터박스 적용 (위아래)
    /// </summary>
    void ApplyVerticalLetterbox(float currentAspect)
    {
        // 타겟 비율로 높이를 제한
        float validHeight = Screen.width / TargetAspectRatio;
        float blackBarHeight = (Screen.height - validHeight) / 2f;

        TopOffset = blackBarHeight;
        BottomOffset = blackBarHeight;
        LeftOffset = 0;
        RightOffset = 0;

        SafeAreaRect = new Rect(0, blackBarHeight, Screen.width, validHeight);

        // 상하단 레터박스 활성화
        if (topLetterbox != null && bottomLetterbox != null)
        {
            topLetterbox.SetActive(true);
            bottomLetterbox.SetActive(true);

            RectTransform topRect = topLetterbox.GetComponent<RectTransform>();
            topRect.sizeDelta = new Vector2(0, blackBarHeight);
            topRect.anchoredPosition = Vector2.zero;

            RectTransform bottomRect = bottomLetterbox.GetComponent<RectTransform>();
            bottomRect.sizeDelta = new Vector2(0, blackBarHeight);
            bottomRect.anchoredPosition = Vector2.zero;
        }

        // 좌우 레터박스 비활성화
        if (leftLetterbox != null) leftLetterbox.SetActive(false);
        if (rightLetterbox != null) rightLetterbox.SetActive(false);

        if (showDebugInfo)
        {
            Debug.Log($"세로 레터박스 적용: 화면({Screen.width}x{Screen.height}), 비율({currentAspect:F3}), 여백({blackBarHeight}px)");
        }

        // SafeArea 변경 이벤트 발생
        OnSafeAreaChanged?.Invoke();
    }

    /// <summary>
    /// 가로 레터박스 적용 (좌우)
    /// </summary>
    void ApplyHorizontalLetterbox(float currentAspect)
    {
        // 타겟 비율로 너비를 제한
        float validWidth = Screen.height * TargetAspectRatio;
        float blackBarWidth = (Screen.width - validWidth) / 2f;

        TopOffset = 0;
        BottomOffset = 0;
        LeftOffset = blackBarWidth;
        RightOffset = blackBarWidth;

        SafeAreaRect = new Rect(blackBarWidth, 0, validWidth, Screen.height);

        // 좌우 레터박스 활성화
        if (leftLetterbox != null && rightLetterbox != null)
        {
            leftLetterbox.SetActive(true);
            rightLetterbox.SetActive(true);

            RectTransform leftRect = leftLetterbox.GetComponent<RectTransform>();
            leftRect.sizeDelta = new Vector2(blackBarWidth, 0);
            leftRect.anchoredPosition = Vector2.zero;

            RectTransform rightRect = rightLetterbox.GetComponent<RectTransform>();
            rightRect.sizeDelta = new Vector2(blackBarWidth, 0);
            rightRect.anchoredPosition = Vector2.zero;
        }

        // 상하단 레터박스 비활성화
        if (topLetterbox != null) topLetterbox.SetActive(false);
        if (bottomLetterbox != null) bottomLetterbox.SetActive(false);

        if (showDebugInfo)
        {
            Debug.Log($"가로 레터박스 적용: 화면({Screen.width}x{Screen.height}), 비율({currentAspect:F3}), 여백({blackBarWidth}px)");
        }

        // SafeArea 변경 이벤트 발생
        OnSafeAreaChanged?.Invoke();
    }

    /// <summary>
    /// 레터박스 제거 (16:9인 경우)
    /// </summary>
    void RemoveLetterboxes()
    {
        TopOffset = 0;
        BottomOffset = 0;
        LeftOffset = 0;
        RightOffset = 0;

        SafeAreaRect = new Rect(0, 0, Screen.width, Screen.height);

        // 모든 레터박스 비활성화
        if (topLetterbox != null) topLetterbox.SetActive(false);
        if (bottomLetterbox != null) bottomLetterbox.SetActive(false);
        if (leftLetterbox != null) leftLetterbox.SetActive(false);
        if (rightLetterbox != null) rightLetterbox.SetActive(false);

        if (showDebugInfo)
        {
            Debug.Log($"레터박스 없음: 화면({Screen.width}x{Screen.height})");
        }

        // SafeArea 변경 이벤트 발생
        OnSafeAreaChanged?.Invoke();
    }

    /// <summary>
    /// 런타임에 레터박스 색상 변경
    /// </summary>
    public void SetLetterboxColor(Color color)
    {
        letterboxColor = color;

        if (topLetterbox != null)
            topLetterbox.GetComponent<Image>().color = color;
        if (bottomLetterbox != null)
            bottomLetterbox.GetComponent<Image>().color = color;
        if (leftLetterbox != null)
            leftLetterbox.GetComponent<Image>().color = color;
        if (rightLetterbox != null)
            rightLetterbox.GetComponent<Image>().color = color;
    }

    /// <summary>
    /// 타겟 해상도 변경
    /// </summary>
    public void SetTargetResolution(Vector2 resolution)
    {
        targetResolution = resolution;
        ApplyLetterbox();
    }

    /// <summary>
    /// 타겟 해상도 변경
    /// </summary>
    public void SetTargetResolution(float width, float height)
    {
        targetResolution = new Vector2(width, height);
        ApplyLetterbox();
    }

    /// <summary>
    /// Canvas Sorting Order 변경
    /// </summary>
    public void SetCanvasSortingOrder(int order)
    {
        canvasSortingOrder = order;
        if (letterboxCanvas != null)
        {
            letterboxCanvas.sortingOrder = order;
        }
    }

    /// <summary>
    /// Raycast Target 설정 변경
    /// </summary>
    public void SetRaycastTarget(bool enabled)
    {
        raycastTarget = enabled;

        if (topLetterbox != null)
            topLetterbox.GetComponent<Image>().raycastTarget = enabled;
        if (bottomLetterbox != null)
            bottomLetterbox.GetComponent<Image>().raycastTarget = enabled;
        if (leftLetterbox != null)
            leftLetterbox.GetComponent<Image>().raycastTarget = enabled;
        if (rightLetterbox != null)
            rightLetterbox.GetComponent<Image>().raycastTarget = enabled;
    }
}

