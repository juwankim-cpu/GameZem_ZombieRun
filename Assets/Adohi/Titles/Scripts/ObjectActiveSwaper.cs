using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using com.cyborgAssets.inspectorButtonPro;

public class ObjectActiveSwaper : MonoBehaviour
{
    [Header("스왑할 오브젝트")]
    [SerializeField] private GameObject objectA;
    [SerializeField] private GameObject objectB;

    [Header("스왑 설정")]
    [SerializeField] private float swapInterval = 1f; // 스왑 간격 (초)
    [SerializeField] private bool autoStart = true; // 자동 시작
    [SerializeField] private float startDelay = 0f; // 시작 지연
    [SerializeField] private InitialActiveObject initialActive = InitialActiveObject.ObjectA;

    [Header("반복 설정")]
    [SerializeField] private bool loop = true; // 무한 반복
    [SerializeField] private int swapCount = 10; // 스왑 횟수 (loop가 false일 때)

    [Header("깜빡임 효과")]
    [SerializeField] private float blinkFastInterval = 0.1f; // 빠른 깜빡임 간격
    [SerializeField] private int blinkBeforeTurnOffCount = 5; // 꺼지기 전 깜빡임 횟수
    [SerializeField] private bool accelerateBlink = false; // 깜빡임 가속 여부
    [SerializeField] private float accelerationFactor = 0.8f; // 가속 비율 (0.8 = 20% 빨라짐)

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = false;

    public enum InitialActiveObject
    {
        ObjectA,
        ObjectB,
        Both,
        None
    }

    private bool isSwapping = false;
    private bool currentAActive = true; // 현재 A가 활성화되어 있는지
    private int currentSwapCount = 0;

    void Start()
    {
        // 초기 상태 설정
        SetInitialState();

        if (autoStart)
        {
            StartSwapping().Forget();
        }
    }

    /// <summary>
    /// 초기 상태 설정
    /// </summary>
    void SetInitialState()
    {
        if (objectA == null || objectB == null)
        {
            Debug.LogError("ObjectActiveSwaper: objectA 또는 objectB가 설정되지 않았습니다!");
            return;
        }

        switch (initialActive)
        {
            case InitialActiveObject.ObjectA:
                objectA.SetActive(true);
                objectB.SetActive(false);
                currentAActive = true;
                break;

            case InitialActiveObject.ObjectB:
                objectA.SetActive(false);
                objectB.SetActive(true);
                currentAActive = false;
                break;

            case InitialActiveObject.Both:
                objectA.SetActive(true);
                objectB.SetActive(true);
                currentAActive = true; // 첫 스왑은 A를 끄고 B는 유지
                break;

            case InitialActiveObject.None:
                objectA.SetActive(false);
                objectB.SetActive(false);
                currentAActive = false;
                break;
        }

        if (showDebugLog)
        {
            Debug.Log($"ObjectActiveSwaper: 초기 상태 설정 완료 - A: {objectA.activeSelf}, B: {objectB.activeSelf}");
        }
    }

    /// <summary>
    /// 스왑 시작
    /// </summary>
    public async UniTask StartSwapping()
    {
        if (isSwapping)
        {
            if (showDebugLog)
            {
                Debug.LogWarning("ObjectActiveSwaper: 이미 스왑이 진행 중입니다.");
            }
            return;
        }

        if (objectA == null || objectB == null)
        {
            Debug.LogError("ObjectActiveSwaper: objectA 또는 objectB가 설정되지 않았습니다!");
            return;
        }

        isSwapping = true;
        currentSwapCount = 0;

        // 시작 지연
        if (startDelay > 0)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        if (showDebugLog)
        {
            Debug.Log("ObjectActiveSwaper: 스왑 시작");
        }

        // 스왑 루프
        while (isSwapping)
        {
            // 스왑 횟수 체크 (loop가 false일 때)
            if (!loop && currentSwapCount >= swapCount)
            {
                isSwapping = false;
                if (showDebugLog)
                {
                    Debug.Log($"ObjectActiveSwaper: 스왑 완료 (총 {currentSwapCount}회)");
                }
                break;
            }

            // 간격 대기
            await UniTask.Delay(TimeSpan.FromSeconds(swapInterval), cancellationToken: this.GetCancellationTokenOnDestroy());

            // 스왑 실행
            Swap();
            currentSwapCount++;
        }
    }

