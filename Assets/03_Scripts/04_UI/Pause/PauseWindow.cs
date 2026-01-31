using Psalmhaven;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class PauseWindow : GameWindow
    {
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button toHomeButton;
        [SerializeField] private Button exitButton;

        public bool isPaused = false;  
        private void Start()
        {
            continueButton.onClick.AddListener(() => CloseWindow());
            restartButton.onClick.AddListener(() => SceneTransitionManager.instance.Restart());
            toHomeButton.onClick.AddListener(() => SceneTransitionManager.instance.LoadSceneAsync(0, false, 0, true));
            exitButton.onClick.AddListener(() => SceneTransitionManager.instance.Exit());
        }
        private void OnDestroy()
        {
            continueButton.onClick.RemoveAllListeners();
            restartButton.onClick.RemoveAllListeners();
            toHomeButton.onClick.RemoveAllListeners();
            exitButton.onClick.RemoveAllListeners();
        }
        public override void OpenWindow()
        {
            isPaused = true;
            gameObject.SetActive(true);
            Time.timeScale = 0;
        }
        public override void CloseWindow()
        {
            isPaused = false;
            gameObject.SetActive(false);
            Time.timeScale = 1;
        }
    }
}