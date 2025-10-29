using com.cyborgAssets.inspectorButtonPro;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Fever : MonoBehaviour
{

    public GameObject feverChunk;
    public WorldObjectAnimation characterWorldObjectAnimation;
    public WorldObjectAnimation textWorldObjectAnimation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        // 초기화 완료 후 비활성화
        feverChunk.SetActive(false);
    }

    [ProButton]
    public async UniTask ShowFever()
    {
        Time.timeScale = 0f;
        feverChunk.SetActive(true);

        // 첫 프레임 대기 (컴포넌트 초기화 완료 대기)
        await UniTask.Yield();

        await UniTask.WhenAll(
            characterWorldObjectAnimation.ShowAndHide(deactivateOnHide: false),
            textWorldObjectAnimation.ShowAndHide(deactivateOnHide: false)
        );

        feverChunk.SetActive(false);
        Time.timeScale = 1f;
    }

    [ProButton]
    public void TestShowAndHide()
    {
        characterWorldObjectAnimation.ShowAndHide().Forget();
        textWorldObjectAnimation.ShowAndHide().Forget();
    }
}
