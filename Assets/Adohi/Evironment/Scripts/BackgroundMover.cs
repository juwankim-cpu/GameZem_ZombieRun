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

        [Header("반복 설정")]
        [SerializeField] private int instanceCount = 3; // 생성할 배경 개수
        [SerializeField] private float spacing = 0f; // 이미지 간 간격 (양수: 간격, 음수: 겹침)

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
                sr.sortingOrder = sortingOrder;

                // 초기 위치 설정 (나란히 배치, 간격 포함)
                bgObject.transform.position = transform.position + Vector3.right * totalWidth * i;

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
        public void TestPrintCurrentStatus()
        {
            Debug.Log($"=== BackgroundMover 상태 ===");
            Debug.Log($"스프라이트 개수: {sprites?.Length ?? 0}");
            Debug.Log($"배경 인스턴스 개수: {backgroundInstances.Count}");
            Debug.Log($"현재 스프라이트 인덱스: {currentSpriteIndex}");
            Debug.Log($"다음 리젠 스프라이트: {(useCustomNextSprite ? nextSpriteIndex.ToString() : "자동")}");
            Debug.Log($"이동 속도: {moveSpeed}");
            Debug.Log($"변경 모드: {spriteChangeMode}");
        }
    }
}
