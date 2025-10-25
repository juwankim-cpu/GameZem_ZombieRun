using UnityEngine;
using DG.Tweening;
using com.cyborgAssets.inspectorButtonPro;

public class HeartBeat : MonoBehaviour
{
    [Header("박동 설정")]
    [SerializeField] private float beatScale = 1.2f; // 박동 시 스케일
    [SerializeField] private float beatDuration = 0.15f; // 한 번 박동 시간
    [SerializeField] private float beatInterval = 0.8f; // 박동 간격
    [SerializeField] private bool doubleBeat = true; // 두 번 박동 (실제 심장처럼)
    [SerializeField] private float doubleBeatDelay = 0.15f; // 두 번째 박동 간격

    [Header("시작 설정")]
    [SerializeField] private bool autoStart = true;
    [SerializeField] private float startDelay = 0f;

    [Header("Ease 설정")]
    [SerializeField] private Ease beatEase = Ease.OutQuad;
    [SerializeField] private Ease returnEase = Ease.InQuad;

    [Header("추가 효과")]
    [SerializeField] private bool useRotation = false; // 회전 효과
    [SerializeField] private float rotationAngle = 5f; // 회전 각도

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Sequence beatSequence;
    private bool isBeating = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalScale = rectTransform.localScale;
    }

    void Start()
    {
        if (autoStart)
        {
            if (startDelay > 0)
            {
                DOVirtual.DelayedCall(startDelay, () => StartBeating());
            }
            else
            {
                StartBeating();
            }
        }
    }

    /// <summary>
    /// 박동 시작
    /// </summary>
    public void StartBeating()
    {
        if (isBeating) return;

        isBeating = true;
        PlayBeat();
    }

    /// <summary>
    /// 박동 중지
    /// </summary>
    public void StopBeating()
    {
        isBeating = false;
        beatSequence?.Kill();
        rectTransform.localScale = originalScale;
        rectTransform.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 박동 재생
    /// </summary>
    void PlayBeat()
    {
        if (!isBeating) return;

        beatSequence?.Kill();
        beatSequence = DOTween.Sequence();

        if (doubleBeat)
        {
            // 첫 번째 박동
            AddBeatToSequence(beatSequence);

            // 짧은 대기
            beatSequence.AppendInterval(doubleBeatDelay);

            // 두 번째 박동 (조금 약하게)
            AddBeatToSequence(beatSequence, 0.8f);

            // 다음 박동까지 대기
            beatSequence.AppendInterval(beatInterval);
        }
        else
        {
            // 단일 박동
            AddBeatToSequence(beatSequence);

            // 다음 박동까지 대기
            beatSequence.AppendInterval(beatInterval);
        }

        // 반복
        beatSequence.OnComplete(() => PlayBeat());
    }

    /// <summary>
    /// 시퀀스에 박동 추가
    /// </summary>
    void AddBeatToSequence(Sequence sequence, float intensity = 1f)
    {
        float targetScale = 1f + (beatScale - 1f) * intensity;

        // 스케일 커지기
        sequence.Append(rectTransform.DOScale(originalScale * targetScale, beatDuration).SetEase(beatEase));

        // 회전 효과
        if (useRotation)
        {
            float angle = rotationAngle * intensity;
            sequence.Join(rectTransform.DORotate(new Vector3(0, 0, angle), beatDuration * 0.5f).SetEase(Ease.OutQuad));
            sequence.Append(rectTransform.DORotate(Vector3.zero, beatDuration * 0.5f).SetEase(Ease.InQuad));
        }

        // 스케일 돌아오기
        sequence.Append(rectTransform.DOScale(originalScale, beatDuration).SetEase(returnEase));
    }

    /// <summary>
    /// 한 번만 박동
    /// </summary>
    public void BeatOnce()
    {
        Sequence onceSequence = DOTween.Sequence();
        AddBeatToSequence(onceSequence);
    }

    /// <summary>
    /// 강한 박동 (긴급 상황 등)
    /// </summary>
    public void BeatIntense()
    {
        Sequence intenseSequence = DOTween.Sequence();
        AddBeatToSequence(intenseSequence, 1.5f);
    }

    /// <summary>
    /// 박동 속도 변경
    /// </summary>
    public void SetBeatSpeed(float speed)
    {
        beatInterval = 0.8f / speed; // speed가 2면 2배 빠르게

        if (isBeating)
        {
            StopBeating();
            StartBeating();
        }
    }

    /// <summary>
    /// 박동 강도 변경
    /// </summary>
    public void SetBeatIntensity(float intensity)
    {
        beatScale = 1f + 0.2f * intensity; // intensity가 1이면 1.2, 2면 1.4
    }

    void OnDestroy()
    {
        beatSequence?.Kill();
    }

    // ========== 테스트 버튼들 ==========

    [ProButton]
    public void TestStartBeating()
    {
        StartBeating();
        Debug.Log("심장 박동 시작");
    }

    [ProButton]
    public void TestStopBeating()
    {
        StopBeating();
        Debug.Log("심장 박동 중지");
    }

    [ProButton]
    public void TestBeatOnce()
    {
        BeatOnce();
        Debug.Log("한 번 박동");
    }

    [ProButton]
    public void TestBeatIntense()
    {
        BeatIntense();
        Debug.Log("강한 박동");
    }

    [ProButton]
    public void TestSpeedNormal()
    {
        SetBeatSpeed(1f);
        Debug.Log("속도: 보통 (1x)");
    }

    [ProButton]
    public void TestSpeedFast()
    {
        SetBeatSpeed(2f);
        Debug.Log("속도: 빠름 (2x)");
    }

    [ProButton]
    public void TestSpeedSlow()
    {
        SetBeatSpeed(0.5f);
        Debug.Log("속도: 느림 (0.5x)");
    }

    [ProButton]
    public void TestIntensityWeak()
    {
        SetBeatIntensity(0.5f);
        Debug.Log("강도: 약함 (0.5x)");
    }

    [ProButton]
    public void TestIntensityNormal()
    {
        SetBeatIntensity(1f);
        Debug.Log("강도: 보통 (1x)");
    }

    [ProButton]
    public void TestIntensityStrong()
    {
        SetBeatIntensity(2f);
        Debug.Log("강도: 강함 (2x)");
    }

    [ProButton]
    public void TestToggleDoubleBeat()
    {
        doubleBeat = !doubleBeat;
        if (isBeating)
        {
            StopBeating();
            StartBeating();
        }
        Debug.Log($"이중 박동: {(doubleBeat ? "ON" : "OFF")}");
    }

    [ProButton]
    public void TestToggleRotation()
    {
        useRotation = !useRotation;
        Debug.Log($"회전 효과: {(useRotation ? "ON" : "OFF")}");
    }

    [ProButton]
    public void TestPrintStatus()
    {
        Debug.Log($"=== HeartBeat 상태 ===");
        Debug.Log($"박동 중: {isBeating}");
        Debug.Log($"박동 스케일: {beatScale}");
        Debug.Log($"박동 시간: {beatDuration}초");
        Debug.Log($"박동 간격: {beatInterval}초");
        Debug.Log($"이중 박동: {doubleBeat}");
        Debug.Log($"회전 효과: {useRotation}");
        Debug.Log($"원본 스케일: {originalScale}");
    }
}
