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


        public async UniTask ShowAsnyc(float speedMultiplier = 1f)
        {
            float duration = 1f / speedMultiplier;
            await transform.DOMoveY(showHeight, duration).SetEase(Ease.OutBack).ToUniTask().SafeAsync(this);
        }

        public async UniTask HideAsync(float speedMultiplier = 1f)
        {
            float duration = 1f / speedMultiplier;
            await transform.DOMoveY(hideHeight, duration).SetEase(Ease.OutBack).ToUniTask().SafeAsync(this);
        }

        public async UniTask ScaleUpAsync(float speedMultiplier = 1f)
        {
            float duration = scaleDuration / speedMultiplier;
            await body.DOScale(scaleUpValue, duration).SetEase(scaleEaseType).ToUniTask().SafeAsync(this);
        }

        public async UniTask ScaleDownAsync(float speedMultiplier = 1f)
        {
            float duration = scaleDuration / speedMultiplier;
            await body.DOScale(scaleDownValue, duration).SetEase(scaleEaseType).ToUniTask().SafeAsync(this);
        }


    }
}
