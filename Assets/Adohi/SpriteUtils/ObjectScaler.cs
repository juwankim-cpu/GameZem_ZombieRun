using UnityEngine;
using DG.Tweening;

namespace ZombieRun.Adohi.Utils
{
    public class ObjectScaler : MonoBehaviour
    {
        [Header("Scale Settings")]
        [SerializeField] private float minScale = 0.8f; // 최소 크기 배율
        [SerializeField] private float maxScale = 1.2f; // 최대 크기 배율
        [SerializeField] private float scaleDuration = 1f; // 한 번 커졌다 작아지는데 걸리는 시간
        [SerializeField] private Ease easeType = Ease.InOutSine; // 이징 타입
        [SerializeField] private bool startOnEnable = true; // Enable될 때 자동 시작
        [SerializeField] private bool uniformScale = true; // XYZ 동일하게 스케일할지 여부

        private Vector3 originalScale;
        private Tween scaleTween;

        void Start()
        {
            originalScale = transform.localScale;

            if (startOnEnable)
            {
                StartScaling();
            }
        }

        void OnEnable()
        {
            if (originalScale != Vector3.zero && startOnEnable)
            {
                StartScaling();
            }
        }

        void OnDisable()
        {
            StopScaling();
        }

        public void StartScaling()
        {
            StopScaling();

            if (uniformScale)
            {
                // XYZ 동일하게 스케일
                Vector3 targetScale = originalScale * maxScale;
                scaleTween = transform.DOScale(targetScale, scaleDuration)
                    .SetEase(easeType)
                    .SetLoops(-1, LoopType.Yoyo); // -1은 무한 루프, Yoyo는 왕복
            }
            else
            {
                // 각 축마다 별도로 스케일 (2D의 경우 X, Y만 사용)
                Vector3 targetScale = new Vector3(
                    originalScale.x * maxScale,
                    originalScale.y * maxScale,
                    originalScale.z
                );
                scaleTween = transform.DOScale(targetScale, scaleDuration)
                    .SetEase(easeType)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        public void StopScaling()
        {
            if (scaleTween != null && scaleTween.IsActive())
            {
                scaleTween.Kill();
                transform.localScale = originalScale; // 원래 크기로 복원
            }
        }

        public void StopScalingWithoutReset()
        {
            if (scaleTween != null && scaleTween.IsActive())
            {
                scaleTween.Kill();
            }
        }

        void OnDestroy()
        {
            StopScalingWithoutReset();
        }
    }
}
