using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System;
using com.cyborgAssets.inspectorButtonPro;

public class UISimpleParticle : MonoBehaviour
{
    [Header("파티클 이미지")]
    [SerializeField] private Sprite particleSprite;
    [SerializeField] private Vector2 particleSize = new Vector2(50f, 50f);
    [SerializeField] private Color particleColor = Color.white;

    [Header("생성 설정")]
    [SerializeField] private Transform spawnPoint; // null이면 이 오브젝트 위치 사용
    [SerializeField] private CoordinateType coordinateType = CoordinateType.Canvas;
    [SerializeField] private Vector2 randomSpawnRadius = new Vector2(50f, 50f); // 랜덤 생성 범위

    [Header("발사 설정")]
    [SerializeField] private int emitCount = 5; // 한 번에 생성할 파티클 수
    [SerializeField] private float emitInterval = 0.1f; // 파티클 간 생성 간격
    [SerializeField] private int burstCount = 1; // 버스트 횟수
    [SerializeField] private float burstDelay = 1f; // 버스트 간 지연

    [Header("이동 설정")]
    [SerializeField] private MovementType movementType = MovementType.Direction;
    [SerializeField] private Vector2 moveDirection = Vector2.up; // 이동 방향
    [SerializeField] private float moveDistance = 200f; // 이동 거리
    [SerializeField] private float moveDuration = 1f; // 이동 시간
    [SerializeField] private Ease moveEase = Ease.OutQuad;
    [SerializeField] private bool randomizeDirection = false; // 방향 랜덤화
    [SerializeField] private float directionRandomAngle = 30f; // 방향 랜덤 각도

    [Header("페이드 설정")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeDelay = 0.3f; // 페이드 시작 지연
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("스케일 애니메이션")]
    [SerializeField] private bool useScaleAnimation = false;
    [SerializeField] private Vector3 startScale = Vector3.zero;
    [SerializeField] private Vector3 endScale = Vector3.one;
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private Ease scaleEase = Ease.OutBack;

    [Header("회전 애니메이션")]
    [SerializeField] private bool useRotation = false;
    [SerializeField] private float rotationSpeed = 360f; // 초당 회전 각도

    [Header("풀링 설정")]
    [SerializeField] private bool usePooling = true;
    [SerializeField] private int poolSize = 20;

    [Header("자동 재생")]
    [SerializeField] private bool playOnStart = false;
    [SerializeField] private float startDelay = 0f;

    public enum CoordinateType
    {
        Canvas,     // 캔버스 로컬 좌표
        World,      // 월드 좌표
        Screen      // 스크린 좌표
    }

    public enum MovementType
    {
        Direction,  // 지정한 방향으로
        Target,     // 특정 위치로
        Outward,    // 중심에서 바깥으로
        Random      // 랜덤 방향
    }

