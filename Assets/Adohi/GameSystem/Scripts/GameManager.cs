using Cysharp.Threading.Tasks;
using UnityAtoms.BaseAtoms;
using UnityEngine;

namespace ZombieRun.Adohi.GameSystem
{
    public class GameManager : MonoBehaviour
    {
        public IntReference currentState;

        public float timeFromStart;

        void Start()
        {
            timeFromStart = 0f;
        }

        public void Update()
        {
            timeFromStart += Time.deltaTime;
        }

        public async UniTask PlayAsync()
        {
            StageStart();
            //show stage ui
            //hide stage ui
            //time scale 1
            //wait for stage end;


        }

        public void StageStart()
        {
            currentState.Value += 1;
            timeFromStart = 0f;
        }




        public async UniTask StageEndAsync()
        {

        }
    }

}
