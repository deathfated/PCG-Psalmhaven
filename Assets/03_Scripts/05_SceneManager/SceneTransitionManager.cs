using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Psalmhaven 
{
    public class SceneTransitionManager : MonoBehaviour
    {
        [SerializeField] private CircleFade fade;
        public static SceneTransitionManager instance;
        private int activeSpawnPoint;
        private void Awake()
        {
            if (instance == null) instance = this;
            else Destroy(gameObject);

            DontDestroyOnLoad(gameObject);
        }

        public void LoadSceneAsync(int sceneIndex, bool useFade, int spawnPointPosition, bool lastScene = false)
        {
            StartCoroutine(LoadSceneProcess(sceneIndex, useFade, spawnPointPosition, lastScene));
        }

        public IEnumerator LoadSceneProcess(int sceneIndex, bool useFade, int spawnPointPosition, bool lastScene = false)
        {
            if (!lastScene)
            {
                activeSpawnPoint = spawnPointPosition;
            }

            if (useFade && fade != null)
                yield return fade.StartFade(Vector3.zero, Vector3.one);

            Time.timeScale = 1;

            yield return SceneManager.LoadSceneAsync(sceneIndex);

            Debug.Log("Transition Complete");
            yield return null;

            if (LevelManager.Instance == null)
            {
                yield break;
            }

            Transform player = GameObject.FindWithTag("Player").transform;
            if (player != null)
            {
                Transform spawnPosition = LevelManager.Instance.GetSpawnPosition(spawnPointPosition);
                player.transform.position = spawnPosition.position;
                player.transform.rotation = spawnPosition.rotation;
            }

            yield return new WaitForSeconds(0.2f);

            if (useFade && fade != null)
                yield return fade.StartFade(Vector3.one, Vector3.zero);
        }

        public void Restart()
        {
            int currentScene = SceneManager.GetActiveScene().buildIndex;
            LoadSceneAsync(currentScene, false, activeSpawnPoint);
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
