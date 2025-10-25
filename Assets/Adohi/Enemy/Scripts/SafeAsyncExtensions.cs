using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace ZombieRun.Adohi
{
    public static class SafeAsyncExtensions
    {
        /// <summary>
        /// MonoBehaviour가 파괴될 때 자동으로 취소되는 UniTask를 실행합니다.
        /// </summary>
        public static async UniTask SafeAsync(this UniTask task, MonoBehaviour mono)
        {
            if (mono == null) return;
            await task.AttachExternalCancellation(mono.GetCancellationTokenOnDestroy());
        }

        /// <summary>
        /// MonoBehaviour가 파괴될 때 자동으로 취소되는 UniTask<T>를 실행합니다.
        /// </summary>
        public static async UniTask<T> SafeAsync<T>(this UniTask<T> task, MonoBehaviour mono)
        {
            if (mono == null) return default;
            return await task.AttachExternalCancellation(mono.GetCancellationTokenOnDestroy());
        }

        /// <summary>
        /// DOTween의 Tween을 MonoBehaviour가 파괴될 때 자동으로 취소되도록 합니다.
        /// </summary>
        public static async UniTask SafeAsync(this Tween tween, MonoBehaviour mono)
        {
            if (mono == null || tween == null) return;
            await tween.ToUniTask(cancellationToken: mono.GetCancellationTokenOnDestroy());
        }

        /// <summary>
        /// DOTween의 TweenerCore를 MonoBehaviour가 파괴될 때 자동으로 취소되도록 합니다.
        /// </summary>
        public static async UniTask SafeAsync<T1, T2, TPlugOptions>(this TweenerCore<T1, T2, TPlugOptions> tweener, MonoBehaviour mono)
            where TPlugOptions : struct, IPlugOptions
        {
            if (mono == null || tweener == null) return;
            await tweener.ToUniTask(cancellationToken: mono.GetCancellationTokenOnDestroy());
        }
    }
}

