using com.cyborgAssets.inspectorButtonPro;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZombieRun.Adohi;

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

        public bool isAttackPlayer = false;

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
