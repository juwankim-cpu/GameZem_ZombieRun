using UnityAtoms.BaseAtoms;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using com.cyborgAssets.inspectorButtonPro;

public class StatusBar : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private Image fillImage;

    [Header("값 설정")]
    [SerializeField] private float maxValue = 100f;
    [SerializeField] private FloatReference currentValue;
    [SerializeField] private bool useAtomValue = true; // Atom 값 사용 여부
    [SerializeField] private float manualValue = 100f; // 수동 값 (Atom 미사용 시)

    [Header("애니메이션 설정")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private Ease easeType = Ease.OutCubic;
    [SerializeField] private bool useLazyAnimation = true;

    [Header("색상 설정")]
    [SerializeField] private bool useColorGradient = true;
    [SerializeField] private bool useCustomGradient = false; // 커스텀 그라디언트 사용 여부
    [SerializeField] private Color fullColor = Color.green;
    [SerializeField] private Color emptyColor = Color.red;
    [SerializeField] private Gradient customGradient; // 커스텀 그라디언트 (선택 사항)

    [Header("디버그")]
    [SerializeField] private bool showDebugInfo = false;

    private float displayedValue;
    private float targetValue;
    private Tweener currentTween;

    void Start()
    {
        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
            if (fillImage == null)
            {
                Debug.LogError("StatusBar: Fill Image가 설정되지 않았습니다!");
                return;
            }
        }

        // 초기값 설정
        targetValue = GetCurrentValue();
        displayedValue = targetValue;
        UpdateBar(true); // 즉시 업데이트
    }

    void Update()
    {
        float newTargetValue = GetCurrentValue();

        // 값이 변경되었는지 확인
        if (!Mathf.Approximately(newTargetValue, targetValue))
        {
            targetValue = newTargetValue;
            UpdateBar(false);

            if (showDebugInfo)
            {
                Debug.Log($"StatusBar: 값 변경 {displayedValue:F1} -> {targetValue:F1} (비율: {GetRatio():P0})");
            }
        }
    }

    /// <summary>
    /// 현재 값 가져오기
    /// </summary>
    float GetCurrentValue()
    {
        if (useAtomValue && currentValue != null)
        {
            return currentValue.Value;
        }
        return manualValue;
    }

    /// <summary>
    /// 현재 비율 계산 (0~1)
    /// </summary>
    float GetRatio()
    {
        return Mathf.Clamp01(targetValue / maxValue);
    }

    /// <summary>
    /// 바 업데이트
    /// </summary>
    void UpdateBar(bool immediate)
    {
        float targetRatio = GetRatio();

        // 기존 트윈 중단
        currentTween?.Kill();

        if (immediate || !useLazyAnimation)
        {
            // 즉시 업데이트
            displayedValue = targetValue;
            fillImage.fillAmount = targetRatio;
            UpdateColor(targetRatio);
        }
        else
        {
            // 애니메이션으로 업데이트
            float currentRatio = fillImage.fillAmount;
            currentTween = DOTween.To(
                () => currentRatio,
                x =>
                {
                    fillImage.fillAmount = x;
                    UpdateColor(x);
                },
                targetRatio,
                animationDuration
            ).SetEase(easeType);

            // displayedValue도 트윈
            DOTween.To(
                () => displayedValue,
                x => displayedValue = x,
                targetValue,
                animationDuration
            ).SetEase(easeType);
        }
    }

    /// <summary>
    /// 비율에 따라 색상 업데이트
    /// </summary>
    void UpdateColor(float ratio)
    {
        if (!useColorGradient || fillImage == null) return;

        Color targetColor;

        if (useCustomGradient && customGradient != null)
        {
            // 커스텀 그라디언트 사용
            targetColor = customGradient.Evaluate(ratio);
        }
        else
        {
            // 빈 색상 -> 꽉 찬 색상으로 보간
            targetColor = Color.Lerp(emptyColor, fullColor, ratio);
        }

        fillImage.color = targetColor;

        if (showDebugInfo)
        {
            Debug.Log($"색상 업데이트: ratio={ratio:F2}, color={targetColor}");
        }
    }

    /// <summary>
    /// 값 설정 (외부에서 호출 가능)
    /// </summary>
    public void SetValue(float value)
    {
        if (useAtomValue && currentValue != null)
        {
            currentValue.Value = value;
        }
        else
        {
            manualValue = value;
        }
    }

    /// <summary>
    /// 값 추가
    /// </summary>
    public void AddValue(float amount)
    {
        SetValue(GetCurrentValue() + amount);
    }

    /// <summary>
    /// 최대값 설정
    /// </summary>
    public void SetMaxValue(float max)
    {
        maxValue = max;
        UpdateBar(false);
    }

    /// <summary>
    /// 즉시 업데이트
    /// </summary>
    public void ForceUpdate()
    {
        targetValue = GetCurrentValue();
        UpdateBar(true);
    }

    void OnDestroy()
    {
        currentTween?.Kill();
    }

    // ========== 테스트 버튼들 ==========

    [ProButton]
    public void TestSetFull()
    {
        SetValue(maxValue);
        Debug.Log($"StatusBar: 최대값으로 설정 ({maxValue})");
    }

    [ProButton]
    public void TestSetEmpty()
    {
        SetValue(0f);
        Debug.Log("StatusBar: 0으로 설정");
    }

    [ProButton]
    public void TestSetHalf()
    {
        SetValue(maxValue * 0.5f);
        Debug.Log($"StatusBar: 50%로 설정 ({maxValue * 0.5f})");
    }

    [ProButton]
    public void TestAdd10()
    {
        AddValue(10f);
        Debug.Log($"StatusBar: +10 (현재: {GetCurrentValue():F1})");
    }

    [ProButton]
    public void TestSubtract10()
    {
        AddValue(-10f);
        Debug.Log($"StatusBar: -10 (현재: {GetCurrentValue():F1})");
    }

    [ProButton]
    public void TestAdd25()
    {
        AddValue(25f);
        Debug.Log($"StatusBar: +25 (현재: {GetCurrentValue():F1})");
    }

    [ProButton]
    public void TestSubtract25()
    {
        AddValue(-25f);
        Debug.Log($"StatusBar: -25 (현재: {GetCurrentValue():F1})");
    }

    [ProButton]
    public void TestRandomValue()
    {
        float randomValue = Random.Range(0f, maxValue);
        SetValue(randomValue);
        Debug.Log($"StatusBar: 랜덤 값 설정 ({randomValue:F1})");
    }

    [ProButton]
    public void TestAnimationSpeed()
    {
        animationDuration = animationDuration == 0.3f ? 1f : 0.3f;
        Debug.Log($"애니메이션 속도 변경: {animationDuration}초");
    }

    [ProButton]
    public void TestToggleLazyAnimation()
    {
        useLazyAnimation = !useLazyAnimation;
        Debug.Log($"Lazy 애니메이션: {(useLazyAnimation ? "ON" : "OFF")}");
    }

    [ProButton]
    public void TestToggleColorGradient()
    {
        useColorGradient = !useColorGradient;
        UpdateBar(true);
        Debug.Log($"색상 그라디언트: {(useColorGradient ? "ON" : "OFF")}");
    }

    [ProButton]
    public void TestToggleCustomGradient()
    {
        useCustomGradient = !useCustomGradient;
        UpdateBar(true);
        Debug.Log($"커스텀 그라디언트: {(useCustomGradient ? "ON" : "OFF")}");
    }

    [ProButton]
    public void TestSetRedToGreen()
    {
        emptyColor = Color.red;
        fullColor = Color.green;
        UpdateBar(true);
        Debug.Log("색상 설정: 빨강 → 초록");
    }

    [ProButton]
    public void TestSetBlackToWhite()
    {
        emptyColor = Color.black;
        fullColor = Color.white;
        UpdateBar(true);
        Debug.Log("색상 설정: 검정 → 하양");
    }

    [ProButton]
    public void TestPrintStatus()
    {
        Debug.Log($"=== StatusBar 상태 ===");
        Debug.Log($"현재 값: {GetCurrentValue():F1} / {maxValue}");
        Debug.Log($"비율: {GetRatio():P0}");
        Debug.Log($"Fill Amount: {fillImage.fillAmount:F2}");
        Debug.Log($"색상: {fillImage.color}");
        Debug.Log($"Empty Color: {emptyColor}, Full Color: {fullColor}");
        Debug.Log($"Atom 사용: {useAtomValue}");
        Debug.Log($"색상 그라디언트: {useColorGradient}, 커스텀: {useCustomGradient}");
        Debug.Log($"애니메이션: {animationDuration}초, Lazy: {useLazyAnimation}");
    }
}
