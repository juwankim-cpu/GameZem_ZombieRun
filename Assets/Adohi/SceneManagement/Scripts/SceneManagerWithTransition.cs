using com.cyborgAssets.inspectorButtonPro;
using EasyTransition;
using UnityEngine;
using UnityEngine.SceneManagement;
using Pixelplacement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ZombieRun.Adohi.SceneManagement
{
    public class SceneManagerWithTransition : Singleton<SceneManagerWithTransition>
    {
        [Header("트랜지션 설정")]
        [SerializeField] private TransitionSettings transitionSettings;
        [SerializeField] private float transitionDuration = 1f;

        [Header("다음 씬 설정")]
#if UNITY_EDITOR
        [SerializeField] private SceneAsset nextScene;
#endif
        [SerializeField] private string nextScenePath;

        /// <summary>
        /// 씬 파일로 씬 전환 (SceneAsset)
        /// </summary>
        public void LoadScene(Object sceneAsset)
        {
            LoadScene(sceneAsset, transitionDuration);
        }

        /// <summary>
        /// 씬 파일과 커스텀 지속시간으로 씬 전환 (SceneAsset)
        /// </summary>
        public void LoadScene(Object sceneAsset, float duration)
        {
#if UNITY_EDITOR
            if (sceneAsset is SceneAsset scene)
            {
                string scenePath = AssetDatabase.GetAssetPath(scene);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                LoadSceneByName(sceneName, duration);
                return;
            }
#endif
            Debug.LogError("SceneAsset이 올바르지 않습니다!");
        }

        /// <summary>
        /// 씬 이름으로 씬 전환
        /// </summary>
        public void LoadSceneByName(string sceneName)
        {
            LoadSceneByName(sceneName, transitionDuration);
        }

        /// <summary>
        /// 씬 이름과 커스텀 지속시간으로 씬 전환
        /// </summary>
        public void LoadSceneByName(string sceneName, float duration)
        {
            if (transitionSettings == null)
            {
                Debug.LogError("TransitionSettings가 설정되지 않았습니다!");
                SceneManager.LoadScene(sceneName);
                return;
            }

            TransitionManager.Instance().Transition(sceneName, transitionSettings, duration);
        }

        /// <summary>
        /// 씬 인덱스로 씬 전환
        /// </summary>
        public void LoadSceneByIndex(int sceneIndex)
        {
            LoadSceneByIndex(sceneIndex, transitionDuration);
        }

        /// <summary>
        /// 씬 인덱스와 커스텀 지속시간으로 씬 전환
        /// </summary>
        public void LoadSceneByIndex(int sceneIndex, float duration)
        {
            if (transitionSettings == null)
            {
                Debug.LogError("TransitionSettings가 설정되지 않았습니다!");
                SceneManager.LoadScene(sceneIndex);
                return;
            }

            string sceneName = SceneUtility.GetScenePathByBuildIndex(sceneIndex);
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"씬 인덱스 {sceneIndex}를 찾을 수 없습니다!");
                return;
            }

            sceneName = System.IO.Path.GetFileNameWithoutExtension(sceneName);
            TransitionManager.Instance().Transition(sceneName, transitionSettings, duration);
        }

        /// <summary>
        /// 현재 씬 다시 로드
        /// </summary>
        public void ReloadCurrentScene()
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            LoadSceneByName(currentSceneName);
        }

        /// <summary>
        /// 설정된 다음 씬으로 전환
        /// </summary>
        [ProButton]
        public void LoadNextScene()
        {
#if UNITY_EDITOR
            if (nextScene != null)
            {
                LoadScene(nextScene);
            }
            else if (!string.IsNullOrEmpty(nextScenePath))
            {
                LoadSceneByName(nextScenePath);
            }
            else
            {
                Debug.LogWarning("다음 씬이 설정되지 않았습니다!");
            }
#else
            if (!string.IsNullOrEmpty(nextScenePath))
            {
                LoadSceneByName(nextScenePath);
            }
            else
            {
                Debug.LogWarning("다음 씬 경로가 설정되지 않았습니다!");
            }
#endif
        }

#if UNITY_EDITOR
        // Inspector에서 SceneAsset이 변경될 때 자동으로 경로 저장
        void OnValidate()
        {
            if (nextScene != null)
            {
                string scenePath = AssetDatabase.GetAssetPath(nextScene);
                nextScenePath = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            }
        }
#endif
    }
}
