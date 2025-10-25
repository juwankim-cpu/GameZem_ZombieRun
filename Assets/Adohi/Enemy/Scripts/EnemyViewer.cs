using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace ZombieRun.Adohi
{
    public class EnemyViewer : MonoBehaviour
    {

        public float hideHeight;
        public float showHeight;


        public float minLeftAngle;
        public float maxLeftAngle;
        public float minRightAngle;
        public float maxRightAngle;


        public async UniTask ShowAsnyc()
        {
            await transform.DOMoveY(showHeight, 1f).SetEase(Ease.OutBack).ToUniTask().SafeAsync(this); ;
        }

        public async UniTask HideAsync()
        {
            await transform.DOMoveY(hideHeight, 1f).SetEase(Ease.OutBack).ToUniTask().SafeAsync(this);
        }


    }
}
