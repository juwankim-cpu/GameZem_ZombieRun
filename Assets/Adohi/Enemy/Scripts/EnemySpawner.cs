using System.Collections.Generic;
using com.cyborgAssets.inspectorButtonPro;
using Cysharp.Threading.Tasks;
using Pixelplacement;
using UnityAtoms.BaseAtoms;
using UnityEngine;

namespace ZombieRun.Adohi.Enemy
{
    public class EnemySpawner : Singleton<EnemySpawner>
    {
        public List<Enemy> enemiePrefabs;


        public float difficulty = 1f;
        public IntReference stage;



        public float firstSpawnDelay = 1f;
        public float initialSpawnDelay = 1f;
        public float minSpawnIntervalAmplify = 0.8f;
        public float maxSpawnIntervalAmplify = 1.2f;
        private bool[] isLocationAllocated = new bool[3];
        public Transform[] spawnPoints;


        [ProButton]
        public async UniTask StartSpawnAsync()
        {

            await UniTask.Delay((int)(firstSpawnDelay * 1000f));

            while (true)
            {
                var nextInterval = initialSpawnDelay * Random.Range(minSpawnIntervalAmplify, maxSpawnIntervalAmplify);

                // false인 위치 중 랜덤 선택
                List<int> availableIndices = new List<int>();
                for (int i = 0; i < isLocationAllocated.Length; i++)
                {
                    if (!isLocationAllocated[i])
                    {
                        availableIndices.Add(i);
                    }
                }

                if (availableIndices.Count > 0)
                {
                    int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
                    // randomIndex를 사용하여 스폰 로직 구현


                    if (stage.Value >= 3)
                    {

                    }
                    else
                    {
                        var enemy = Instantiate(enemiePrefabs[stage.Value], spawnPoints[randomIndex].position, spawnPoints[randomIndex].rotation);
                        enemy.slotIndex = randomIndex;
                        isLocationAllocated[randomIndex] = true;
                        enemy.DoActionAsync().Forget();

                    }
                }

                await UniTask.Delay((int)(nextInterval * 1000f));
            }
        }

        public void ReleaseEnemy(Enemy enemy)
        {
            isLocationAllocated[enemy.slotIndex] = false;
        }
    }
}

