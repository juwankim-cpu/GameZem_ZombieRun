using com.cyborgAssets.inspectorButtonPro;
using Cysharp.Threading.Tasks;
using UnityEngine;
using ZombieRun.Adohi;

namespace ZombieRun.Adohi.Enemy
{
    public class Enemy : MonoBehaviour
    {
        public EnemyViewer enemyViewer;
        public EnemySightSystem enemyLeftSightSystem;
        public EnemySightSystem enemyRightSightSystem;

        public int slotIndex;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }


        [ProButton]
        public async UniTask DoActionAsync()
        {
            await enemyViewer.ShowAsnyc().SafeAsync(this);
            await UniTask.WhenAll(
                enemyLeftSightSystem.DoSight(1f),
                enemyRightSightSystem.DoSight(1f)
            ).SafeAsync(this);



            await enemyViewer.HideAsync().SafeAsync(this);

            EnemySpawner.Instance.ReleaseEnemy(this);
            Destroy(gameObject);
        }

    }

}
