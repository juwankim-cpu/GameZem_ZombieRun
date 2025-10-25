using UnityEngine;
using System.Collections.Generic;
using com.cyborgAssets.inspectorButtonPro;

namespace ZombieRun.Adohi.Evironment
{
    public class BackgroundMover : MonoBehaviour
    {
        [Header("배경 설정")]
        [SerializeField] private Sprite[] sprites; // 여러 스프라이트 지원
        [SerializeField] private float moveSpeed = 2f; // 이동 속도 (단위/초)
        [SerializeField] private string sortingLayerName = "Background";
        [SerializeField] private int sortingOrder = 0;

        [Header("Sorting Order 설정")]
        [SerializeField] private bool useSortingOrderPerSprite = false; // 스프라이트별 개별 Order 사용
        [SerializeField] private int[] sortingOrders; // 각 스프라이트에 대응하는 Order

        [Header("반복 설정")]
        [SerializeField] private int instanceCount = 3; // 생성할 배경 개수
        [SerializeField] private float spacing = 0f; // 이미지 간 간격 (양수: 간격, 음수: 겹침)

        [Header("초기 위치 설정")]
        [SerializeField] private AlignmentMode alignmentMode = AlignmentMode.Right;
        [SerializeField] private float startOffset = 0f; // 시작 위치 오프셋 (수동 조정용)

        public enum AlignmentMode
        {
            Left,       // 왼쪽 정렬 (왼쪽부터 채움)
            Center,     // 중앙 정렬 (양쪽 균등)
            Right,      // 오른쪽 정렬 (오른쪽으로만)
            CameraFit,  // 카메라 화면에 맞춤 (화면 왼쪽부터 채움)
            Custom      // 커스텀 (startOffset 사용)
        }

        [Header("스프라이트 변경 설정")]
        [SerializeField] private SpriteChangeMode spriteChangeMode = SpriteChangeMode.Sequential; // 스프라이트 변경 모드
        [SerializeField] private bool changeOnReposition = true; // 리젠 시 스프라이트 변경 여부
        [SerializeField] private int changeAfterRepositionCount = 1; // 몇 번 리젠 후 변경할지 (1 = 매번)

        public enum SpriteChangeMode
        {
            Fixed,       // 고정 (변경 안함)
            Sequential,  // 순차적
            Random,      // 랜덤
            PingPong     // 왕복 (0->1->2->1->0...)
        }

        private List<BackgroundInstance> backgroundInstances = new List<BackgroundInstance>();
        private float spriteWidth;
        private float totalWidth; // 스프라이트 너비 + 간격
        private int currentSpriteIndex = 0;
        private int nextSpriteIndex = 0; // 다음 리젠 시 사용할 스프라이트 인덱스
        private bool useCustomNextSprite = false; // 커스텀 스프라이트 인덱스 사용 여부
        private int repositionCounter = 0;
        private bool pingPongReverse = false; // PingPong 모드용

        private class BackgroundInstance
        {
            public GameObject gameObject;
            public SpriteRenderer spriteRenderer;
            public int spriteIndex;
        }

        void Start()
        {
            if (sprites == null || sprites.Length == 0)
            {
                Debug.LogError("BackgroundMover: Sprite가 설정되지 않았습니다!");
                return;
            }

            // 스프라이트 너비 계산 (첫 번째 스프라이트 기준)
            spriteWidth = sprites[0].bounds.size.x;
            totalWidth = spriteWidth + spacing;

            // 배경 인스턴스 생성
            CreateBackgroundInstances();
        }

        void Update()
        {
            if (backgroundInstances.Count == 0) return;

            // 모든 배경을 왼쪽으로 이동
            foreach (var bgInstance in backgroundInstances)
            {
                bgInstance.gameObject.transform.position += Vector3.left * moveSpeed * Time.deltaTime;
            }

            // 가장 왼쪽 배경이 화면 밖으로 나가면 오른쪽 끝으로 이동
            CheckAndRepositionBackgrounds();
        }

