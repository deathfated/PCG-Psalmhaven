using System;
using UI;
using UnityEngine;

namespace Psalmhaven.UI
{
    public class DiceBoard : GameWindow
    {
        [SerializeField] private float openPosY;
        [SerializeField] private Script_Dice dice;
        private Animator animator;

        [SerializeField] private RectTransform rectTransform;
        private float closePosY;
        private readonly float openAnimateDuration = 0.3f;
        private void Start()
        {
            animator = GetComponent<Animator>();
            rectTransform = GetComponent<RectTransform>();
            closePosY = transform.position.y;
        }

        public override void OpenWindow()
        {
            animator = GetComponent<Animator>();
            animator.SetBool("IsShow", true);
            //rectTransform.DOAnchorPosY(openPosY, openAnimateDuration).OnComplete(()=>dice.canRoll = true);
            dice.canRoll = true;
        }

        public override void CloseWindow()
        {
            if (dice.isRoll) return;
            dice.canRoll = false;
            //rectTransform.DOAnchorPosY(closePosY, openAnimateDuration);
            animator.SetBool("IsShow", false);
        }
        public void RollDice(Action OnStartRoll = null, Action<int> OnFinishedRoll = null)
        {
            dice.RollDice(OnStartRoll, OnFinishedRoll);
        }
    }
}

