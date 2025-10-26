using System;
using Cysharp.Threading.Tasks;
using Pixelplacement;
using UnityAtoms.BaseAtoms;
using UnityEngine;
using UnityEngine.Events;


namespace ZombieRun.Adohi.Enemy
{
    public class EnemyManager : Singleton<EnemyManager>
    {
        public EnemySpawner enemySpawner;
        public GameObjectReference player;

        public UnityEvent<int> OnHitCharacter = new();
        public bool isSpawnOnAwake = false;

        public enum DummyCharacterState
        {
            Idle,
            Crouch,
            Attractiveness,
            Study,
        }

        public void Awake()
        {
            if (isSpawnOnAwake)
            {
                enemySpawner.StartSpawnAsync().Forget();
            }


        }

        float CalculateCurrentDamage()
        {
            return 5f;
        }

    }
}