        void CreateBackgroundInstances()
        {
            // 초기 오프셋 계산
            float initialOffset = CalculateInitialOffset();

            for (int i = 0; i < instanceCount; i++)
            {
                GameObject bgObject = new GameObject($"Background_{i}");
                bgObject.transform.SetParent(transform);

                // SpriteRenderer 추가
                SpriteRenderer sr = bgObject.AddComponent<SpriteRenderer>();

                // 초기 스프라이트 인덱스 결정
                int spriteIndex = GetInitialSpriteIndex(i);
                sr.sprite = sprites[spriteIndex];
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = GetSortingOrderForSprite(spriteIndex);

                // 초기 위치 설정 (오프셋 포함)
                float xPosition = initialOffset + totalWidth * i;
                bgObject.transform.position = transform.position + Vector3.right * xPosition;

                // BackgroundInstance 생성 및 추가
                BackgroundInstance instance = new BackgroundInstance
                {
                    gameObject = bgObject,
                    spriteRenderer = sr,
                    spriteIndex = spriteIndex
                };

                backgroundInstances.Add(instance);
            }
        }

        /// <summary>
        /// 초기 오프셋 계산
        /// </summary>
        float CalculateInitialOffset()
        {
            switch (alignmentMode)
            {
                case AlignmentMode.Left:
                    // 왼쪽 정렬: 첫 번째가 기준점에서 시작하고 왼쪽으로 확장
                    return -(instanceCount - 1) * totalWidth;

                case AlignmentMode.Center:
                    // 중앙 정렬: 중앙을 기준으로 양쪽에 배치
                    float totalSpan = (instanceCount - 1) * totalWidth;
                    return -totalSpan / 2f;

                case AlignmentMode.Right:
                    // 오른쪽 정렬: 기존 동작 (오른쪽으로만)
                    return 0f;

                case AlignmentMode.CameraFit:
                    // 카메라에 맞춤: 화면 왼쪽부터 채움
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        // 카메라 왼쪽 끝 위치 계산
                        float cameraLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;
                        // 기준점(transform.position)에서 카메라 왼쪽까지의 거리
                        float offsetToCamera = cameraLeft - transform.position.x;
                        return offsetToCamera;
                    }
                    return 0f;

                case AlignmentMode.Custom:
                    // 커스텀: 사용자 지정 오프셋
                    return startOffset;

                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 스프라이트에 대응하는 Sorting Order 가져오기
        /// </summary>
        int GetSortingOrderForSprite(int spriteIndex)
        {
            if (!useSortingOrderPerSprite)
            {
                return sortingOrder; // 기본 Order 사용
            }

            // sortingOrders 배열이 유효한지 확인
            if (sortingOrders != null && sortingOrders.Length > 0)
            {
                // 인덱스 범위 확인
                if (spriteIndex >= 0 && spriteIndex < sortingOrders.Length)
                {
                    return sortingOrders[spriteIndex];
                }
                else
                {
                    // 범위 벗어나면 마지막 값 또는 기본값 사용
                    return sortingOrders[sortingOrders.Length - 1];
                }
            }

            return sortingOrder; // 배열이 없으면 기본값
        }

        /// <summary>
        /// 초기 스프라이트 인덱스 결정
        /// </summary>
        int GetInitialSpriteIndex(int instanceIndex)
        {
            if (sprites.Length == 1) return 0;

            switch (spriteChangeMode)
            {
                case SpriteChangeMode.Sequential:
                    return instanceIndex % sprites.Length;

                case SpriteChangeMode.Random:
                    return Random.Range(0, sprites.Length);

                case SpriteChangeMode.PingPong:
                    int idx = instanceIndex % (sprites.Length * 2 - 2);
                    return idx < sprites.Length ? idx : (sprites.Length * 2 - 2 - idx);

                default: // Fixed
                    return 0;
            }
        }

        void CheckAndRepositionBackgrounds()
        {
            // 카메라의 왼쪽 경계 계산
            Camera cam = Camera.main;
            if (cam == null) return;

            float leftBound = cam.transform.position.x - cam.orthographicSize * cam.aspect - spriteWidth;

            foreach (var bgInstance in backgroundInstances)
            {
                // 배경이 화면 왼쪽 밖으로 완전히 벗어났는지 확인
                if (bgInstance.gameObject.transform.position.x < leftBound)
                {
                    // 가장 오른쪽 배경 찾기
                    float maxX = float.MinValue;
                    foreach (var otherBg in backgroundInstances)
                    {
                        if (otherBg.gameObject.transform.position.x > maxX)
                        {
                            maxX = otherBg.gameObject.transform.position.x;
                        }
                    }

                    // 가장 오른쪽 끝으로 재배치 (간격 포함)
                    Vector3 currentPos = bgInstance.gameObject.transform.position;
                    bgInstance.gameObject.transform.position = new Vector3(maxX + totalWidth, currentPos.y, currentPos.z);

                    // 스프라이트 변경
                    if (useCustomNextSprite)
                    {
                        // 커스텀 스프라이트 인덱스 사용
                        bgInstance.spriteIndex = nextSpriteIndex;
                        bgInstance.spriteRenderer.sprite = sprites[nextSpriteIndex];
                        bgInstance.spriteRenderer.sortingOrder = GetSortingOrderForSprite(nextSpriteIndex);
                    }
                    else if (changeOnReposition)
                    {
                        // 자동 변경 모드
                        repositionCounter++;
                        if (repositionCounter >= changeAfterRepositionCount)
                        {
                            repositionCounter = 0;
                            ChangeSprite(bgInstance);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 다음 스프라이트로 변경
        /// </summary>
        void ChangeSprite(BackgroundInstance bgInstance)
        {
            if (sprites.Length == 1) return;

            int newSpriteIndex = GetNextSpriteIndex();
            bgInstance.spriteIndex = newSpriteIndex;
            bgInstance.spriteRenderer.sprite = sprites[newSpriteIndex];
            bgInstance.spriteRenderer.sortingOrder = GetSortingOrderForSprite(newSpriteIndex);
        }

        /// <summary>
        /// 다음 스프라이트 인덱스 가져오기
        /// </summary>
        int GetNextSpriteIndex()
        {
            switch (spriteChangeMode)
            {
                case SpriteChangeMode.Sequential:
                    currentSpriteIndex = (currentSpriteIndex + 1) % sprites.Length;
                    return currentSpriteIndex;

                case SpriteChangeMode.Random:
                    return Random.Range(0, sprites.Length);

                case SpriteChangeMode.PingPong:
                    if (pingPongReverse)
                    {
                        currentSpriteIndex--;
                        if (currentSpriteIndex <= 0)
                        {
                            currentSpriteIndex = 0;
                            pingPongReverse = false;
                        }
                    }
                    else
                    {
                        currentSpriteIndex++;
                        if (currentSpriteIndex >= sprites.Length - 1)
                        {
                            currentSpriteIndex = sprites.Length - 1;
                            pingPongReverse = true;
                        }
                    }
                    return currentSpriteIndex;

                default: // Fixed
                    return currentSpriteIndex;
            }
        }

        // 런타임에 속도 변경
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }

        // 런타임에 스프라이트 배열 변경
        public void SetSprites(Sprite[] newSprites)
        {
            if (newSprites == null || newSprites.Length == 0)
            {
                Debug.LogError("SetSprites: 유효하지 않은 스프라이트 배열입니다!");
                return;
            }

            sprites = newSprites;
            spriteWidth = sprites[0].bounds.size.x;
            totalWidth = spriteWidth + spacing;

            // 모든 배경의 스프라이트 재설정
            for (int i = 0; i < backgroundInstances.Count; i++)
            {
                var bgInstance = backgroundInstances[i];
                int spriteIndex = i % sprites.Length;
                bgInstance.spriteIndex = spriteIndex;
                bgInstance.spriteRenderer.sprite = sprites[spriteIndex];
                bgInstance.spriteRenderer.sortingOrder = GetSortingOrderForSprite(spriteIndex);
            }
        }

        /// <summary>
        /// 런타임에 스프라이트와 Sorting Order 배열 동시 변경
        /// </summary>
        public void SetSpritesWithOrders(Sprite[] newSprites, int[] newOrders)
        {
            if (newSprites == null || newSprites.Length == 0)
            {
                Debug.LogError("SetSpritesWithOrders: 유효하지 않은 스프라이트 배열입니다!");
                return;
            }

            sprites = newSprites;
            sortingOrders = newOrders;
            useSortingOrderPerSprite = (newOrders != null && newOrders.Length > 0);

            spriteWidth = sprites[0].bounds.size.x;
            totalWidth = spriteWidth + spacing;

            // 모든 배경의 스프라이트 및 Order 재설정
            for (int i = 0; i < backgroundInstances.Count; i++)
            {
                var bgInstance = backgroundInstances[i];
                int spriteIndex = i % sprites.Length;
                bgInstance.spriteIndex = spriteIndex;
                bgInstance.spriteRenderer.sprite = sprites[spriteIndex];
                bgInstance.spriteRenderer.sortingOrder = GetSortingOrderForSprite(spriteIndex);
            }
        }

        // 런타임에 단일 스프라이트로 변경 (하위 호환성)
        public void SetSprite(Sprite newSprite)
        {
            SetSprites(new Sprite[] { newSprite });
        }

        // 런타임에 간격 변경
        public void SetSpacing(float newSpacing)
        {
            spacing = newSpacing;
            totalWidth = spriteWidth + spacing;
        }

        // 런타임에 스프라이트 변경 모드 변경
        public void SetSpriteChangeMode(SpriteChangeMode mode)
        {
            spriteChangeMode = mode;
            currentSpriteIndex = 0;
            pingPongReverse = false;
        }

        // 특정 배경 인스턴스의 스프라이트 수동 변경
        public void SetBackgroundSprite(int instanceIndex, int spriteIndex)
        {
            if (instanceIndex >= 0 && instanceIndex < backgroundInstances.Count &&
                spriteIndex >= 0 && spriteIndex < sprites.Length)
            {
                var bgInstance = backgroundInstances[instanceIndex];
                bgInstance.spriteIndex = spriteIndex;
                bgInstance.spriteRenderer.sprite = sprites[spriteIndex];
                bgInstance.spriteRenderer.sortingOrder = GetSortingOrderForSprite(spriteIndex);
            }
        }

        /// <summary>
        /// 다음 리젠되는 배경부터 지정한 스프라이트를 사용
        /// </summary>
        /// <param name="spriteIndex">사용할 스프라이트 인덱스 (0부터 시작)</param>
        public void ChangeNextSprite(int spriteIndex)
        {
            if (spriteIndex >= 0 && spriteIndex < sprites.Length)
            {
                nextSpriteIndex = spriteIndex;
                useCustomNextSprite = true;
                Debug.Log($"다음 리젠 스프라이트 설정: Index {spriteIndex}");
            }
            else
            {
                Debug.LogWarning($"ChangeNextSprite: 유효하지 않은 스프라이트 인덱스 {spriteIndex} (범위: 0~{sprites.Length - 1})");
            }
        }

        /// <summary>
        /// 커스텀 스프라이트 설정 해제 (자동 모드로 복귀)
        /// </summary>
        public void ResetNextSprite()
        {
            useCustomNextSprite = false;
            Debug.Log("커스텀 스프라이트 설정 해제 - 자동 모드로 복귀");
        }

        /// <summary>
        /// 다음 리젠되는 N개의 배경에만 지정한 스프라이트를 사용
        /// </summary>
        public void ChangeNextSpriteForCount(int spriteIndex, int count)
        {
            if (spriteIndex >= 0 && spriteIndex < sprites.Length)
            {
                nextSpriteIndex = spriteIndex;
                useCustomNextSprite = true;
                // 추후 카운트 기능 구현 가능
                Debug.Log($"다음 {count}개 리젠 스프라이트 설정: Index {spriteIndex}");
            }
        }

        /// <summary>
        /// 정렬 모드 설정 (배경 재생성 필요)
        /// </summary>
        public void SetAlignmentMode(AlignmentMode mode)
        {
            alignmentMode = mode;
            Debug.Log($"정렬 모드 변경: {mode} (RecreateBackgrounds()를 호출하여 적용하세요)");
        }

        /// <summary>
        /// 시작 오프셋 설정 (Custom 모드에서 사용)
        /// </summary>
        public void SetStartOffset(float offset)
        {
            startOffset = offset;
            Debug.Log($"시작 오프셋 설정: {offset}");
        }

        /// <summary>
        /// Sorting Orders 배열 설정
        /// </summary>
        public void SetSortingOrders(int[] newOrders)
        {
            sortingOrders = newOrders;
            useSortingOrderPerSprite = (newOrders != null && newOrders.Length > 0);

            // 모든 배경의 Order 업데이트
            foreach (var bgInstance in backgroundInstances)
            {
                bgInstance.spriteRenderer.sortingOrder = GetSortingOrderForSprite(bgInstance.spriteIndex);
            }

            Debug.Log($"Sorting Orders 설정: {(useSortingOrderPerSprite ? $"{newOrders.Length}개 Order" : "비활성화")}");
        }

        /// <summary>
        /// 스프라이트별 Order 사용 토글
        /// </summary>
        public void ToggleSortingOrderPerSprite()
        {
            useSortingOrderPerSprite = !useSortingOrderPerSprite;

            // Order 업데이트
            foreach (var bgInstance in backgroundInstances)
            {
                bgInstance.spriteRenderer.sortingOrder = GetSortingOrderForSprite(bgInstance.spriteIndex);
            }

            Debug.Log($"스프라이트별 Order 사용: {useSortingOrderPerSprite}");
        }

        void OnDestroy()
        {
            // 생성한 배경 인스턴스 정리
            foreach (var bgInstance in backgroundInstances)
            {
                if (bgInstance?.gameObject != null)
                {
                    Destroy(bgInstance.gameObject);
                }
            }
            backgroundInstances.Clear();
        }

        // ========== 테스트 버튼들 (Inspector에서 사용) ==========

        [ProButton]
        public void TestChangeNextSprite0()
        {
            ChangeNextSprite(0);
        }

        [ProButton]
        public void TestChangeNextSprite1()
        {
            ChangeNextSprite(1);
        }

        [ProButton]
        public void TestChangeNextSprite2()
        {
            ChangeNextSprite(2);
        }

        [ProButton]
        public void TestChangeNextSprite3()
        {
            ChangeNextSprite(3);
        }

        [ProButton]
        public void TestResetNextSprite()
        {
            ResetNextSprite();
        }

        [ProButton]
        public void TestResetSpriteIndex()
        {
            currentSpriteIndex = 0;
            repositionCounter = 0;
            pingPongReverse = false;
            useCustomNextSprite = false;
            Debug.Log("모든 스프라이트 설정 초기화");
        }

        [ProButton]
        public void TestSpeedUp()
        {
            moveSpeed *= 2f;
            Debug.Log($"속도 증가: {moveSpeed}");
        }

        [ProButton]
        public void TestSpeedDown()
        {
            moveSpeed *= 0.5f;
            Debug.Log($"속도 감소: {moveSpeed}");
        }

        [ProButton]
        public void TestSetAlignmentLeft()
        {
            alignmentMode = AlignmentMode.Left;
            RecreateBackgrounds();
            Debug.Log("정렬 모드: 왼쪽 (화면 왼쪽부터 채움)");
        }

        [ProButton]
        public void TestSetAlignmentCenter()
        {
            alignmentMode = AlignmentMode.Center;
            RecreateBackgrounds();
            Debug.Log("정렬 모드: 중앙 (양쪽 균등)");
        }

        [ProButton]
        public void TestSetAlignmentRight()
        {
            alignmentMode = AlignmentMode.Right;
            RecreateBackgrounds();
            Debug.Log("정렬 모드: 오른쪽 (오른쪽으로만)");
        }

        [ProButton]
        public void TestSetAlignmentCameraFit()
        {
            alignmentMode = AlignmentMode.CameraFit;
            RecreateBackgrounds();
            Debug.Log("정렬 모드: 카메라 맞춤 (화면 왼쪽부터 채움)");
        }

        [ProButton]
        public void TestSetCustomOffset_Minus1000()
        {
            alignmentMode = AlignmentMode.Custom;
            startOffset = -1000f;
            RecreateBackgrounds();
            Debug.Log($"커스텀 오프셋: {startOffset}");
        }

        [ProButton]
        public void TestSetCustomOffset_Minus500()
        {
            alignmentMode = AlignmentMode.Custom;
            startOffset = -500f;
            RecreateBackgrounds();
            Debug.Log($"커스텀 오프셋: {startOffset}");
        }

        /// <summary>
        /// 배경 재생성 (설정 변경 후 적용)
        /// </summary>
        public void RecreateBackgrounds()
        {
            // 기존 배경 제거
            foreach (var bgInstance in backgroundInstances)
            {
                if (bgInstance?.gameObject != null)
                {
                    Destroy(bgInstance.gameObject);
                }
            }
            backgroundInstances.Clear();

            // 새로 생성
            CreateBackgroundInstances();
        }

        [ProButton]
        public void TestToggleSortingOrderPerSprite()
        {
            ToggleSortingOrderPerSprite();
        }

        [ProButton]
        public void TestSetSortingOrders_0_5_10()
        {
            SetSortingOrders(new int[] { 0, 5, 10 });
        }

        [ProButton]
        public void TestSetSortingOrders_10_5_0()
        {
            SetSortingOrders(new int[] { 10, 5, 0 });
        }

        [ProButton]
        public void TestSetSortingOrders_Null()
        {
            SetSortingOrders(null);
        }

        [ProButton]
        public void TestPrintSortingOrders()
        {
            Debug.Log($"=== Sorting Orders 상태 ===");
            Debug.Log($"스프라이트별 Order 사용: {useSortingOrderPerSprite}");

            if (sortingOrders != null && sortingOrders.Length > 0)
            {
                Debug.Log($"Sorting Orders 배열 크기: {sortingOrders.Length}");
                for (int i = 0; i < sortingOrders.Length; i++)
                {
                    Debug.Log($"  Sprite {i} → Order {sortingOrders[i]}");
                }
            }
            else
            {
                Debug.Log("Sorting Orders 배열: 없음 (기본값 사용)");
            }

            Debug.Log("\n현재 배경 인스턴스들:");
            for (int i = 0; i < backgroundInstances.Count; i++)
            {
                var bgInstance = backgroundInstances[i];
                Debug.Log($"  Instance {i}: Sprite Index {bgInstance.spriteIndex}, Order {bgInstance.spriteRenderer.sortingOrder}");
            }
        }

        [ProButton]
        public void TestPrintCurrentStatus()
        {
            Debug.Log($"=== BackgroundMover 상태 ===");
            Debug.Log($"스프라이트 개수: {sprites?.Length ?? 0}");
            Debug.Log($"배경 인스턴스 개수: {backgroundInstances.Count}");
            Debug.Log($"정렬 모드: {alignmentMode}");
            Debug.Log($"시작 오프셋: {CalculateInitialOffset()}");
            Debug.Log($"현재 스프라이트 인덱스: {currentSpriteIndex}");
            Debug.Log($"다음 리젠 스프라이트: {(useCustomNextSprite ? nextSpriteIndex.ToString() : "자동")}");
            Debug.Log($"이동 속도: {moveSpeed}");
            Debug.Log($"변경 모드: {spriteChangeMode}");
            Debug.Log($"스프라이트별 Order 사용: {useSortingOrderPerSprite}");
            Debug.Log($"Sorting Orders 배열 크기: {(sortingOrders != null ? sortingOrders.Length : 0)}");
        }
    }
}
