using UnityEngine;
using System.Collections.Generic;

namespace ZombieRun.Adohi.Evironment
{
    public class BackgroundMover : MonoBehaviour
    {
        [Header("배경 설정")]
        [SerializeField] private Sprite sprite;
        [SerializeField] private float moveSpeed = 2f; // 이동 속도 (단위/초)
        [SerializeField] private string sortingLayerName = "Background";
        [SerializeField] private int sortingOrder = 0;

        [Header("반복 설정")]
        [SerializeField] private int instanceCount = 3; // 생성할 배경 개수
        [SerializeField] private float spacing = 0f; // 이미지 간 간격 (양수: 간격, 음수: 겹침)

        private List<GameObject> backgroundInstances = new List<GameObject>();
        private float spriteWidth;
        private float totalWidth; // 스프라이트 너비 + 간격

        void Start()
        {
            if (sprite == null)
            {
                Debug.LogError("BackgroundMover: Sprite가 설정되지 않았습니다!");
                return;
            }

            // 스프라이트 너비 계산
            spriteWidth = sprite.bounds.size.x;
            totalWidth = spriteWidth + spacing;

            // 배경 인스턴스 생성
            CreateBackgroundInstances();
        }

        void Update()
        {
            if (backgroundInstances.Count == 0) return;

            // 모든 배경을 왼쪽으로 이동
            foreach (var bg in backgroundInstances)
            {
                bg.transform.position += Vector3.left * moveSpeed * Time.deltaTime;
            }

            // 가장 왼쪽 배경이 화면 밖으로 나가면 오른쪽 끝으로 이동
            CheckAndRepositionBackgrounds();
        }

        void CreateBackgroundInstances()
        {
            for (int i = 0; i < instanceCount; i++)
            {
                GameObject bgInstance = new GameObject($"Background_{i}");
                bgInstance.transform.SetParent(transform);

                // SpriteRenderer 추가
                SpriteRenderer sr = bgInstance.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = sortingOrder;

                // 초기 위치 설정 (나란히 배치, 간격 포함)
                bgInstance.transform.position = transform.position + Vector3.right * totalWidth * i;

                backgroundInstances.Add(bgInstance);
            }
        }

        void CheckAndRepositionBackgrounds()
        {
            // 카메라의 왼쪽 경계 계산
            Camera cam = Camera.main;
            if (cam == null) return;

            float leftBound = cam.transform.position.x - cam.orthographicSize * cam.aspect - spriteWidth;

            foreach (var bg in backgroundInstances)
            {
                // 배경이 화면 왼쪽 밖으로 완전히 벗어났는지 확인
                if (bg.transform.position.x < leftBound)
                {
                    // 가장 오른쪽 배경 찾기
                    float maxX = float.MinValue;
                    foreach (var otherBg in backgroundInstances)
                    {
                        if (otherBg.transform.position.x > maxX)
                        {
                            maxX = otherBg.transform.position.x;
                        }
                    }

                    // 가장 오른쪽 끝으로 재배치 (간격 포함)
                    bg.transform.position = new Vector3(maxX + totalWidth, bg.transform.position.y, bg.transform.position.z);
                }
            }
        }

        // 런타임에 속도 변경
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = speed;
        }

        // 런타임에 스프라이트 변경
        public void SetSprite(Sprite newSprite)
        {
            sprite = newSprite;
            spriteWidth = sprite.bounds.size.x;
            totalWidth = spriteWidth + spacing;

            foreach (var bg in backgroundInstances)
            {
                SpriteRenderer sr = bg.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = newSprite;
                }
            }
        }

        // 런타임에 간격 변경
        public void SetSpacing(float newSpacing)
        {
            spacing = newSpacing;
            totalWidth = spriteWidth + spacing;
        }

        void OnDestroy()
        {
            // 생성한 배경 인스턴스 정리
            foreach (var bg in backgroundInstances)
            {
                if (bg != null)
                {
                    Destroy(bg);
                }
            }
            backgroundInstances.Clear();
        }
    }
}
