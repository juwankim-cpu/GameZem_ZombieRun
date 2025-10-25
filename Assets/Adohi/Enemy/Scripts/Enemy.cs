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

        public Transform body;


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
            await UniTask.WhenAll(
                enemyLeftSightSystem.DoSight(1f),
                enemyRightSightSystem.DoSight(1f)
            ).SafeAsync(this);
            await enemyViewer.ScaleDownAsync().SafeAsync(this);



            await enemyViewer.HideAsync().SafeAsync(this);

            EnemySpawner.Instance.ReleaseEnemy(this);
            Destroy(gameObject);
        }

    }

}
