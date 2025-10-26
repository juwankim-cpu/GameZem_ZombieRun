using System;
using System.Threading;
using com.cyborgAssets.inspectorButtonPro;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityAtoms.BaseAtoms;
using UnityEngine;
using ZombieRun.Adohi;
using ZombieRun.Adohi.GameSystem;

namespace ZombieRun.Adohi.Enemy
{
    public class Enemy : MonoBehaviour
    {
        public enum EnemyType
        {

            Soilder,
            Grandma,
            Teacher
        }
        public EnemyType enemyType;
        public EnemyViewer enemyViewer;
        public EnemySightSystem enemyLeftSightSystem;
        public EnemySightSystem enemyRightSightSystem;

        public BoolReference isAttackPlayer;

        public int slotIndex;

        public Animator animator;

        public float attackAnimationDuration = 1f;
        public float sightDelay = 1f;
        public float resultDelay = 1f;

        private float timesFaster = 1f;
        private CancellationTokenSource cancellationTokenSource;


        void Awake()
        {
            enemyLeftSightSystem.Initialize(this);
            enemyRightSightSystem.Initialize(this);

            if (enemyType == EnemyType.Grandma) isAttackPlayer.Value = true;

            // CancellationTokenSource 생성
            cancellationTokenSource = new CancellationTokenSource();
        }

        void Start()
        {
            isAttackPlayer.ObserveEveryValueChanged(x => x.Value).Subscribe(x =>
            {
                if (x)
                {
                    Debug.Log("Attack Player");
                    if (enemyType != EnemyType.Grandma)
                    {
                        GameManager.Instance.character.GetHit();
                        EnemyManager.Instance.GetHit();

                    }
                }
            });
        }

        [ProButton]
        public async UniTask DoActionAsync()
        {
            try
            {
                var ct = cancellationTokenSource.Token;

                await enemyViewer.ShowAsnyc().AttachExternalCancellation(ct);
                await enemyViewer.ScaleUpAsync().AttachExternalCancellation(ct);

                if (ct.IsCancellationRequested) return;
                animator.SetTrigger("IsAttack");

                await UniTask.Delay((int)(attackAnimationDuration * 1000 / timesFaster), cancellationToken: ct);

                await UniTask.WhenAll(
                    enemyLeftSightSystem.DoSight(sightDelay / timesFaster),
                    enemyRightSightSystem.DoSight(sightDelay / timesFaster)
                ).AttachExternalCancellation(ct);

                if (ct.IsCancellationRequested) return;

                if (isAttackPlayer)
                {
                    animator.SetTrigger("IsSuccess");
                    if (enemyType == EnemyType.Grandma)
                    {
                        GameManager.Instance.character.GetHit();
                        EnemyManager.Instance.GetHit();
                    }
                }
                else
                {
                    animator.SetTrigger("IsFail");
                }

                await UniTask.Delay((int)(resultDelay * 1000 / timesFaster), cancellationToken: ct);
                await enemyViewer.ScaleDownAsync().AttachExternalCancellation(ct);
                await enemyViewer.HideAsync().AttachExternalCancellation(ct);

                if (EnemySpawner.Instance != null)
                    EnemySpawner.Instance.ReleaseEnemy(this);

                if (gameObject != null)
                    Destroy(gameObject);
            }
            catch (OperationCanceledException)
            {
                // 취소된 경우 안전하게 처리
                Debug.Log($"[Enemy] {enemyType} 액션이 취소되었습니다.");
            }
        }

        void OnDestroy()
        {
            // 게임오브젝트 파괴 시 모든 진행 중인 UniTask 취소
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }

    }

}