    /// <summary>
    /// 스왑 중지
    /// </summary>
    public void StopSwapping()
    {
        isSwapping = false;
        if (showDebugLog)
        {
            Debug.Log($"ObjectActiveSwaper: 스왑 중지 (총 {currentSwapCount}회)");
        }
    }

    /// <summary>
    /// 한 번 스왑
    /// </summary>
    public void Swap()
    {
        if (objectA == null || objectB == null)
        {
            Debug.LogError("ObjectActiveSwaper: objectA 또는 objectB가 설정되지 않았습니다!");
            return;
        }

        // 현재 상태의 반대로 설정
        currentAActive = !currentAActive;

        objectA.SetActive(currentAActive);
        objectB.SetActive(!currentAActive);

        if (showDebugLog)
        {
            Debug.Log($"ObjectActiveSwaper: 스왑 #{currentSwapCount + 1} - A: {currentAActive}, B: {!currentAActive}");
        }
    }

    /// <summary>
    /// Object A 활성화
    /// </summary>
    public void ActivateA()
    {
        if (objectA == null || objectB == null) return;

        objectA.SetActive(true);
        objectB.SetActive(false);
        currentAActive = true;

        if (showDebugLog)
        {
            Debug.Log("ObjectActiveSwaper: Object A 활성화");
        }
    }

    /// <summary>
    /// Object B 활성화
    /// </summary>
    public void ActivateB()
    {
        if (objectA == null || objectB == null) return;

        objectA.SetActive(false);
        objectB.SetActive(true);
        currentAActive = false;

        if (showDebugLog)
        {
            Debug.Log("ObjectActiveSwaper: Object B 활성화");
        }
    }

    /// <summary>
    /// 둘 다 활성화
    /// </summary>
    public void ActivateBoth()
    {
        if (objectA == null || objectB == null) return;

        objectA.SetActive(true);
        objectB.SetActive(true);

        if (showDebugLog)
        {
            Debug.Log("ObjectActiveSwaper: 둘 다 활성화");
        }
    }

    /// <summary>
    /// 둘 다 비활성화
    /// </summary>
    public void DeactivateBoth()
    {
        if (objectA == null || objectB == null) return;

        objectA.SetActive(false);
        objectB.SetActive(false);

        if (showDebugLog)
        {
            Debug.Log("ObjectActiveSwaper: 둘 다 비활성화");
        }
    }

    /// <summary>
    /// 스왑 간격 설정
    /// </summary>
    public void SetSwapInterval(float interval)
    {
        swapInterval = Mathf.Max(0.01f, interval);
        if (showDebugLog)
        {
            Debug.Log($"ObjectActiveSwaper: 스왑 간격 변경: {swapInterval}초");
        }
    }

    /// <summary>
    /// 초기화
    /// </summary>
    public void ResetSwapper()
    {
        StopSwapping();
        currentSwapCount = 0;
        SetInitialState();

        if (showDebugLog)
        {
            Debug.Log("ObjectActiveSwaper: 초기화");
        }
    }

    /// <summary>
    /// 빠르게 깜빡이다가 둘 다 끄기
    /// </summary>
    public async UniTask BlinkAndTurnOff()
    {
        await BlinkAndTurnOff(blinkBeforeTurnOffCount, blinkFastInterval);
    }