    private Canvas canvas;
    private RectTransform canvasRectTransform;
    private List<GameObject> particlePool = new List<GameObject>();
    private List<GameObject> activeParticles = new List<GameObject>();

    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRectTransform = canvas.GetComponent<RectTransform>();
        }

        if (usePooling)
        {
            InitializePool();
        }
    }

    /// <summary>
    /// Canvas Scale Factor 가져오기 (화면비/해상도에 따른 보정)
    /// </summary>
    float GetCanvasScaleFactor()
    {
        if (canvas != null)
        {
            return canvas.scaleFactor;
        }
        return 1f;
    }

    private async void Start()
    {
        if (playOnStart)
        {
            if (startDelay > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(startDelay), cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            await PlayParticle();
        }
    }

    /// <summary>
    /// 풀 초기화
    /// </summary>
    void InitializePool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject particle = CreateParticleObject();
            particle.SetActive(false);
            particlePool.Add(particle);
        }
    }

    /// <summary>
    /// 파티클 오브젝트 생성
    /// </summary>
    GameObject CreateParticleObject()
    {
        GameObject particle = new GameObject("Particle");
        particle.transform.SetParent(transform, false);

        Image image = particle.AddComponent<Image>();
        image.sprite = particleSprite;
        image.color = particleColor;

        RectTransform rect = particle.GetComponent<RectTransform>();

        // Canvas Scale Factor 보정 (화면비와 무관하게 일정한 크기 유지)
        float scaleFactor = GetCanvasScaleFactor();
        rect.sizeDelta = particleSize / scaleFactor;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        return particle;
    }

    /// <summary>
    /// 풀에서 파티클 가져오기
    /// </summary>
    GameObject GetParticle()
    {
        GameObject particle;

        if (usePooling && particlePool.Count > 0)
        {
            particle = particlePool[0];
            particlePool.RemoveAt(0);
            particle.SetActive(true);

            // Scale Factor가 변경되었을 수 있으므로 크기 재설정
            RectTransform rect = particle.GetComponent<RectTransform>();
            float scaleFactor = GetCanvasScaleFactor();
            rect.sizeDelta = particleSize / scaleFactor;

            activeParticles.Add(particle);
            return particle;
        }
        else
        {
            particle = CreateParticleObject();
            activeParticles.Add(particle);
            return particle;
        }
    }

    /// <summary>
    /// 풀에 파티클 반환
    /// </summary>
    void ReturnParticle(GameObject particle)
    {
        if (particle == null) return;

        activeParticles.Remove(particle);

        if (usePooling)
        {
            particle.SetActive(false);
            particlePool.Add(particle);
        }
        else
        {
            Destroy(particle);
        }
    }

    /// <summary>
    /// 파티클 재생
    /// </summary>
    public async UniTask PlayParticle()
    {
        for (int burst = 0; burst < burstCount; burst++)
        {
            if (burst > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(burstDelay), cancellationToken: this.GetCancellationTokenOnDestroy());
            }

            for (int i = 0; i < emitCount; i++)
            {
                EmitParticle().Forget();

                if (i < emitCount - 1 && emitInterval > 0)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(emitInterval), cancellationToken: this.GetCancellationTokenOnDestroy());
                }
            }
        }
    }

    /// <summary>
    /// 단일 파티클 발사
    /// </summary>
    async UniTask EmitParticle()
    {
        GameObject particle = GetParticle();
        RectTransform rectTransform = particle.GetComponent<RectTransform>();
        Image image = particle.GetComponent<Image>();

        // 생성 위치 설정 (부모 scale 보정)
        Vector2 spawnPos = GetSpawnPosition();
        rectTransform.anchoredPosition = CompensateForParentScale(spawnPos);

        // 초기 설정
        image.color = particleColor;

        if (useScaleAnimation)
        {
            rectTransform.localScale = startScale;
        }
        else
        {
            rectTransform.localScale = Vector3.one;
        }

        // 목표 위치 계산 (부모 scale 보정)
        Vector2 targetPos = CalculateTargetPosition(spawnPos);
        Vector2 compensatedTargetPos = CompensateForParentScale(targetPos);

        // 애니메이션 시퀀스
        Sequence sequence = DOTween.Sequence();

        // 스케일 애니메이션
        if (useScaleAnimation)
        {
            sequence = sequence.Join(rectTransform.DOScale(endScale, scaleDuration).SetEase(scaleEase));
        }

        // 이동 애니메이션
        sequence = sequence.Join(rectTransform.DOAnchorPos(compensatedTargetPos, moveDuration).SetEase(moveEase));

        // 회전 애니메이션
        if (useRotation)
        {
            float totalRotation = rotationSpeed * moveDuration;
            sequence = sequence.Join(rectTransform.DORotate(new Vector3(0, 0, totalRotation), moveDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
        }

        // 페이드 아웃
        if (useFade)
        {
            sequence = sequence.Insert(fadeDelay, image.DOFade(0f, fadeDuration));
        }

        try
        {
            await sequence.AsyncWaitForCompletion().AsUniTask();
        }
        catch (OperationCanceledException)
        {
            // 취소된 경우 처리
        }
        finally
        {
            ReturnParticle(particle);
        }
    }

    /// <summary>
    /// 생성 위치 계산
    /// </summary>
    Vector2 GetSpawnPosition()
    {
        Vector2 basePos = Vector2.zero;
        RectTransform myRect = transform as RectTransform;

        if (spawnPoint != null)
        {
            if (coordinateType == CoordinateType.Canvas)
            {
                RectTransform spawnRect = spawnPoint.GetComponent<RectTransform>();
                if (spawnRect != null && myRect != null && canvas != null)
                {
                    // spawnPoint의 월드 위치를 캔버스 로컬 좌표로 변환
                    Vector2 spawnCanvasPos;
                    Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRectTransform,
                        RectTransformUtility.WorldToScreenPoint(canvasCamera, spawnRect.position),
                        canvasCamera,
                        out spawnCanvasPos
                    );

                    // 이 오브젝트의 위치를 캔버스 로컬 좌표로 변환
                    Vector2 myCanvasPos;
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRectTransform,
                        RectTransformUtility.WorldToScreenPoint(canvasCamera, myRect.position),
                        canvasCamera,
                        out myCanvasPos
                    );

                    // 차이를 계산하여 이 오브젝트 기준 로컬 좌표로 변환
                    basePos = spawnCanvasPos - myCanvasPos;
                }
            }
            else if (coordinateType == CoordinateType.World)
            {
                basePos = WorldToCanvasPosition(spawnPoint.position);
            }
            else if (coordinateType == CoordinateType.Screen)
            {
                basePos = ScreenToCanvasPosition(spawnPoint.position);
            }
        }

        // 랜덤 오프셋 추가 (Canvas Scale Factor 보정)
        float scaleFactor = GetCanvasScaleFactor();
        Vector2 effectiveRadius = randomSpawnRadius / scaleFactor;

        Vector2 randomOffset = new Vector2(
            UnityEngine.Random.Range(-effectiveRadius.x, effectiveRadius.x),
            UnityEngine.Random.Range(-effectiveRadius.y, effectiveRadius.y)
        );

        return basePos + randomOffset;
    }

    /// <summary>
    /// 부모의 localScale을 고려하여 위치 보정
    /// </summary>
    Vector2 CompensateForParentScale(Vector2 position)
    {
        RectTransform parentRect = transform as RectTransform;
        if (parentRect != null)
        {
            Vector3 parentScale = parentRect.localScale;

            // scale이 0이 아닐 때만 보정
            if (Mathf.Abs(parentScale.x) > 0.001f && Mathf.Abs(parentScale.y) > 0.001f)
            {
                return new Vector2(
                    position.x / parentScale.x,
                    position.y / parentScale.y
                );
            }
        }
        return position;
    }

    /// <summary>
    /// 목표 위치 계산
    /// </summary>
    Vector2 CalculateTargetPosition(Vector2 startPos)
    {
        Vector2 direction = moveDirection.normalized;
        Vector2 movement = Vector2.zero;

        // Canvas Scale Factor 보정
        float scaleFactor = GetCanvasScaleFactor();
        float effectiveDistance = moveDistance / scaleFactor;

        switch (movementType)
        {
            case MovementType.Direction:
                if (randomizeDirection)
                {
                    float randomAngle = UnityEngine.Random.Range(-directionRandomAngle, directionRandomAngle);
                    direction = Quaternion.Euler(0, 0, randomAngle) * direction;
                }
                movement = direction * effectiveDistance;
                break;

            case MovementType.Outward:
                // 중심에서 바깥으로
                Vector2 outwardDir = startPos.normalized;
                if (outwardDir == Vector2.zero) outwardDir = UnityEngine.Random.insideUnitCircle.normalized;
                movement = outwardDir * effectiveDistance;
                break;

            case MovementType.Random:
                // 완전 랜덤 방향
                Vector2 randomDir = UnityEngine.Random.insideUnitCircle.normalized;
                movement = randomDir * effectiveDistance;
                break;

            default:
                movement = direction * effectiveDistance;
                break;
        }

        return startPos + movement;
    }

    /// <summary>
    /// 월드 좌표를 이 오브젝트 기준 로컬 좌표로 변환 (월드 오브젝트 → UI)
    /// </summary>
    Vector2 WorldToCanvasPosition(Vector3 worldPos)
    {
        if (canvas == null || canvasRectTransform == null) return Vector2.zero;
        RectTransform myRect = transform as RectTransform;
        if (myRect == null) return Vector2.zero;

        // 1. 월드 좌표 → 스크린 좌표 (메인 카메라 사용)
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return Vector2.zero;

        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

        // 2. 스크린 좌표 → 캔버스 로컬 좌표
        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // 목표 위치의 캔버스 좌표
        Vector2 targetCanvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            screenPos,
            canvasCamera,
            out targetCanvasPos
        );

        // 이 오브젝트의 캔버스 좌표
        Vector2 myCanvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            RectTransformUtility.WorldToScreenPoint(canvasCamera, myRect.position),
            canvasCamera,
            out myCanvasPos
        );

        // 차이를 반환 (이 오브젝트 기준 로컬 좌표)
        return targetCanvasPos - myCanvasPos;
    }

    /// <summary>
    /// 스크린 좌표를 이 오브젝트 기준 로컬 좌표로 변환
    /// </summary>
    Vector2 ScreenToCanvasPosition(Vector3 screenPos)
    {
        if (canvas == null || canvasRectTransform == null) return Vector2.zero;
        RectTransform myRect = transform as RectTransform;
        if (myRect == null) return Vector2.zero;

        Camera canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // 목표 위치의 캔버스 좌표
        Vector2 targetCanvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            screenPos,
            canvasCamera,
            out targetCanvasPos
        );

        // 이 오브젝트의 캔버스 좌표
        Vector2 myCanvasPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform,
            RectTransformUtility.WorldToScreenPoint(canvasCamera, myRect.position),
            canvasCamera,
            out myCanvasPos
        );

        // 차이를 반환 (이 오브젝트 기준 로컬 좌표)
        return targetCanvasPos - myCanvasPos;
    }

    /// <summary>
    /// 특정 위치에서 파티클 재생
    /// </summary>
    public async UniTask PlayAt(Vector3 position, CoordinateType coordType = CoordinateType.World)
    {
        CoordinateType originalType = coordinateType;
        Transform originalSpawn = spawnPoint;

        coordinateType = coordType;

        // 임시 스폰 포인트 생성
        GameObject tempSpawn = new GameObject("TempSpawn");
        tempSpawn.transform.position = position;
        spawnPoint = tempSpawn.transform;

        await PlayParticle();

        // 원래 설정으로 복구
        coordinateType = originalType;
        spawnPoint = originalSpawn;
        Destroy(tempSpawn);
    }

    void OnDestroy()
    {
        // 모든 활성 파티클 정리
        foreach (var particle in activeParticles)
        {
            if (particle != null)
            {
                Destroy(particle);
            }
        }
        activeParticles.Clear();

        // 풀의 모든 파티클 정리
        foreach (var particle in particlePool)
        {
            if (particle != null)
            {
                Destroy(particle);
            }
        }
        particlePool.Clear();
    }

    // ========== 테스트 버튼들 ==========

    [ProButton]
    public async void TestPlay()
    {
        await PlayParticle();
    }

    [ProButton]
    public async void TestPlay5Times()
    {
        for (int i = 0; i < 5; i++)
        {
            await PlayParticle();
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        }
    }

    [ProButton]
    public void TestSetDirectionUp()
    {
        moveDirection = Vector2.up;
        Debug.Log("방향: 위");
    }

    [ProButton]
    public void TestSetDirectionRight()
    {
        moveDirection = Vector2.right;
        Debug.Log("방향: 오른쪽");
    }

    [ProButton]
    public void TestSetMovementOutward()
    {
        movementType = MovementType.Outward;
        Debug.Log("이동 타입: 바깥으로");
    }

    [ProButton]
    public void TestSetMovementRandom()
    {
        movementType = MovementType.Random;
        Debug.Log("이동 타입: 랜덤");
    }

    [ProButton]
    public void TestToggleRotation()
    {
        useRotation = !useRotation;
        Debug.Log($"회전: {(useRotation ? "ON" : "OFF")}");
    }

    [ProButton]
    public void TestToggleFade()
    {
        useFade = !useFade;
        Debug.Log($"페이드: {(useFade ? "ON" : "OFF")}");
    }

    [ProButton]
    public void TestToggleScale()
    {
        useScaleAnimation = !useScaleAnimation;
        Debug.Log($"스케일 애니메이션: {(useScaleAnimation ? "ON" : "OFF")}");
    }

    [ProButton]
    public void TestClearAllParticles()
    {
        foreach (var particle in activeParticles.ToArray())
        {
            ReturnParticle(particle);
        }
        Debug.Log("모든 파티클 제거");
    }

    [ProButton]
    public void TestPrintStatus()
    {
        Debug.Log($"=== UISimpleParticle 상태 ===");
        Debug.Log($"활성 파티클: {activeParticles.Count}");
        Debug.Log($"풀 파티클: {particlePool.Count}");
        Debug.Log($"발사 횟수: {emitCount}, 간격: {emitInterval}초");
        Debug.Log($"버스트: {burstCount}회, 지연: {burstDelay}초");
        Debug.Log($"이동: {movementType}, 거리: {moveDistance}, 시간: {moveDuration}초");
        Debug.Log($"페이드: {useFade}, 회전: {useRotation}, 스케일: {useScaleAnimation}");
    }
}
