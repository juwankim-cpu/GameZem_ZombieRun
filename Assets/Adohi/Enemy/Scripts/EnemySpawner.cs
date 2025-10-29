using System;
using System.Collections.Generic;
using System.Threading;
using com.cyborgAssets.inspectorButtonPro;
using Cysharp.Threading.Tasks;
using Pixelplacement;
using UnityAtoms.BaseAtoms;
using UnityEngine;
using ZombieRun.Adohi.GameSystem;

namespace ZombieRun.Adohi.Enemy
{
    public class EnemySpawner : Singleton<EnemySpawner>
    {
        public List<Enemy> enemiePrefabs;

        public int stage => GameManager.Instance.currentStage.Value;



        public float firstSpawnDelay = 1f;
        public float initialSpawnDelay = 1f;
        public float minSpawnIntervalAmplify = 0.8f;
        public float maxSpawnIntervalAmplify = 1.2f;
        private bool[] isLocationAllocated = new bool[3];
        public Transform[] spawnPoints;

        private CancellationTokenSource cancellationTokenSource;


        [ProButton]
        public async UniTask StartSpawnAsync()
        {
            // 기존 CancellationTokenSource가 있으면 취소하고 새로 생성
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                var ct = cancellationTokenSource.Token;

                await UniTask.Delay((int)(firstSpawnDelay * 1000f), cancellationToken: ct);

                while (!ct.IsCancellationRequested)
                {
                    var nextInterval = initialSpawnDelay * UnityEngine.Random.Range(minSpawnIntervalAmplify, maxSpawnIntervalAmplify) / GameManager.Instance.difficulty;

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
                        int randomIndex = availableIndices[UnityEngine.Random.Range(0, availableIndices.Count)];
                        // randomIndex를 사용하여 스폰 로직 구현

                        if (stage >= 3)
                        {
                            var enemy = Instantiate(enemiePrefabs[UnityEngine.Random.Range(0, enemiePrefabs.Count)], spawnPoints[randomIndex].position, spawnPoints[randomIndex].rotation);
                            enemy.slotIndex = randomIndex;
                            isLocationAllocated[randomIndex] = true;
                            enemy.DoActionAsync().Forget();
                        }
                        else
                        {
                            var enemy = Instantiate(enemiePrefabs[stage], spawnPoints[randomIndex].position, spawnPoints[randomIndex].rotation);
                            enemy.slotIndex = randomIndex;
                            isLocationAllocated[randomIndex] = true;
                            enemy.DoActionAsync().Forget();
                        }
                    }

                    await UniTask.Delay((int)(nextInterval * 1000f), cancellationToken: ct);
                }
            }
            catch (OperationCanceledException)
            {
                // 취소된 경우 안전하게 처리
                Debug.Log("[EnemySpawner] 스폰이 취소되었습니다.");
            }
        }

        public void StopSpawn()
        {
            cancellationTokenSource?.Cancel();
        }

        public void ReleaseEnemy(Enemy enemy)
        {
            isLocationAllocated[enemy.slotIndex] = false;
        }

        void OnDestroy()
        {
            // 게임오브젝트 파괴 시 모든 진행 중인 UniTask 취소
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
        }
    }
}

