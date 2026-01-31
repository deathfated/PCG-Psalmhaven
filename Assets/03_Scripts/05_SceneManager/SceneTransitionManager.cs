using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Psalmhaven 
{
    public class SceneTransitionManager : MonoBehaviour
    {
        [SerializeField] private CircleFade fade;
        public static SceneTransitionManager instance;
        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
        }
        public void LoadSceneAsync(int sceneIndex, bool useFade)
        {
            if (useFade && fade != null) fade.StartFade(Vector3.zero, Vector3.one);
            Time.timeScale = 1;
            SceneManager.LoadSceneAsync(sceneIndex);
        }
        public void Restart()
        {
            int currentScene = SceneManager.GetActiveScene().buildIndex;
            LoadSceneAsync(currentScene, false);
        }
        public void Exit()
        {
            Application.Quit();
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#endif
        }
    }
}
