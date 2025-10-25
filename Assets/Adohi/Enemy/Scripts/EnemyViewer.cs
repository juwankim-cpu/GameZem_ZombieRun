using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace ZombieRun.Adohi
{
    public class EnemyViewer : MonoBehaviour
    {

        public float hideHeight;
        public float showHeight;

        [Header("Scale Settings")]
        public float scaleUpValue = 1.2f;
        public float scaleDownValue = 0.8f;
        public float scaleDuration = 1f;
        public Ease scaleEaseType = Ease.OutBack;
        public Transform body;


        public async UniTask ShowAsnyc()
        {
            await transform.DOMoveY(showHeight, 1f).SetEase(Ease.OutBack).ToUniTask().SafeAsync(this); ;
        }

        public async UniTask HideAsync()
        {
            await transform.DOMoveY(hideHeight, 1f).SetEase(Ease.OutBack).ToUniTask().SafeAsync(this);
        }

        public async UniTask ScaleUpAsync()
        {
            await body.DOScale(scaleUpValue, scaleDuration).SetEase(scaleEaseType).ToUniTask().SafeAsync(this);
        }
        public async UniTask ScaleDownAsync()
        {
            await body.DOScale(scaleDownValue, scaleDuration).SetEase(scaleEaseType).ToUniTask().SafeAsync(this);
        }


    }
}
