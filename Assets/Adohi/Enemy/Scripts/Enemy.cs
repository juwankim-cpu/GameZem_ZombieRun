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


        void Awake()
        {
            enemyLeftSightSystem.Initialize(this);
            enemyRightSightSystem.Initialize(this);

            if (enemyType == EnemyType.Grandma) isAttackPlayer.Value = true;
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
            await enemyViewer.ShowAsnyc().SafeAsync(this);
            await enemyViewer.ScaleUpAsync().SafeAsync(this);
            animator.SetTrigger("IsAttack");
            await UniTask.Delay((int)(attackAnimationDuration * 1000 / timesFaster));
            await UniTask.WhenAll(
                enemyLeftSightSystem.DoSight(sightDelay / timesFaster),
                enemyRightSightSystem.DoSight(sightDelay / timesFaster)
            ).SafeAsync(this);

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

            await UniTask.Delay((int)(resultDelay * 1000 / timesFaster));
            await enemyViewer.ScaleDownAsync().SafeAsync(this);



            await enemyViewer.HideAsync().SafeAsync(this);

            EnemySpawner.Instance.ReleaseEnemy(this);
            Destroy(gameObject);
        }

    }

}