    /// <summary>
    /// 빠르게 깜빡이다가 둘 다 끄기 (커스텀 횟수와 간격)
    /// </summary>
    /// <param name="blinkCount">깜빡임 횟수</param>
    /// <param name="interval">깜빡임 간격</param>
    public async UniTask BlinkAndTurnOff(int blinkCount, float interval)
    {
        if (objectA == null || objectB == null)
        {
            Debug.LogError("ObjectActiveSwaper: objectA 또는 objectB가 설정되지 않았습니다!");
            return;
        }

        if (showDebugLog)
        {
            Debug.Log($"ObjectActiveSwaper: 깜빡임 시작 ({blinkCount}회, 간격: {interval}초)");
        }

        float currentInterval = interval;

        for (int i = 0; i < blinkCount; i++)
        {
            // 깜빡임 (스왑)
            currentAActive = !currentAActive;
            objectA.SetActive(currentAActive);
            objectB.SetActive(!currentAActive);

            if (showDebugLog)
            {
                Debug.Log($"ObjectActiveSwaper: 깜빡임 {i + 1}/{blinkCount} - 간격: {currentInterval:F3}초");
            }

            // 대기
            await UniTask.Delay(TimeSpan.FromSeconds(currentInterval), cancellationToken: this.GetCancellationTokenOnDestroy());

            // 가속 적용
            if (accelerateBlink && i < blinkCount - 1)
            {
                currentInterval *= accelerationFactor;
                currentInterval = Mathf.Max(currentInterval, 0.01f); // 최소 간격 보장
            }
        }

        // 둘 다 끄기
        objectA.SetActive(false);
        objectB.SetActive(false);

        if (showDebugLog)
        {
            Debug.Log("ObjectActiveSwaper: 깜빡임 완료 - 둘 다 비활성화");
        }
    }

    /// <summary>
    /// 느리게 시작해서 빠르게 깜빡이다가 끄기
    /// </summary>
    public async UniTask SlowToFastBlinkAndTurnOff()
    {
        await SlowToFastBlinkAndTurnOff(swapInterval, blinkFastInterval, blinkBeforeTurnOffCount);
    }

