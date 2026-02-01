using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI 
{
    public class MainMenu : GameWindow
    {
        [SerializeField] private RectTransform title;
        [SerializeField] private RectTransform subtitle;
        [SerializeField] private RectTransform startText;
        [SerializeField] private CanvasGroup background;

        [Header("Animation")]
        [SerializeField] private float backgroundFadeDuration = 1f;
        [SerializeField] private float titleMovePos;
        [SerializeField] private float titleMoveDuration = 0.5f;
        [SerializeField] private float startTitleInterval = 0.2f;
        [SerializeField] private float closeBackgroundInterval = 0.8f;

        private PlayerInput input;
        public Action OnMenuOpened;
        public Action OnMenuClosed;

        public override void OpenWindow()
        {
           input = GetComponent<PlayerInput>();
           input.enabled = true;
           gameObject.SetActive(true);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(background.DOFade(1, backgroundFadeDuration))
                    .AppendCallback(()=>title.gameObject.SetActive(true))
                    .Append(title.DOAnchorPosY(titleMovePos, titleMoveDuration).SetEase(Ease.InQuart))
                    .AppendCallback(()=>subtitle.gameObject.SetActive(true))
                    .AppendInterval(startTitleInterval)
                    .AppendCallback(() => {
                        subtitle.gameObject.SetActive(true);
                        startText.gameObject.SetActive(true);
                        OnMenuOpened?.Invoke();
                     });
        }
        public override void CloseWindow()
        {
            input.enabled = false;
            Sequence sequence = DOTween.Sequence();
            sequence.Append(background.DOFade(0, backgroundFadeDuration))
                    .AppendCallback(() => {
                        gameObject.SetActive(false);
                        OnMenuClosed?.Invoke();
                     });
        }

        public void EnableInput(bool status) 
        {
            input.enabled = status;
        }
    }
}