using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class DiceBoard : GameWindow
    {
        [SerializeField] private float openPosY;
        [SerializeField] private Script_Dice dice;

        [SerializeField] private RectTransform rectTransform;
        private float closePosY;
        private readonly float openAnimateDuration = 0.3f;
        private void Start()
        {
            rectTransform = GetComponent<RectTransform>();
            closePosY = transform.position.y;
        }

        public override void OpenWindow()
        {
            rectTransform.DOAnchorPosY(openPosY, openAnimateDuration).OnComplete(()=>dice.canRoll = true);
        }

        public override void CloseWindow()
        {
            if (dice.isRoll) return;
            dice.canRoll = false;
            rectTransform.DOAnchorPosY(closePosY, openAnimateDuration);
        }
        public void RollDice(Action OnStartRoll = null, Action<int> OnFinishedRoll = null)
        {
            dice.RollDice(OnStartRoll, OnFinishedRoll);
        }
    }
}

