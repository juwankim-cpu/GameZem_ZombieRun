using UnityEngine;
using Ami.BroAudio;

public class AnimationSoundSystem : StateMachineBehaviour
{
    [Header("BroAudio Settings")]
    [SerializeField] private SoundID soundId;
    [SerializeField, Range(0f, 1f)] private float offset; // 0=시작, 1=끝, 중간도 가능
    [SerializeField] private bool stopOnExit = true;
    [SerializeField, Range(0.1f, 3f)] private float pitch = 1f; // 재생 속도 (0.1 ~ 3배)
    [SerializeField] private bool useAnimationSpeed = true; // 애니메이션 속도를 pitch에 반영

    private bool played;
    private IAudioPlayer handle;
    private Animator cachedAnimator;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        played = false;
        handle = null;
        cachedAnimator = animator;

        if (offset <= 0f)
            PlaySound();
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!played && offset > 0f && stateInfo.normalizedTime >= offset)
        {
            cachedAnimator = animator;
            PlaySound();
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stopOnExit && handle != null)
        {
            handle.Stop();
            handle = null;
        }
    }

    private void PlaySound()
    {
        played = true;

        if (!soundId.IsValid())
            return;

        handle = BroAudio.Play(soundId);

        if (handle != null)
        {
            float finalPitch = pitch;

            // 애니메이션 속도를 pitch에 반영
            if (useAnimationSpeed && cachedAnimator != null)
            {
                finalPitch *= cachedAnimator.speed;
            }

            // pitch가 1이 아니면 적용
            if (finalPitch != 1f)
            {
                handle.SetPitch(finalPitch);
            }
        }
    }
}