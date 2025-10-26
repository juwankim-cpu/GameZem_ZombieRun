using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ZombieRun.Adohi.Utils
{
    public class ObjectFloater : MonoBehaviour
    {
        [Header("Float Settings")]
        [SerializeField] private float floatHeight = 0.5f; // 위아래로 움직일 거리
        [SerializeField] private float floatDuration = 1.5f; // 한 번 움직이는데 걸리는 시간
        [SerializeField] private Ease easeType = Ease.InOutSine; // 이징 타입
        [SerializeField] private bool startOnEnable = true; // Enable될 때 자동 시작
        [SerializeField] private float returnDuration = 0.5f; // 중앙으로 돌아가는데 걸리는 시간

        private Vector3 startPosition;
        private Tween floatTween;
        private CancellationTokenSource stopCts;
        private bool isStopping = false;

        void Start()
        {
            startPosition = transform.localPosition;

            if (startOnEnable)
            {
                StartFloating();
            }
        }

        void OnEnable()
        {
            if (startPosition != Vector3.zero && startOnEnable)
            {
                StartFloating();
            }
        }

        void OnDisable()
        {
            StopFloatingImmediate();
        }

        public void StartFloating()
        {
            StopFloatingImmediate();
            isStopping = false;

            // 위아래로 무한 반복하는 애니메이션
            floatTween = transform.DOLocalMoveY(startPosition.y + floatHeight, floatDuration)
                .SetEase(easeType)
                .SetLoops(-1, LoopType.Yoyo); // -1은 무한 루프, Yoyo는 왕복
        }

        public async UniTask StopFloating()
        {
            if (isStopping) return;
            isStopping = true;

            // 기존 정지 작업 취소
            stopCts?.Cancel();
            stopCts?.Dispose();
            stopCts = new CancellationTokenSource();

            // 현재 플로팅 트윈 중지
            if (floatTween != null && floatTween.IsActive())
            {
                floatTween.Kill();
            }

            try
            {
                // 중앙 위치로 부드럽게 이동
                await transform.DOLocalMoveY(startPosition.y, returnDuration)
                    .SetEase(Ease.OutSine)
                    .WithCancellation(stopCts.Token);
            }
            catch (System.OperationCanceledException)
            {
                // 취소되었을 때는 무시
            }
            finally
            {
                isStopping = false;
            }
        }

        public void StopFloatingImmediate()
        {
            isStopping = false;

            // 진행 중인 정지 작업 취소
            stopCts?.Cancel();
            stopCts?.Dispose();
            stopCts = null;

            // 모든 트윈 즉시 중지
            if (floatTween != null && floatTween.IsActive())
            {
                floatTween.Kill();
            }

            transform.DOKill(); // 해당 transform의 모든 DOTween 중지
        }

        void OnDestroy()
        {
            StopFloatingImmediate();
        }
    }

}