    /// <summary>
    /// 느리게 시작해서 빠르게 깜빡이다가 끄기 (커스텀)
    /// </summary>
    public async UniTask SlowToFastBlinkAndTurnOff(float startInterval, float endInterval, int totalCount)
    {
        if (objectA == null || objectB == null)
        {
            Debug.LogError("ObjectActiveSwaper: objectA 또는 objectB가 설정되지 않았습니다!");
            return;
        }

        if (showDebugLog)
        {
            Debug.Log($"ObjectActiveSwaper: 느림→빠름 깜빡임 시작 ({startInterval}초 → {endInterval}초, {totalCount}회)");
        }

        for (int i = 0; i < totalCount; i++)
        {
            // 간격을 선형적으로 감소
            float t = (float)i / (totalCount - 1);
            float currentInterval = Mathf.Lerp(startInterval, endInterval, t);

            // 깜빡임 (스왑)
            currentAActive = !currentAActive;
            objectA.SetActive(currentAActive);
            objectB.SetActive(!currentAActive);

            if (showDebugLog)
            {
                Debug.Log($"ObjectActiveSwaper: 깜빡임 {i + 1}/{totalCount} - 간격: {currentInterval:F3}초");
            }

            // 대기
            await UniTask.Delay(TimeSpan.FromSeconds(currentInterval), cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        // 둘 다 끄기
        objectA.SetActive(false);
        objectB.SetActive(false);

        if (showDebugLog)
        {
            Debug.Log("ObjectActiveSwaper: 느림→빠름 깜빡임 완료 - 둘 다 비활성화");
        }
    }

    /// <summary>
    /// 지정한 시간 동안 깜빡이다가 끄기
    /// </summary>
    public async UniTask BlinkForDurationAndTurnOff(float duration, float interval)
    {
        if (objectA == null || objectB == null)
        {
            Debug.LogError("ObjectActiveSwaper: objectA 또는 objectB가 설정되지 않았습니다!");
            return;
        }

        float elapsed = 0f;
        float currentInterval = interval;

        if (showDebugLog)
        {
            Debug.Log($"ObjectActiveSwaper: {duration}초 동안 깜빡임 시작");
        }

        while (elapsed < duration)
        {
            // 깜빡임 (스왑)
            currentAActive = !currentAActive;
            objectA.SetActive(currentAActive);
            objectB.SetActive(!currentAActive);

            // 대기
            await UniTask.Delay(TimeSpan.FromSeconds(currentInterval), cancellationToken: this.GetCancellationTokenOnDestroy());
            elapsed += currentInterval;

            // 가속 적용
            if (accelerateBlink)
            {
                currentInterval *= accelerationFactor;
                currentInterval = Mathf.Max(currentInterval, 0.01f);
            }
        }

        // 둘 다 끄기
        objectA.SetActive(false);
        objectB.SetActive(false);

        if (showDebugLog)
        {
            Debug.Log("ObjectActiveSwaper: 시간 기반 깜빡임 완료 - 둘 다 비활성화");
        }
    }

    void OnDestroy()
    {
        isSwapping = false;
    }

    // ========== 테스트 버튼들 ==========

    [ProButton]
    public async void TestStartSwapping()
    {
        await StartSwapping();
    }

    [ProButton]
    public void TestStopSwapping()
    {
        StopSwapping();
    }

    [ProButton]
    public void TestSwapOnce()
    {
        Swap();
    }

    [ProButton]
    public void TestActivateA()
    {
        ActivateA();
    }

    [ProButton]
    public void TestActivateB()
    {
        ActivateB();
    }

    [ProButton]
    public void TestActivateBoth()
    {
        ActivateBoth();
    }

    [ProButton]
    public void TestDeactivateBoth()
    {
        DeactivateBoth();
    }

    [ProButton]
    public void TestSetInterval05()
    {
        SetSwapInterval(0.5f);
    }

    [ProButton]
    public void TestSetInterval2()
    {
        SetSwapInterval(2f);
    }

    [ProButton]
    public void TestReset()
    {
        ResetSwapper();
    }

    [ProButton]
    public void TestToggleDebug()
    {
        showDebugLog = !showDebugLog;
        Debug.Log($"디버그 로그: {(showDebugLog ? "ON" : "OFF")}");
    }

    [ProButton]
    public async void TestBlinkAndTurnOff()
    {
        await BlinkAndTurnOff();
    }

    [ProButton]
    public async void TestBlinkFast10Times()
    {
        await BlinkAndTurnOff(10, 0.1f);
    }

    [ProButton]
    public async void TestBlinkSlow5Times()
    {
        await BlinkAndTurnOff(5, 0.3f);
    }

    [ProButton]
    public async void TestSlowToFastBlink()
    {
        await SlowToFastBlinkAndTurnOff();
    }

    [ProButton]
    public async void TestSlowToFastBlink10Times()
    {
        await SlowToFastBlinkAndTurnOff(1f, 0.05f, 10);
    }

    [ProButton]
    public async void TestBlink2SecondsAndOff()
    {
        await BlinkForDurationAndTurnOff(2f, 0.1f);
    }

    [ProButton]
    public async void TestBlink3SecondsAccelerate()
    {
        bool originalAccelerate = accelerateBlink;
        accelerateBlink = true;
        await BlinkForDurationAndTurnOff(3f, 0.2f);
        accelerateBlink = originalAccelerate;
    }

    [ProButton]
    public void TestToggleAccelerate()
    {
        accelerateBlink = !accelerateBlink;
        Debug.Log($"깜빡임 가속: {(accelerateBlink ? "ON" : "OFF")}");
    }

    [ProButton]
    public void TestPrintStatus()
    {
        Debug.Log($"=== ObjectActiveSwaper 상태 ===");
        Debug.Log($"스왑 진행 중: {isSwapping}");
        Debug.Log($"스왑 횟수: {currentSwapCount}");
        Debug.Log($"스왑 간격: {swapInterval}초");
        Debug.Log($"빠른 깜빡임 간격: {blinkFastInterval}초");
        Debug.Log($"깜빡임 횟수: {blinkBeforeTurnOffCount}");
        Debug.Log($"깜빡임 가속: {accelerateBlink} (비율: {accelerationFactor})");
        Debug.Log($"Object A: {(objectA != null ? objectA.name : "null")} - Active: {(objectA != null ? objectA.activeSelf : false)}");
        Debug.Log($"Object B: {(objectB != null ? objectB.name : "null")} - Active: {(objectB != null ? objectB.activeSelf : false)}");
        Debug.Log($"현재 A 활성: {currentAActive}");
        Debug.Log($"루프: {loop}, 목표 횟수: {(loop ? "무한" : swapCount.ToString())}");
    }
}
