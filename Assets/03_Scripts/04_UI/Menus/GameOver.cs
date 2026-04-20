using DG.Tweening;
using Psalmhaven;
using System;
using UnityEngine;
using UnityEngine.UI;
namespace UI
{
    public class GameOver : GameWindow
    {
        [SerializeField] private RectTransform gameOverText;
        [SerializeField] private RectTransform subtitle;
        [SerializeField] private GameObject buttonGroup;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button exitButton;

        public Action GameOverCutscene;
        private readonly float textScaleDuration = 0.5f;
        private void Start()
        {
            restartButton.onClick.AddListener(() => {
                CloseWindow();
                SceneTransitionManager.instance.Restart();
            });
            exitButton.onClick.AddListener(() => GameOverCutscene?.Invoke());
        }
        private void OnDestroy()
        {
            restartButton.onClick.RemoveAllListeners();
            exitButton.onClick.RemoveAllListeners();
        }
        public override void OpenWindow()
        {
            Time.timeScale = 0;

            gameObject.SetActive(true);
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            sequence.Append(gameOverText.DOScale(Vector3.one, textScaleDuration))
                    .AppendCallback(() => {
                        buttonGroup.gameObject.SetActive(true);
                        subtitle.gameObject.SetActive(true);
                    });
        }
        public override void CloseWindow()
        {
            Time.timeScale = 1;
            gameObject.SetActive(false);
        }
    }

}